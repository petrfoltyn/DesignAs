#pragma once
#include "MaterialProperties.h"
#include "InteractionDiagram.h"
#include "ConcreteIntegrationFast.h"  // Use analytical integration
#include <cmath>
#include <iostream>
#include <iomanip>
#include <vector>
#include <algorithm>

// Design result structure
struct DesignResult {
    bool converged;
    double As2;          // [m^2] bottom reinforcement area
    double epsTop;       // [-] strain at top fiber
    double epsBot;       // [-] strain at bottom fiber
    double epsS2;        // [-] strain in As2
    double sigmaS2;      // [Pa] stress in As2
    double N_calc;       // [N] calculated axial force
    double M_calc;       // [Nm] calculated moment
    double errorAbs;     // [Nm] absolute moment error
    double errorRel;     // [-] relative moment error
    int iterations;      // number of iterations (not used with diagram lookup)
};

// Design algorithm using pre-generated interaction diagram
class ReinforcementDesigner {
private:
    SectionGeometry geom;
    ConcreteProperties concrete;
    SteelProperties steel;
    std::vector<DiagramPoint> diagram;  // Pre-generated interaction diagram (As1=0, As2=0)

    // Find two points on diagram that bracket the target moment
    // Returns indices of points before and after target M at given N
    std::pair<int, int> FindBracketingPoints(double N_target, double M_target) {
        // First, find points with similar N values
        // For pure bending (N=0), we need to find the range where M varies

        int bestIdx = -1;
        double minError = 1e100;

        // Find point closest to target N
        for (size_t i = 0; i < diagram.size(); i++) {
            double N_diagram = diagram[i].N * 1000.0;  // kN to N
            double error = std::abs(N_diagram - N_target);
            if (error < minError) {
                minError = error;
                bestIdx = i;
            }
        }

        if (bestIdx < 0) return {-1, -1};

        // Now search around this point for M bracketing
        // Look for segment where M changes from below to above target (or vice versa)

        for (int i = 0; i < (int)diagram.size() - 1; i++) {
            double N1 = diagram[i].N * 1000.0;
            double N2 = diagram[i + 1].N * 1000.0;
            double M1 = diagram[i].M * 1000.0;
            double M2 = diagram[i + 1].M * 1000.0;

            // Check if both points have similar N (within tolerance)
            double N_mid = (N1 + N2) / 2.0;
            if (std::abs(N_mid - N_target) < std::abs(N_target) * 0.1 + 1000.0) {
                // Check if M brackets target
                if ((M1 <= M_target && M_target <= M2) || (M2 <= M_target && M_target <= M1)) {
                    return {i, i + 1};
                }
            }
        }

        return {-1, -1};
    }

    // Linear interpolation between two diagram points to find required As2
    DesignResult InterpolateDesign(const DiagramPoint& p1, const DiagramPoint& p2,
                                   double N_target, double M_target) {
        DesignResult result;
        result.converged = false;

        // Convert to consistent units (N, Nm)
        double M1 = p1.M * 1000.0;
        double M2 = p2.M * 1000.0;

        // Interpolation parameter
        double t = 0.5;
        if (std::abs(M2 - M1) > 1e-6) {
            t = (M_target - M1) / (M2 - M1);
        }

        // Clamp t to [0, 1]
        t = std::max(0.0, std::min(1.0, t));

        // Interpolate all properties
        result.epsTop = (p1.epsTop + t * (p2.epsTop - p1.epsTop)) / 1000.0;  // per mille to absolute
        result.epsBot = (p1.epsBot + t * (p2.epsBot - p1.epsBot)) / 1000.0;
        result.epsS2 = (p1.epsS2 + t * (p2.epsS2 - p1.epsS2)) / 1000.0;
        result.sigmaS2 = (p1.sigS2 + t * (p2.sigS2 - p1.sigS2)) * 1e6;  // MPa to Pa

        // Calculate required As2 from equilibrium
        // At this strain state, we have concrete forces - USE FAST ANALYTICAL METHOD
        ConcreteForces cf = ConcreteIntegrationFast::CalculateForce(
            result.epsTop, result.epsBot, geom.b, geom.h, concrete
        );

        // Required As2 for equilibrium: N = Fc + As2 * sigmaS2
        if (std::abs(result.sigmaS2) > 1e-6) {
            result.As2 = (N_target - cf.Fc) / result.sigmaS2;
        } else {
            result.As2 = 0.0;
        }

        // Ensure non-negative
        if (result.As2 < 0.0) result.As2 = 0.0;

        // Calculate actual N and M with this As2
        double Fs2 = result.As2 * result.sigmaS2;
        result.N_calc = cf.Fc + Fs2;

        double y2_center = geom.d2 - geom.h / 2.0;
        result.M_calc = cf.Mc + Fs2 * (-y2_center);

        // Calculate errors
        result.errorAbs = std::abs(result.M_calc - M_target);
        result.errorRel = (std::abs(M_target) > 1e-6) ? result.errorAbs / std::abs(M_target) : 0.0;

        result.converged = true;
        result.iterations = 0;  // Direct lookup, no iterations

        return result;
    }

public:
    // Constructor: generates interaction diagram once
    ReinforcementDesigner(const SectionGeometry& g, const ConcreteProperties& c,
                         const SteelProperties& s, int diagramDensity = 10)
        : geom(g), concrete(c), steel(s) {

        std::cout << "Generating interaction diagram (As1=0, As2=0)...\n";

        // Generate diagram with As1=0, As2=0 (concrete only, for finding strain states)
        InteractionDiagram diagramGen(geom, concrete, steel, 0.0, 0.0);
        diagram = diagramGen.Generate(diagramDensity);

        std::cout << "Diagram generated with " << diagram.size() << " points.\n";

        // Print diagram bounds for debugging
        double M_min = 1e100, M_max = -1e100;
        double N_min = 1e100, N_max = -1e100;
        for (const auto& pt : diagram) {
            M_min = std::min(M_min, pt.M);
            M_max = std::max(M_max, pt.M);
            N_min = std::min(N_min, pt.N);
            N_max = std::max(N_max, pt.N);
        }
        std::cout << "Diagram bounds: N=[" << N_min << ", " << N_max << "] kN, "
                  << "M=[" << M_min << ", " << M_max << "] kNm\n";
    }

    // Design for specific load case
    DesignResult Design(const DesignLoads& loads, bool verbose = true) {
        if (verbose) {
            std::cout << "\n==========================================================\n";
            std::cout << "Designing for: N = " << loads.N / 1000.0 << " kN, M = " << loads.M / 1000.0 << " kNm\n";
            std::cout << "==========================================================\n";
        }

        // Find bracketing points on diagram
        auto [idx1, idx2] = FindBracketingPoints(loads.N, loads.M);

        if (idx1 < 0 || idx2 < 0) {
            if (verbose) {
                std::cout << "ERROR: Could not find bracketing points on diagram!\n";
                std::cout << "Target load may be outside the feasible range.\n";
            }

            DesignResult result;
            result.converged = false;
            return result;
        }

        if (verbose) {
            std::cout << "Found bracketing points:\n";
            std::cout << "  Point " << idx1 << ": N=" << diagram[idx1].N << " kN, M=" << diagram[idx1].M << " kNm\n";
            std::cout << "  Point " << idx2 << ": N=" << diagram[idx2].N << " kN, M=" << diagram[idx2].M << " kNm\n";
        }

        // Interpolate to find design
        DesignResult result = InterpolateDesign(diagram[idx1], diagram[idx2], loads.N, loads.M);

        if (verbose && result.converged) {
            std::cout << "\n[OK] Design found by interpolation\n";
            std::cout << "Required As2 = " << result.As2 * 10000.0 << " cm^2\n";
            std::cout << "Error: " << result.errorRel * 100.0 << " %\n";
        }

        return result;
    }

    // Get the generated diagram (for export, visualization, etc.)
    const std::vector<DiagramPoint>& GetDiagram() const {
        return diagram;
    }

    // Design for multiple load cases efficiently
    std::vector<DesignResult> DesignMultiple(const std::vector<DesignLoads>& loadCases) {
        std::vector<DesignResult> results;

        std::cout << "\n==========================================================\n";
        std::cout << "Designing for " << loadCases.size() << " load cases\n";
        std::cout << "==========================================================\n";

        for (size_t i = 0; i < loadCases.size(); i++) {
            std::cout << "\nLoad case " << (i + 1) << "/" << loadCases.size() << ":\n";
            results.push_back(Design(loadCases[i]));
        }

        return results;
    }
};

#pragma once
#include "MaterialProperties.h"
#include "ConcreteIntegration.h"
#include "SteelStress.h"
#include <cmath>
#include <iostream>
#include <iomanip>

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
    int iterations;      // number of iterations
};

// Design algorithm (Variant 2: As1=0, As2 variable)
class ReinforcementDesigner {
private:
    SectionGeometry geom;
    ConcreteProperties concrete;
    SteelProperties steel;
    DesignLoads loads;

    // Find epsBot that satisfies N equilibrium for given epsTop and As2
    double FindEpsBot(double epsTop, double As2_given) {
        double epsBot = epsTop; // initial guess

        for (int i = 0; i < 50; i++) {
            // Concrete forces
            ConcreteForces cf = ConcreteIntegration::CalculateForce(
                epsTop, epsBot, geom.b, geom.h, concrete
            );

            // Strain in As2
            double y2_from_top = geom.h - geom.d2;
            double epsS2 = epsTop + (epsBot - epsTop) * y2_from_top / geom.h;

            // Stress in As2
            double sigmaS2 = SteelStress::CalculateStress(epsS2, steel);

            // Steel force
            double Fs2 = As2_given * sigmaS2;

            // Total axial force
            double N_calc = cf.Fc + Fs2;

            // Check convergence
            double N_error = std::abs(N_calc - loads.N);
            if (N_error < std::abs(loads.N) * 0.0001 + 1.0) {
                return epsBot;
            }

            // Update epsBot - Newton-like step
            double dN_deps = geom.b * geom.h * std::abs(concrete.fcd) +
                           As2_given * steel.Es * y2_from_top / geom.h;
            if (dN_deps > 1e-6) {
                epsBot -= (N_calc - loads.N) / dN_deps * 0.5;
            } else {
                epsBot += (loads.N - N_calc) * 0.0001;
            }

            // Constrain epsBot
            if (epsBot < 1.2 * concrete.epsCu) epsBot = 1.2 * concrete.epsCu;
            if (epsBot > 1.2 * steel.epsUd) epsBot = 1.2 * steel.epsUd;
        }

        return epsBot;
    }

    // Calculate As2, M, N for given epsTop
    void CalculateForEpsTop(double epsTop, double& As2_out, double& M_out, double& N_out,
                           double& epsBot_out, double& epsS2_out, double& sigmaS2_out) {
        // Start with As2 = 0 and find corresponding epsBot
        As2_out = 0.0;
        epsBot_out = FindEpsBot(epsTop, As2_out);

        // Concrete forces
        ConcreteForces cf = ConcreteIntegration::CalculateForce(
            epsTop, epsBot_out, geom.b, geom.h, concrete
        );

        // Strain in As2
        double y2_from_top = geom.h - geom.d2;
        epsS2_out = epsTop + (epsBot_out - epsTop) * y2_from_top / geom.h;

        // Stress in As2
        sigmaS2_out = SteelStress::CalculateStress(epsS2_out, steel);

        // Calculate required As2 for equilibrium
        if (std::abs(sigmaS2_out) > 1e-6) {
            As2_out = (loads.N - cf.Fc) / sigmaS2_out;
        }

        // Ensure non-negative area
        if (As2_out < 0.0) As2_out = 0.0;

        // Recalculate with actual As2
        epsBot_out = FindEpsBot(epsTop, As2_out);

        // Final forces with correct epsBot
        cf = ConcreteIntegration::CalculateForce(
            epsTop, epsBot_out, geom.b, geom.h, concrete
        );

        epsS2_out = epsTop + (epsBot_out - epsTop) * y2_from_top / geom.h;
        sigmaS2_out = SteelStress::CalculateStress(epsS2_out, steel);

        double Fs2 = As2_out * sigmaS2_out;

        // Moment about centroid
        // y2_center is distance from centroid (negative for bottom reinforcement)
        // Sign convention: positive moment causes tension at bottom
        double y2_center = geom.d2 - geom.h / 2.0;
        M_out = cf.Mc + Fs2 * (-y2_center);  // Note the negative sign
        N_out = cf.Fc + Fs2;
    }

public:
    ReinforcementDesigner(const SectionGeometry& g, const ConcreteProperties& c,
                         const SteelProperties& s, const DesignLoads& l)
        : geom(g), concrete(c), steel(s), loads(l) {}

    DesignResult Design() {
        DesignResult result = { false, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0 };

        // Regula falsi - iteration to find correct epsTop
        double epsTop_min = concrete.epsCu;  // start from ultimate compression
        double epsTop_max = steel.epsUd;     // end at ultimate tension

        const int maxIter = 50;
        const double tolRel = 0.01;  // 1%
        const double tolAbs = 100.0; // 100 Nm = 0.1 kNm

        // Calculate M for boundaries
        double As2_min, M_min, N_min, epsBot_min, epsS2_min, sigmaS2_min;
        CalculateForEpsTop(epsTop_min, As2_min, M_min, N_min, epsBot_min, epsS2_min, sigmaS2_min);

        double As2_max, M_max, N_max, epsBot_max, epsS2_max, sigmaS2_max;
        CalculateForEpsTop(epsTop_max, As2_max, M_max, N_max, epsBot_max, epsS2_max, sigmaS2_max);

        std::cout << std::fixed << std::setprecision(4);
        std::cout << "\nRegula Falsi Iteration:\n";
        std::cout << "Target moment M = " << loads.M / 1000.0 << " kNm\n";
        std::cout << "Initial bounds: M_min = " << M_min / 1000.0 << " kNm, M_max = " << M_max / 1000.0 << " kNm\n\n";

        // Check if target moment is within bounds
        if ((loads.M < M_min && loads.M < M_max) || (loads.M > M_min && loads.M > M_max)) {
            std::cout << "ERROR: Target moment is outside the feasible range!\n";
            std::cout << "Feasible range: [" << std::min(M_min, M_max) / 1000.0
                     << ", " << std::max(M_min, M_max) / 1000.0 << "] kNm\n";
            return result;
        }

        // Regula falsi iteration
        for (int iter = 0; iter < maxIter; iter++) {
            // Interpolation - regula falsi
            double epsTop_new;
            if (std::abs(M_max - M_min) < 1e-6) {
                epsTop_new = (epsTop_min + epsTop_max) / 2.0;
            }
            else {
                epsTop_new = epsTop_min + (epsTop_max - epsTop_min) *
                            (loads.M - M_min) / (M_max - M_min);
            }

            // Calculate for new epsTop
            double As2_new, M_new, N_new, epsBot_new, epsS2_new, sigmaS2_new;
            CalculateForEpsTop(epsTop_new, As2_new, M_new, N_new, epsBot_new, epsS2_new, sigmaS2_new);

            // Error
            double error_abs = std::abs(M_new - loads.M);
            double error_rel = (std::abs(loads.M) > 1e-6) ? error_abs / std::abs(loads.M) : 0.0;

            std::cout << "Iter " << std::setw(2) << iter + 1 << ": "
                     << "epsTop = " << std::setw(8) << epsTop_new * 1000.0 << " o/oo, "
                     << "M = " << std::setw(10) << M_new / 1000.0 << " kNm, "
                     << "As2 = " << std::setw(8) << As2_new * 10000.0 << " cm^2, "
                     << "error = " << std::setw(8) << error_rel * 100.0 << "%\n";

            // Check convergence
            if (error_abs < tolAbs || error_rel < tolRel) {
                result.converged = true;
                result.As2 = As2_new;
                result.epsTop = epsTop_new;
                result.epsBot = epsBot_new;
                result.epsS2 = epsS2_new;
                result.sigmaS2 = sigmaS2_new;
                result.N_calc = N_new;
                result.M_calc = M_new;
                result.errorAbs = error_abs;
                result.errorRel = error_rel;
                result.iterations = iter + 1;
                return result;
            }

            // Update bounds
            if ((M_new < loads.M && M_min < loads.M) || (M_new > loads.M && M_min > loads.M)) {
                epsTop_min = epsTop_new;
                M_min = M_new;
            }
            else {
                epsTop_max = epsTop_new;
                M_max = M_new;
            }
        }

        std::cout << "\nDID NOT CONVERGE after " << maxIter << " iterations!\n";
        return result;
    }
};

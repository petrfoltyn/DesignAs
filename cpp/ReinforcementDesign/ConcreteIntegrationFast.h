#pragma once
#include "MaterialProperties.h"
#include "ConcreteIntegration.h"  // Use ConcreteForces from original file
#include <cmath>
#include <algorithm>
#include <limits>

// Fast analytical concrete stress block integration (ported from C# FastConcreteNM)
class ConcreteIntegrationFast {
private:
    static constexpr double EC2 = -0.002;        // εc2 = -2‰
    static constexpr double INV_EC2 = -500.0;    // 1/εc2
    static constexpr double TOLERANCE = 1e-12;

    static inline bool IsNonZero(double val) { return std::abs(val) >= TOLERANCE; }
    static inline bool IsZero(double val) { return std::abs(val) < TOLERANCE; }
    static inline bool IsLess(double a, double b) { return a < b - TOLERANCE; }

public:
    /// <summary>
    /// Fast analytical calculation of concrete forces using EC2 parabolic-rectangular diagram
    /// Ported from C# FastConcreteNM
    /// </summary>
    /// <param name="b">Width [m]</param>
    /// <param name="h">Height [m]</param>
    /// <param name="k">Strain gradient (slope) [1/m]</param>
    /// <param name="q">Strain at centroid [-]</param>
    /// <param name="fcd">Design concrete strength [Pa] (negative for compression)</param>
    /// <returns>Concrete forces (N, M about centroid)</returns>
    static ConcreteForces FastConcreteNM(double b, double h, double k, double q, double fcd) {
        double h2 = 0.5 * h;
        double x1 = -h2;  // BOTTOM (in local coordinates, x=0 at centroid)
        double x2 = h2;   // TOP

        double x0, xEc2;

        // Calculate critical points
        if (IsNonZero(k)) {
            x0 = -q / k;                  // Point where ε = 0
            xEc2 = (EC2 - q) / k;         // Point where ε = εc2
        } else {
            // Constant strain case
            if (IsZero(q)) {
                x0 = 0.0;
            } else {
                x0 = (q > 0) ? -std::numeric_limits<double>::infinity()
                             : std::numeric_limits<double>::infinity();
            }
            xEc2 = std::numeric_limits<double>::infinity();
        }

        double N = 0.0;
        double M = 0.0;

        // Constant strain (k=0)
        if (IsZero(k)) {
            if (q >= 0) {
                // Tension or zero
                return { 0.0, 0.0 };
            } else if (q > EC2) {
                // Parabolic section
                double epsilonNorm = q / EC2;
                double sigma = fcd * (1.0 - (1.0 - epsilonNorm) * (1.0 - epsilonNorm));
                N = sigma * b * h;
                M = 0.0;
            } else {
                // Constant section
                N = fcd * b * h;
                M = 0.0;
            }
            return { N, M };
        }

        // SEGMENT 2: Parabolic compression section
        double xaPara = std::max(x1, std::min(xEc2, x0));
        double xbPara = std::min(x2, std::max(xEc2, x0));

        if (IsLess(xaPara, xbPara)) {
            double a = k * INV_EC2;
            double c = q * INV_EC2;

            double dx = xbPara - xaPara;
            double dx2 = xbPara * xbPara - xaPara * xaPara;
            double dx3 = (xbPara * xbPara * xbPara - xaPara * xaPara * xaPara) / 3.0;

            double nPara = fcd * b * (
                (2.0 * a - 2.0 * a * c) * dx2 * 0.5 +
                (2.0 * c - c * c) * dx -
                a * a * dx3
            );

            double xa2 = xaPara * xaPara;
            double xb2 = xbPara * xbPara;
            double xa4 = xa2 * xa2;
            double xb4 = xb2 * xb2;
            double dx4 = 0.25 * (xb4 - xa4);

            double mPara = fcd * b * (
                (2.0 * a - 2.0 * a * c) * dx3 +
                (2.0 * c - c * c) * dx2 * 0.5 -
                a * a * dx4
            );

            N += nPara;
            M += mPara;
        }

        // SEGMENT 3: Constant compression section (ε ≤ εc2)
        double xaConst, xbConst;

        if (std::abs(k) < 1e-10) {
            xaConst = xbConst = 0.0;
        } else if (k > 0) {
            // ε increases with x, greater compression (ε ≤ εc2) is for x ≤ xEc2
            xaConst = x1;
            xbConst = std::min(xEc2, x2);
        } else {
            // ε decreases with x, greater compression (ε ≤ εc2) is for x ≥ xEc2
            xaConst = std::max(xEc2, x1);
            xbConst = x2;
        }

        if (IsLess(xaConst, xbConst)) {
            double dx = xbConst - xaConst;
            double centroid = 0.5 * (xaConst + xbConst);
            double nConst = fcd * b * dx;
            double mConst = nConst * centroid;

            N += nConst;
            M += mConst;
        }

        return { N, M };
    }

    /// <summary>
    /// Calculate concrete forces given strain at top and bottom
    /// (Wrapper that converts to k,q parameterization and calls FastConcreteNM)
    /// </summary>
    static ConcreteForces CalculateForce(
        double epsTop,
        double epsBot,
        double b,
        double h,
        const ConcreteProperties& props
    ) {
        // Convert epsTop, epsBot to k, q parameterization
        // Local coordinates: x=0 at centroid, x=+h/2 at top, x=-h/2 at bottom
        // Strain: ε(x) = k*x + q

        // At top (x = h/2): epsTop = k*(h/2) + q
        // At bot (x = -h/2): epsBot = k*(-h/2) + q

        // Solve:
        // epsTop - epsBot = k*h
        // q = (epsTop + epsBot) / 2

        double k = (epsTop - epsBot) / h;
        double q = (epsTop + epsBot) / 2.0;

        auto result = FastConcreteNM(b, h, k, q, props.fcd);

        // IMPORTANT: Negate moment for consistency with C++ sign convention
        // C++ numericalintegration uses: momentSum += dF * (-yFromCenter)
        // C# FastConcreteNM uses local coordinates with opposite sign
        result.Mc = -result.Mc;

        return result;
    }
};

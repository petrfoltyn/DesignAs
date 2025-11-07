#pragma once
#include "MaterialProperties.h"
#include <cmath>
#include <algorithm>

// Concrete force resultants
struct ConcreteForces {
    double Fc;       // [N] resultant compressive force
    double Mc;       // [Nm] moment from concrete (about centroid)
};

// Parabolic-rectangular concrete stress block integration
class ConcreteIntegration {
public:
    static ConcreteForces CalculateForce(
        double epsTop,
        double epsBot,
        double b,
        double h,
        const ConcreteProperties& props
    ) {
        const int n = 100; // number of segments for numerical integration
        double dy = h / n;

        double Fc = 0.0;
        double momentSum = 0.0;

        // Integration from bottom to top (y from 0 to h, y=0 is bottom)
        for (int i = 0; i < n; i++) {
            double y = i * dy + dy / 2.0;  // segment center

            // Linear strain distribution
            double eps = epsBot + (epsTop - epsBot) * y / h;

            // Parabolic-rectangular concrete diagram
            double sigma = 0.0;
            if (eps < 0) {  // compression
                if (eps >= props.epsC2) {
                    // Parabolic part
                    sigma = props.fcd * (1.0 - std::pow(1.0 - eps / props.epsC2, 2));
                }
                else {
                    // Constant part
                    sigma = props.fcd;
                }
            }
            // tension: sigma = 0

            double dF = sigma * b * dy;
            Fc += dF;

            // Moment about centroid (y=h/2)
            // Note: Using sign convention where positive moment causes tension at bottom
            double yFromCenter = y - h / 2.0;
            momentSum += dF * (-yFromCenter);  // Negative sign for correct moment convention
        }

        return { Fc, momentSum };
    }
};

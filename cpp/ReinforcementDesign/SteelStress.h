#pragma once
#include "MaterialProperties.h"
#include <algorithm>

// Bilinear steel stress-strain model
class SteelStress {
public:
    static double CalculateStress(double eps, const SteelProperties& props) {
        double epsY = props.fyd / props.Es;

        if (eps >= epsY) {
            return props.fyd;
        }
        else if (eps <= -epsY) {
            return -props.fyd;
        }
        else {
            return eps * props.Es;
        }
    }
};

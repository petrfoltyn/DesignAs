#include <iostream>
#include <iomanip>
#include "MaterialProperties.h"
#include "ReinforcementDesigner.h"

int main() {
    std::cout << "==========================================================\n";
    std::cout << "  REINFORCEMENT DESIGN FOR RC CROSS-SECTION (Variant 2)\n";
    std::cout << "  As1 = 0, As2 variable (bottom edge)\n";
    std::cout << "==========================================================\n\n";

    // Input parameters - DEFAULT VALUES
    SectionGeometry geom;
    ConcreteProperties concrete;
    SteelProperties steel;
    DesignLoads loads;

    // Geometry [m]
    geom.b = 0.3;
    geom.h = 0.5;
    geom.d2 = 0.05;
    geom.d1 = 0.05; // top cover (not used in Variant 2)

    // Concrete properties
    double fcd_input = 20.0;
    concrete.fcd = -fcd_input * 1e6; // convert to Pa, negative for compression

    double epsC2_input = 2.0;
    concrete.epsC2 = -epsC2_input / 1000.0; // convert to absolute, negative

    double epsCu_input = 3.5;
    concrete.epsCu = -epsCu_input / 1000.0;

    // Steel properties
    double fyd_input = 435.0;
    steel.fyd = fyd_input * 1e6; // convert to Pa

    double Es_input = 200.0;
    steel.Es = Es_input * 1e9; // convert to Pa

    double epsUd_input = 10.0;
    steel.epsUd = epsUd_input / 1000.0; // convert to absolute

    // Design loads
    double N_input = 0.0;
    loads.N = N_input * 1000.0; // convert to N

    double M_input = 30.0;
    loads.M = M_input * 1000.0; // convert to Nm

    // Print input values
    std::cout << "INPUT PARAMETERS:\n";
    std::cout << "-----------------------------------\n";
    std::cout << "Cross-section geometry:\n";
    std::cout << "  b  = " << geom.b << " m\n";
    std::cout << "  h  = " << geom.h << " m\n";
    std::cout << "  d2 = " << geom.d2 << " m\n\n";

    std::cout << "Concrete properties:\n";
    std::cout << "  fcd = " << fcd_input << " MPa\n";
    std::cout << "  ec2 = " << epsC2_input << " per mille\n";
    std::cout << "  ecu = " << epsCu_input << " per mille\n\n";

    std::cout << "Steel properties:\n";
    std::cout << "  fyd = " << fyd_input << " MPa\n";
    std::cout << "  Es  = " << Es_input << " GPa\n";
    std::cout << "  eud = " << epsUd_input << " per mille\n\n";

    std::cout << "Design loads:\n";
    std::cout << "  N = " << N_input << " kN\n";
    std::cout << "  M = " << M_input << " kNm\n";

    // Reinforcement design
    std::cout << "\n==========================================================\n";
    ReinforcementDesigner designer(geom, concrete, steel, loads);
    DesignResult result = designer.Design();

    // Output results
    std::cout << "\n==========================================================\n";
    std::cout << "  DESIGN RESULTS\n";
    std::cout << "==========================================================\n";

    if (result.converged) {
        std::cout << std::fixed << std::setprecision(2);
        std::cout << "\n[OK] Design CONVERGED after " << result.iterations << " iterations\n\n";

        std::cout << "REQUIRED REINFORCEMENT:\n";
        std::cout << "  As2 = " << result.As2 * 10000.0 << " cm^2\n\n";

        std::cout << "STRAINS:\n";
        std::cout << "  e_top    = " << std::setw(8) << result.epsTop * 1000.0 << " per mille\n";
        std::cout << "  e_bottom = " << std::setw(8) << result.epsBot * 1000.0 << " per mille\n";
        std::cout << "  e_s2     = " << std::setw(8) << result.epsS2 * 1000.0 << " per mille\n\n";

        std::cout << "STRESSES:\n";
        std::cout << "  sig_s2 = " << std::setw(8) << result.sigmaS2 / 1e6 << " MPa\n\n";

        std::cout << "EQUILIBRIUM CHECK:\n";
        std::cout << "  N required   = " << std::setw(10) << loads.N / 1000.0 << " kN\n";
        std::cout << "  N calculated = " << std::setw(10) << result.N_calc / 1000.0 << " kN\n";
        std::cout << "  M required   = " << std::setw(10) << loads.M / 1000.0 << " kNm\n";
        std::cout << "  M calculated = " << std::setw(10) << result.M_calc / 1000.0 << " kNm\n\n";

        std::cout << "ACCURACY:\n";
        std::cout << "  Absolute error  = " << result.errorAbs / 1000.0 << " kNm\n";
        std::cout << "  Relative error  = " << result.errorRel * 100.0 << " %\n";

        if (result.errorRel < 0.001) {
            std::cout << "  [OK] Excellent accuracy!\n";
        }
        else if (result.errorRel < 0.01) {
            std::cout << "  [OK] Good accuracy\n";
        }
    }
    else {
        std::cout << "\n[ERROR] Design DID NOT CONVERGE!\n";
        std::cout << "Try adjusting input parameters.\n";
    }

    std::cout << "\n==========================================================\n";
    std::cout << "\nPress Enter to exit...";
    std::cin.get();

    return 0;
}

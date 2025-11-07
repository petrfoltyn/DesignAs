#include <iostream>
#include <iomanip>
#include <cmath>
#include "MaterialProperties.h"
#include "ConcreteIntegration.h"
#include "ConcreteIntegrationFast.h"
#include "PerformanceTimer.h"

int main() {
    std::cout << "==========================================================\n";
    std::cout << "  CONCRETE INTEGRATION COMPARISON TEST\n";
    std::cout << "  Numerical (100 segments) vs. Analytical (closed-form)\n";
    std::cout << "==========================================================\n\n";

    // Setup test case
    SectionGeometry geom;
    geom.b = 0.3;
    geom.h = 0.5;
    geom.d1 = 0.05;
    geom.d2 = 0.05;

    ConcreteProperties concrete;
    concrete.fcd = -20.0e6;    // -20 MPa
    concrete.epsC2 = -0.002;
    concrete.epsCu = -0.0035;

    // Test cases
    struct TestCase {
        std::string name;
        double epsTop;
        double epsBot;
    };

    std::vector<TestCase> testCases = {
        {"Pure compression", -0.0035, -0.0035},
        {"Balanced (εtop=εcu, εbot=0)", -0.0035, 0.0},
        {"Small bending", -0.002, -0.001},
        {"Typical bending", -0.003, 0.002},
        {"Large bending", -0.0035, 0.010},
        {"Tension dominant", -0.001, 0.005},
        {"Nearly pure tension", 0.0, 0.010}
    };

    std::cout << "Test geometry: b=" << geom.b << "m, h=" << geom.h << "m\n";
    std::cout << "Concrete: fcd=" << concrete.fcd/1e6 << " MPa, ec2=" << concrete.epsC2*1000 << " o/oo\n\n";

    // Run tests
    std::cout << std::fixed << std::setprecision(6);
    std::cout << "TEST RESULTS:\n";
    std::cout << std::string(100, '-') << "\n";
    std::cout << std::setw(25) << std::left << "Test Case"
              << std::setw(12) << "Fc_num[kN]"
              << std::setw(12) << "Fc_fast[kN]"
              << std::setw(12) << "Diff[%]"
              << std::setw(12) << "Mc_num[kNm]"
              << std::setw(12) << "Mc_fast[kNm]"
              << std::setw(12) << "Diff[%]"
              << "\n";
    std::cout << std::string(100, '-') << "\n";

    double maxDiffN = 0.0;
    double maxDiffM = 0.0;

    for (const auto& tc : testCases) {
        // Numerical integration (existing)
        ConcreteForces cfNum = ConcreteIntegration::CalculateForce(
            tc.epsTop, tc.epsBot, geom.b, geom.h, concrete
        );

        // Analytical integration (new)
        ConcreteForces cfFast = ConcreteIntegrationFast::CalculateForce(
            tc.epsTop, tc.epsBot, geom.b, geom.h, concrete
        );

        // Calculate differences
        double diffN = 0.0;
        double diffM = 0.0;

        if (std::abs(cfNum.Fc) > 1e-6) {
            diffN = std::abs((cfFast.Fc - cfNum.Fc) / cfNum.Fc) * 100.0;
        }
        if (std::abs(cfNum.Mc) > 1e-6) {
            diffM = std::abs((cfFast.Mc - cfNum.Mc) / cfNum.Mc) * 100.0;
        }

        maxDiffN = std::max(maxDiffN, diffN);
        maxDiffM = std::max(maxDiffM, diffM);

        std::cout << std::setw(25) << std::left << tc.name
                  << std::setw(12) << std::right << cfNum.Fc / 1000.0
                  << std::setw(12) << cfFast.Fc / 1000.0
                  << std::setw(12) << diffN
                  << std::setw(12) << cfNum.Mc / 1000.0
                  << std::setw(12) << cfFast.Mc / 1000.0
                  << std::setw(12) << diffM
                  << "\n";
    }

    std::cout << std::string(100, '-') << "\n";
    std::cout << "Maximum difference - N: " << maxDiffN << " %\n";
    std::cout << "Maximum difference - M: " << maxDiffM << " %\n\n";

    // Performance comparison
    std::cout << "\n==========================================================\n";
    std::cout << "  PERFORMANCE COMPARISON\n";
    std::cout << "==========================================================\n\n";

    PerformanceTimer timer(false);  // Disable auto-logging for cleaner output

    const int iterations = 10000;
    std::cout << "Running " << iterations << " integrations with each method...\n\n";

    // Use typical bending case
    double epsTop = -0.003;
    double epsBot = 0.002;

    // Benchmark numerical integration
    timer.Start("Numerical_10000");
    for (int i = 0; i < iterations; i++) {
        ConcreteForces cf = ConcreteIntegration::CalculateForce(
            epsTop, epsBot, geom.b, geom.h, concrete
        );
    }
    double timeNum = timer.Stop();

    // Benchmark analytical integration
    timer.Start("Analytical_10000");
    for (int i = 0; i < iterations; i++) {
        ConcreteForces cf = ConcreteIntegrationFast::CalculateForce(
            epsTop, epsBot, geom.b, geom.h, concrete
        );
    }
    double timeFast = timer.Stop();

    std::cout << std::fixed << std::setprecision(3);
    std::cout << "Numerical (100 segments):  " << timeNum << " ms (" << (timeNum/iterations) << " ms per call)\n";
    std::cout << "Analytical (closed-form):  " << timeFast << " ms (" << (timeFast/iterations) << " ms per call)\n";
    std::cout << "\nSpeedup: " << (timeNum / timeFast) << "x faster\n";
    std::cout << "Time saved per 1000 calls: " << (timeNum - timeFast) << " ms\n\n";

    // Summary
    std::cout << "==========================================================\n";
    std::cout << "  SUMMARY\n";
    std::cout << "==========================================================\n\n";

    if (maxDiffN < 0.1 && maxDiffM < 0.1) {
        std::cout << "[OK] Analytical method matches numerical method within 0.1%\n";
    } else if (maxDiffN < 1.0 && maxDiffM < 1.0) {
        std::cout << "[OK] Analytical method matches numerical method within 1%\n";
    } else {
        std::cout << "[WARNING] Differences exceed 1% - review implementation\n";
    }

    std::cout << "\nRecommendation:\n";
    std::cout << "  Replace ConcreteIntegration with ConcreteIntegrationFast\n";
    std::cout << "  - " << (timeNum / timeFast) << "x faster\n";
    std::cout << "  - Exact result (no numerical error)\n";
    std::cout << "  - Maximum error: " << std::max(maxDiffN, maxDiffM) << "%\n";

    std::cout << "\n==========================================================\n";
    std::cout << "\nPress Enter to exit...";
    std::cin.get();

    return 0;
}

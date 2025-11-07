#include <iostream>
#include <iomanip>
#include "MaterialProperties.h"
#include "ReinforcementDesigner.h"
#include "InteractionDiagram.h"
#include "PerformanceTimer.h"

int main() {
    // Create performance timer
    PerformanceTimer timer(true);  // Enable auto-logging

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

    // ========== INTERACTION DIAGRAM GENERATION ==========
    std::cout << "\n==========================================================\n";
    std::cout << "  GENERATING INTERACTION DIAGRAM\n";
    std::cout << "==========================================================\n\n";

    // Generate concrete-only diagram
    std::cout << "Generating concrete-only diagram...\n";
    timer.Start("ConcreteOnlyDiagramGeneration");
    InteractionDiagram diagramConcrete(geom, concrete, steel, 0.0, 0.0);
    auto pointsConcrete = diagramConcrete.Generate(10);  // 10 interpolation points between characteristic points
    timer.Stop("8 characteristic + 70 interpolated points");

    timer.Start("ConcreteOnlyDiagramExportCSV");
    InteractionDiagram::ExportToCSV(pointsConcrete, "interaction_diagram_concrete_only.csv");
    timer.Stop();

    // Generate diagram with reinforcement As2 = 10 cm^2
    std::cout << "\nGenerating diagram with reinforcement (As1=0, As2=10 cm^2)...\n";
    timer.Start("WithReinforcementDiagramGeneration");
    double As2_diagram = 10.0 / 10000.0;  // 10 cm^2 to m^2
    InteractionDiagram diagramWithReinf(geom, concrete, steel, 0.0, As2_diagram);
    auto pointsWithReinf = diagramWithReinf.Generate(10);
    timer.Stop("As2=10 cm^2, 78 total points");

    timer.Start("WithReinforcementDiagramExportCSV");
    InteractionDiagram::ExportToCSV(pointsWithReinf, "interaction_diagram_with_reinforcement.csv");
    timer.Stop();

    // ========== REINFORCEMENT DESIGN FOR SPECIFIC LOAD ==========
    std::cout << "\n==========================================================\n";
    std::cout << "  REINFORCEMENT DESIGN FOR SPECIFIC LOADS\n";
    std::cout << "==========================================================\n";

    // Create designer - generates interaction diagram once
    timer.Start("DesignerInitialization");
    ReinforcementDesigner designer(geom, concrete, steel, 10);
    timer.Stop("Generate diagram once, reuse for all designs");

    // Design for first load case
    timer.Start("Design_LoadCase1_N0_M30");
    DesignResult result = designer.Design(loads);
    timer.Stop("N=0, M=30 kNm");

    // Example: Design for additional load cases (reusing same diagram)
    std::cout << "\n\n==========================================================\n";
    std::cout << "  ADDITIONAL LOAD CASES (using same diagram)\n";
    std::cout << "==========================================================\n";

    // Load case 2: Higher moment
    timer.Start("Design_LoadCase2_N0_M50");
    DesignLoads loads2;
    loads2.N = 0.0;
    loads2.M = 50000.0;  // 50 kNm
    DesignResult result2 = designer.Design(loads2);
    timer.Stop("N=0, M=50 kNm");

    // Load case 3: Compression + moment
    timer.Start("Design_LoadCase3_N-100_M30");
    DesignLoads loads3;
    loads3.N = -100000.0;  // -100 kN (compression)
    loads3.M = 30000.0;    // 30 kNm
    DesignResult result3 = designer.Design(loads3);
    timer.Stop("N=-100 kN, M=30 kNm");

    // Output summary of all results
    std::cout << "\n==========================================================\n";
    std::cout << "  DESIGN RESULTS SUMMARY\n";
    std::cout << "==========================================================\n\n";

    auto printResult = [](const std::string& caseName, const DesignResult& res, const DesignLoads& lds) {
        std::cout << caseName << ":\n";
        if (res.converged) {
            std::cout << std::fixed << std::setprecision(2);
            std::cout << "  [OK] Design successful\n";
            std::cout << "  Loads:  N = " << lds.N / 1000.0 << " kN, M = " << lds.M / 1000.0 << " kNm\n";
            std::cout << "  As2 = " << res.As2 * 10000.0 << " cm^2\n";
            std::cout << "  Strains: e_top = " << res.epsTop * 1000.0 << " o/oo, e_bot = " << res.epsBot * 1000.0 << " o/oo\n";
            std::cout << "  Stress: sig_s2 = " << res.sigmaS2 / 1e6 << " MPa\n";
            std::cout << "  Error: " << res.errorRel * 100.0 << " %\n";
        } else {
            std::cout << "  [ERROR] Design failed\n";
        }
        std::cout << "\n";
    };

    printResult("Load Case 1 (N=0, M=30 kNm)", result, loads);
    printResult("Load Case 2 (N=0, M=50 kNm)", result2, loads2);
    printResult("Load Case 3 (N=-100 kN, M=30 kNm)", result3, loads3);

    std::cout << "\n==========================================================\n";

    // ========== BENCHMARK: 1000 LOAD CASES ==========
    std::cout << "\n==========================================================\n";
    std::cout << "  BENCHMARK: 1 DIAGRAM + 1000 LOAD CASES\n";
    std::cout << "==========================================================\n\n";

    // Generate 1000 random load combinations within feasible range
    std::vector<DesignLoads> batchLoads;
    batchLoads.reserve(1000);

    // Create varied load cases:
    // - Pure bending: N=0, M varying
    // - Compression + bending: N<0, M varying
    // - Small tension + bending: N>0 (small), M varying

    for (int i = 0; i < 1000; i++) {
        DesignLoads ld;

        if (i < 400) {
            // Pure bending cases (40%)
            ld.N = 0.0;
            ld.M = 10000.0 + (i * 100.0);  // 10 to 50 kNm
        } else if (i < 800) {
            // Compression + bending cases (40%)
            ld.N = -50000.0 - ((i - 400) * 250.0);  // -50 to -150 kN
            ld.M = 15000.0 + ((i - 400) * 75.0);    // 15 to 45 kNm
        } else {
            // Small tension + bending cases (20%)
            ld.N = 10000.0 + ((i - 800) * 100.0);   // 10 to 30 kN
            ld.M = 20000.0 + ((i - 800) * 50.0);    // 20 to 30 kNm
        }

        batchLoads.push_back(ld);
    }

    std::cout << "Testing " << batchLoads.size() << " load combinations...\n\n";

    // Measure only the design operations (diagram already generated in designer)
    timer.Start("Batch_1000_Designs");
    int successCount = 0;
    int failCount = 0;

    for (const auto& ld : batchLoads) {
        DesignResult res = designer.Design(ld, false);  // verbose = false for batch processing
        if (res.converged) {
            successCount++;
        } else {
            failCount++;
        }
    }

    double batchTime = timer.Stop("1000 N,M combinations");

    std::cout << "\nBatch results:\n";
    std::cout << "  Successful designs: " << successCount << " / " << batchLoads.size() << "\n";
    std::cout << "  Failed designs: " << failCount << " / " << batchLoads.size() << "\n";
    std::cout << "  Total time: " << std::fixed << std::setprecision(3) << batchTime << " ms\n";
    std::cout << "  Average per design: " << (batchTime / batchLoads.size()) << " ms\n";
    std::cout << "  Designs per second: " << (1000.0 / batchTime * 1000.0) << "\n";

    std::cout << "\n==========================================================\n";

    // ========== PERFORMANCE ANALYSIS ==========
    timer.PrintSummary();
    timer.Analyze();
    timer.ExportToCSV("performance_results.csv");

    std::cout << "\nPress Enter to exit...";
    std::cin.get();

    return 0;
}

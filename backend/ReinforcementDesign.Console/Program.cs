using ReinforcementDesign;
using System.Globalization;

Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine("  VÝPOČET INTERAKČNÍHO DIAGRAMU N-M PRO ŽELEZOBETONOVÝ PRŮŘEZ");
Console.WriteLine("  Design podle Eurokódu EC2 - Parabolicko-obdélníkový diagram");
Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine();

// Nastavení kultury pro desetinnou tečku
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Režim natvrdo nastaven na "pouze beton" (false = s výztuží, Varianta 2)
bool concreteOnly = false;

Console.WriteLine("REŽIM: S VÝZTUŽÍ (VARIANTA 2)");
Console.WriteLine("────────────────────────────────────────────────────────────────────");
Console.WriteLine("Výpočet interakčního diagramu s návrhovou výztuží");
Console.WriteLine("(Varianta 2: pouze dolní výztuž, As1 = 0)");
Console.WriteLine();

// ===================================================================
// ZADÁNÍ PARAMETRŮ
// ===================================================================

// Geometrie průřezu
var geometry = new CrossSectionGeometry
{
    B = 0.3,              // 300 mm
    H = 0.5,              // 500 mm
    Layer1Distance = 0.05, // 50 mm od horního okraje
    Layer2YPos = 0.05      // 50 mm od dolního okraje
};

// Vlastnosti betonu
var concrete = new ConcreteProperties
{
    Fcd = -20e6,      // -20 MPa
    EpsC2 = -0.002,   // -2‰
    EpsCu = -0.0035   // -3.5‰
};

// Vlastnosti výztuže
var steel = new SteelProperties
{
    Fyd = 435e6,      // 435 MPa
    Es = 200e9,       // 200 GPa
    EpsUd = 0.01      // 10‰
};

// Návrhové zatížení
double N_design = 0;    // kN (kladná = tah, záporná = tlak)
double M_design = 30;   // kNm

Console.WriteLine("ZADÁNÍ:");
Console.WriteLine("────────────────────────────────────────────────────────────────────");
Console.WriteLine($"Geometrie:      b = {geometry.B * 1000:F0} mm, h = {geometry.H * 1000:F0} mm");
Console.WriteLine($"Výztuž:         y1 = {geometry.Y1 * 1000:F0} mm, y2 = {geometry.Y2 * 1000:F0} mm");
Console.WriteLine($"Beton:          fcd = {Math.Abs(concrete.Fcd) / 1e6:F1} MPa");
Console.WriteLine($"Ocel:           fyd = {steel.Fyd / 1e6:F0} MPa, Es = {steel.Es / 1e9:F0} GPa");
Console.WriteLine($"Návrhové síly:  N = {N_design:F1} kN, M = {M_design:F1} kNm");
Console.WriteLine();

// ===================================================================
// VÝPOČET INTERAKČNÍHO DIAGRAMU
// ===================================================================

// Zahuštění pro každý interval (mezi charakteristickými body)
// 9 charakteristických bodů -> 8 intervalů
int[] densities = new int[]
{
    5,  // Bod 1 -> Bod 2
    5,  // Bod 2 -> Bod 2b
    5,  // Bod 2b -> Bod 3
    10, // Bod 3 -> Bod 4 (oblast s velkou změnou)
    10, // Bod 4 -> Bod 5 (oblast s velkou změnou)
    5,  // Bod 5 -> Bod 6
    5,  // Bod 6 -> Bod 7
    5   // Bod 7 -> Bod 8
};

Console.WriteLine("VÝPOČET INTERAKČNÍHO DIAGRAMU:");
Console.WriteLine("────────────────────────────────────────────────────────────────────");
Console.Write("Počítám body diagramu");

if (concreteOnly)
{
    // REŽIM: POUZE BETON
    var diagramConcrete = new ConcreteDiagram(geometry, concrete, steel);
    var pointsConcrete = diagramConcrete.Calculate(densities);

    Console.WriteLine($" ✓ ({pointsConcrete.Count} bodů)");
    Console.WriteLine();

    // ===================================================================
    // VÝPIS VÝSLEDKŮ - POUZE BETON
    // ===================================================================

    Console.WriteLine("CHARAKTERISTICKÉ BODY (POUZE BETON):");
    Console.WriteLine("────────────────────────────────────────────────────────────────────");

    // Najít pouze hlavní body (ne interpolované)
    var mainPointsConcrete = pointsConcrete.Where(p => p.Name.StartsWith("Bod ")).ToList();

    foreach (var point in mainPointsConcrete)
    {
        Console.WriteLine(point.ToString());
    }

    Console.WriteLine();

    // ===================================================================
    // EXPORT DO CSV - POUZE BETON
    // ===================================================================

    string csvPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "interaction_diagram_concrete_only.csv");

    try
    {
        using var writer = new StreamWriter(csvPath);
        writer.WriteLine(ConcretePoint.CsvHeader);

        foreach (var point in pointsConcrete)
        {
            writer.WriteLine(point.ToCsv());
        }

        Console.WriteLine($"✓ Data exportována do CSV: {csvPath}");
        Console.WriteLine($"  Celkem {pointsConcrete.Count} bodů");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Chyba při exportu CSV: {ex.Message}");
    }

    Console.WriteLine();

    // ===================================================================
    // STATISTIKY - POUZE BETON
    // ===================================================================

    Console.WriteLine("STATISTIKY:");
    Console.WriteLine("────────────────────────────────────────────────────────────────────");

    // Najít extrémní hodnoty
    var maxN = pointsConcrete.MaxBy(p => p.N);
    var minN = pointsConcrete.MinBy(p => p.N);
    var maxM = pointsConcrete.MaxBy(p => Math.Abs(p.M));

    Console.WriteLine($"Maximální normálová síla:");
    Console.WriteLine($"  N_max = {maxN?.N:F2} kN při {maxN?.Name}");
    Console.WriteLine($"  (k = {maxN?.K:F6}, q = {maxN?.Q:F6})");
    Console.WriteLine();

    Console.WriteLine($"Minimální normálová síla:");
    Console.WriteLine($"  N_min = {minN?.N:F2} kN při {minN?.Name}");
    Console.WriteLine($"  (k = {minN?.K:F6}, q = {minN?.Q:F6})");
    Console.WriteLine();

    Console.WriteLine($"Maximální moment:");
    Console.WriteLine($"  |M_max| = {Math.Abs(maxM?.M ?? 0):F2} kNm při {maxM?.Name}");
    Console.WriteLine($"  (N = {maxM?.N:F2} kN)");
    Console.WriteLine();
}
else
{
    // REŽIM: S VÝZTUŽÍ
    var diagram = new InteractionDiagram(geometry, concrete, steel);
    diagram.SetDesignLoads(N_design, M_design);
    var points = diagram.Calculate(densities);

    Console.WriteLine($" ✓ ({points.Count} bodů)");
    Console.WriteLine();

    // ===================================================================
    // VÝPIS VÝSLEDKŮ - S VÝZTUŽÍ
    // ===================================================================

    Console.WriteLine("CHARAKTERISTICKÉ BODY (VARIANTA 2: pouze dolní výztuž):");
    Console.WriteLine("────────────────────────────────────────────────────────────────────");

    // Najít pouze hlavní body (ne interpolované)
    var mainPoints = points.Where(p => p.Name.StartsWith("Bod ")).ToList();

    foreach (var point in mainPoints)
    {
        Console.WriteLine(point.ToString());
    }

    Console.WriteLine();

    // ===================================================================
    // EXPORT DO CSV
    // ===================================================================

    string csvPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "interaction_diagram.csv");

    try
    {
        using var writer = new StreamWriter(csvPath);
        writer.WriteLine(InteractionPoint.CsvHeader);

        foreach (var point in points)
        {
            writer.WriteLine(point.ToCsv());
        }

        Console.WriteLine($"✓ Data exportována do CSV: {csvPath}");
        Console.WriteLine($"  Celkem {points.Count} bodů");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Chyba při exportu CSV: {ex.Message}");
    }

    Console.WriteLine();

    // ===================================================================
    // STATISTIKY A ANALÝZA
    // ===================================================================

    Console.WriteLine("STATISTIKY:");
    Console.WriteLine("────────────────────────────────────────────────────────────────────");

    // Najít extrémní hodnoty
    var maxN = points.MaxBy(p => p.N);
    var minN = points.MinBy(p => p.N);
    var maxM = points.MaxBy(p => Math.Abs(p.M));

    Console.WriteLine($"Maximální normálová síla:");
    Console.WriteLine($"  N_max = {maxN?.N:F2} kN při {maxN?.Name}");
    Console.WriteLine($"  (εtop = {maxN?.EpsTop:F2}‰, εbottom = {maxN?.EpsBottom:F2}‰)");
    Console.WriteLine();

    Console.WriteLine($"Minimální normálová síla:");
    Console.WriteLine($"  N_min = {minN?.N:F2} kN při {minN?.Name}");
    Console.WriteLine($"  (εtop = {minN?.EpsTop:F2}‰, εbottom = {minN?.EpsBottom:F2}‰)");
    Console.WriteLine();

    Console.WriteLine($"Maximální moment:");
    Console.WriteLine($"  |M_max| = {Math.Abs(maxM?.M ?? 0):F2} kNm při {maxM?.Name}");
    Console.WriteLine($"  (N = {maxM?.N:F2} kN)");
    Console.WriteLine();

    // ===================================================================
    // PŘÍKLAD VÝPOČTU PRO BOD 4
    // ===================================================================

    Console.WriteLine("PŘÍKLAD VÝPOČTU (pro Bod 4):");
    Console.WriteLine("────────────────────────────────────────────────────────────────────");

    var bod4 = points.FirstOrDefault(p => p.Name == "Bod 4");
    if (bod4 != null)
    {
        Console.WriteLine($"Stav přetvoření: εtop = {bod4.EpsTop:F2}‰, εbottom = {bod4.EpsBottom:F2}‰");
        Console.WriteLine($"                 εs1 = {bod4.EpsS1:F2}‰, εs2 = {bod4.EpsS2:F2}‰");
        Console.WriteLine();

        Console.WriteLine($"Síly od betonu:  Fc = {bod4.Fc:F2} kN, Mc = {bod4.Mc:F2} kNm");
        Console.WriteLine($"Síla od výztuže: Fs2 = {bod4.Fs2:F2} kN");
        Console.WriteLine();

        Console.WriteLine($"Celkové síly:    N = {bod4.N:F2} kN, M = {bod4.M:F2} kNm");
        Console.WriteLine();

        Console.WriteLine("VARIANTA 2: Pouze dolní výztuž (As1 = 0)");
        Console.WriteLine($"  As2 = {bod4.As2:F2} cm²");
        Console.WriteLine($"  Md = {bod4.Md:F2} kNm (možný moment pro danou normálovou sílu N = {N_design:F1} kN)");
    }
}

Console.WriteLine();

// ===================================================================
// TEST ITERAČNÍHO ALGORITMU - FindDesignPoint
// ===================================================================

Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine("  TEST: Iterační hledání návrhového bodu");
Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine();

try
{
    var diagram = new InteractionDiagram(geometry, concrete, steel);

    Console.WriteLine($"Hledám bod pro N = {N_design:F1} kN, M = {M_design:F1} kNm");
    Console.WriteLine($"Tolerance: relativní = 1%, absolutní = 0.1 kN (100 N)");
    Console.WriteLine();

    var designPoint = diagram.FindDesignPoint(
        N_design,
        M_design,
        toleranceRel: 0.01,
        toleranceAbs: 0.1,
        maxIterations: 50);

    Console.WriteLine("VÝSLEDEK:");
    Console.WriteLine("────────────────────────────────────────────────────────────────────");
    Console.WriteLine(designPoint.ToString());
    Console.WriteLine();

    Console.WriteLine("KONTROLA:");
    Console.WriteLine($"  Požadováno:  N = {N_design:F2} kN, M = {M_design:F2} kNm");
    Console.WriteLine($"  Vypočteno:   N = {designPoint.N:F2} kN, M = {designPoint.M:F2} kNm");
    Console.WriteLine($"  Chyba M:     ΔM = {Math.Abs(designPoint.M - M_design):F4} kNm");
    Console.WriteLine($"  Chyba M (%): {Math.Abs(designPoint.M - M_design) / Math.Abs(M_design) * 100:F2}%");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Chyba: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine("  VÝPOČET DOKONČEN");
Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine();

// Demo ReinforcementCalculator
ReinforcementCalculatorDemo.RunDemo();

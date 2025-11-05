using ReinforcementDesign;
using System.Globalization;

Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine("  VÝPOČET INTERAKČNÍHO DIAGRAMU N-M PRO ŽELEZOBETONOVÝ PRŮŘEZ");
Console.WriteLine("  Design podle Eurokódu EC2 - Parabolicko-obdélníkový diagram");
Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine();

// Nastavení kultury pro desetinnou tečku
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

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

var diagram = new InteractionDiagram(geometry, concrete, steel);
diagram.SetDesignLoads(N_design, M_design);

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

var points = diagram.Calculate(densities);

Console.WriteLine($" ✓ ({points.Count} bodů)");
Console.WriteLine();

// ===================================================================
// VÝPIS VÝSLEDKŮ - KONZOLE
// ===================================================================

Console.WriteLine("CHARAKTERISTICKÉ BODY:");
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
// POROVNÁNÍ TŘÍ METOD NÁVRHU
// ===================================================================

Console.WriteLine("POROVNÁNÍ METOD NÁVRHU (pro Bod 4):");
Console.WriteLine("────────────────────────────────────────────────────────────────────");

var bod4 = points.FirstOrDefault(p => p.Name == "Bod 4");
if (bod4 != null)
{
    Console.WriteLine($"Stav přetvoření: εtop = {bod4.EpsTop:F2}‰, εbottom = {bod4.EpsBottom:F2}‰");
    Console.WriteLine($"Vnitřní síly:    N = {bod4.N:F2} kN, M = {bod4.M:F2} kNm");
    Console.WriteLine();

    Console.WriteLine("Metoda 1: Optimální As1 a As2");
    Console.WriteLine($"  As1 = {bod4.As1:F2} cm²");
    Console.WriteLine($"  As2 = {bod4.As2:F2} cm²");
    Console.WriteLine($"  Celkem: {bod4.As1 + bod4.As2:F2} cm²");
    Console.WriteLine();

    Console.WriteLine("Metoda 2: Pouze dolní výztuž (As1 = 0)");
    Console.WriteLine($"  As = {bod4.As:F2} cm²");
    Console.WriteLine($"  Md = {bod4.Md:F2} kNm");
    Console.WriteLine();

    Console.WriteLine("Metoda 3: Rovnoměrné rozložení (As1 = As2 = Astot/2)");
    Console.WriteLine($"  Astot = {bod4.Astot:F2} cm² (po {bod4.Astot / 2:F2} cm² v každé vrstvě)");
    Console.WriteLine($"  Mdtot = {bod4.Mdtot:F2} kNm");
}

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════════════════");
Console.WriteLine("  VÝPOČET DOKONČEN");
Console.WriteLine("═══════════════════════════════════════════════════════════════════");

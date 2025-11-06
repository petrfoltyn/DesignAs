namespace ReinforcementDesign;

/// <summary>
/// Demo třída ukazující použití ReinforcementCalculator
/// </summary>
public static class ReinforcementCalculatorDemo
{
    public static void RunDemo()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  DEMO: ReinforcementCalculator - Výpočet výztuže");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Zadání
        var geometry = new CrossSectionGeometry
        {
            B = 0.3,
            H = 0.5,
            Layer1Distance = 0.05,
            Layer2YPos = 0.05
        };

        var concrete = new ConcreteProperties
        {
            Fcd = -20e6,
            EpsC2 = -0.002,
            EpsCu = -0.0035
        };

        var steel = new SteelProperties
        {
            Fyd = 435e6,
            Es = 200e9,
            EpsUd = 0.01
        };

        // Návrhové zatížení
        double N_design = 0 * 1000;      // kN -> N
        double M_design = 30 * 1000;     // kNm -> Nm

        // Přetvoření - příklad z Bodu 3
        double k = -0.012611;
        double q = -0.000347;

        Console.WriteLine("ZADÁNÍ:");
        Console.WriteLine($"  N = {N_design/1000:F1} kN");
        Console.WriteLine($"  M = {M_design/1000:F1} kNm");
        Console.WriteLine($"  k = {k:F6} [1/m]");
        Console.WriteLine($"  q = {q:F6} [-]");
        Console.WriteLine();

        // Výpočet sil od betonu
        var concreteForces = ConcreteIntegration.FastConcreteNM(
            geometry.B, geometry.H, k, q, concrete.Fcd);

        Console.WriteLine("SÍLY OD BETONU:");
        Console.WriteLine($"  Fc = {concreteForces.N/1000:F2} kN");
        Console.WriteLine($"  Mc = {concreteForces.M/1000:F2} kNm");
        Console.WriteLine();

        // Lokální souřadnice výztuže
        double h2 = geometry.H / 2;
        double y1Local = geometry.Y1 - h2;
        double y2Local = geometry.Y2 - h2;

        // Napětí ve výztuži
        double sigma1 = SteelIntegration.CalculateSigma(y1Local, k, q, steel);
        double sigma2 = SteelIntegration.CalculateSigma(y2Local, k, q, steel);

        Console.WriteLine("NAPĚTÍ VE VÝZTUŽI:");
        Console.WriteLine($"  σ1 = {sigma1/1e6:F1} MPa");
        Console.WriteLine($"  σ2 = {sigma2/1e6:F1} MPa");
        Console.WriteLine();

        // ═══════════════════════════════════════════════════════════════
        // VÝPOČTY VARIANT
        // ═══════════════════════════════════════════════════════════════

        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("VARIANTA 1: Optimální řešení (As1, As2)");
        Console.WriteLine("───────────────────────────────────────────────────────────────────");

        var optimal = ReinforcementCalculator.CalculateOptimal(
            N_design, M_design, concreteForces,
            sigma1, sigma2, y1Local, y2Local);

        if (optimal.IsValid)
        {
            Console.WriteLine($"  As1 = {optimal.As1 * 10000:F2} cm²");
            Console.WriteLine($"  As2 = {optimal.As2 * 10000:F2} cm²");
            Console.WriteLine($"  Celkem = {(optimal.As1 + optimal.As2) * 10000:F2} cm²");
            Console.WriteLine($"  Fs1 = {optimal.Fs1/1000:F2} kN");
            Console.WriteLine($"  Fs2 = {optimal.Fs2/1000:F2} kN");
        }
        else
        {
            Console.WriteLine($"  ✗ {optimal.ErrorMessage}");
        }

        Console.WriteLine();

        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("VARIANTA 2: Pouze dolní výztuž (As1 = 0)");
        Console.WriteLine("───────────────────────────────────────────────────────────────────");

        var single = ReinforcementCalculator.CalculateSingleLayer(
            N_design, concreteForces, sigma2, y2Local);

        if (single.IsValid)
        {
            Console.WriteLine($"  As = {single.As * 10000:F2} cm²");
            Console.WriteLine($"  Md = {single.Md/1000:F2} kNm");
            Console.WriteLine($"  Fs = {single.Fs/1000:F2} kN");
        }
        else
        {
            Console.WriteLine($"  ✗ {single.ErrorMessage}");
        }

        Console.WriteLine();

        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("VARIANTA 3: Rovnoměrné rozložení (As1 = As2 = Astot/2)");
        Console.WriteLine("───────────────────────────────────────────────────────────────────");

        var uniform = ReinforcementCalculator.CalculateUniform(
            N_design, concreteForces, sigma1, sigma2, y1Local, y2Local);

        if (uniform.IsValid)
        {
            Console.WriteLine($"  Astot = {uniform.Astot * 10000:F2} cm²");
            Console.WriteLine($"  As1 = As2 = {uniform.As1 * 10000:F2} cm²");
            Console.WriteLine($"  Mdtot = {uniform.Mdtot/1000:F2} kNm");
            Console.WriteLine($"  Fs1 = {uniform.Fs1/1000:F2} kN");
            Console.WriteLine($"  Fs2 = {uniform.Fs2/1000:F2} kN");
        }
        else
        {
            Console.WriteLine($"  ✗ {uniform.ErrorMessage}");
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
    }
}

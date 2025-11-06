namespace ReinforcementDesign;

/// <summary>
/// Třída pro výpočet interakčního diagramu N-M
/// </summary>
public class InteractionDiagram
{
    private readonly CrossSectionGeometry _geometry;
    private readonly ConcreteProperties _concrete;
    private readonly SteelProperties _steel;

    // Návrhové zatížení (pro výpočet As1, As2)
    private double _nDesign; // [kN]
    private double _mDesign; // [kNm]

    public InteractionDiagram(
        CrossSectionGeometry geometry,
        ConcreteProperties concrete,
        SteelProperties steel)
    {
        _geometry = geometry;
        _concrete = concrete;
        _steel = steel;
    }

    /// <summary>
    /// Nastavení návrhového zatížení
    /// </summary>
    public void SetDesignLoads(double n, double m)
    {
        _nDesign = n;
        _mDesign = m;
    }

    /// <summary>
    /// Nalezení přesného bodu interakčního diagramu pro zadané N_design a M_design
    /// Používá metodu regula falsi (metoda sečen) pro iteraci
    /// </summary>
    /// <param name="nDesign">Návrhová normálová síla [kN]</param>
    /// <param name="mDesign">Návrhový moment [kNm]</param>
    /// <param name="toleranceRel">Relativní tolerance [%] (např. 0.01 = 1%)</param>
    /// <param name="toleranceAbs">Absolutní tolerance [kN] (např. 0.1 kN = 100 N)</param>
    /// <param name="maxIterations">Maximální počet iterací</param>
    /// <returns>Nalezený bod s přesným řešením</returns>
    public InteractionPoint FindDesignPoint(
        double nDesign,
        double mDesign,
        double toleranceRel = 0.01,
        double toleranceAbs = 0.1,
        int maxIterations = 50)
    {
        // Nastavit návrhové zatížení
        SetDesignLoads(nDesign, mDesign);

        // Spočítat iniciální body interakčního diagramu
        var points = Calculate();

        // Najít dva sousední body, mezi kterými leží řešení
        int idx1 = -1, idx2 = -1;
        for (int i = 0; i < points.Count - 1; i++)
        {
            double mPoint1 = points[i].M;
            double mPoint2 = points[i + 1].M;

            // Kontrola, zda řešení leží mezi těmito body
            if ((mPoint1 <= mDesign && mDesign <= mPoint2) || (mPoint2 <= mDesign && mDesign <= mPoint1))
            {
                idx1 = i;
                idx2 = i + 1;
                break;
            }
        }

        if (idx1 == -1)
        {
            throw new InvalidOperationException(
                $"Řešení pro M_design = {mDesign:F2} kNm nebylo nalezeno v rozsahu interakčního diagramu. " +
                $"Rozsah momentů: [{points.Min(p => p.M):F2}, {points.Max(p => p.M):F2}] kNm");
        }

        // Začít iteraci regula falsi
        var point1 = points[idx1];
        var point2 = points[idx2];

        double eps1Top = point1.EpsTop / 1000;      // ‰ -> jednotky
        double eps1Bottom = point1.EpsBottom / 1000;
        double m1 = point1.M;

        double eps2Top = point2.EpsTop / 1000;
        double eps2Bottom = point2.EpsBottom / 1000;
        double m2 = point2.M;

        InteractionPoint result = point1;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Regula falsi: lineární interpolace
            double t = (mDesign - m1) / (m2 - m1);
            double epsTopNew = eps1Top + t * (eps2Top - eps1Top);
            double epsBottomNew = eps1Bottom + t * (eps2Bottom - eps1Bottom);

            // Vypočítat nový bod
            result = CalculatePoint($"Iteration {iter + 1}", epsTopNew, epsBottomNew);

            // Kontrola konvergence
            double errorAbs = Math.Abs(result.M - mDesign);
            double errorRel = Math.Abs(mDesign) > 1e-6 ? errorAbs / Math.Abs(mDesign) : errorAbs;

            if (errorAbs < toleranceAbs || errorRel < toleranceRel)
            {
                result.Name = $"Design point (converged after {iter + 1} iterations)";
                return result;
            }

            // Aktualizace intervalů pro další iteraci
            if ((m1 - mDesign) * (result.M - mDesign) < 0)
            {
                // Řešení leží mezi point1 a result
                eps2Top = epsTopNew;
                eps2Bottom = epsBottomNew;
                m2 = result.M;
            }
            else
            {
                // Řešení leží mezi result a point2
                eps1Top = epsTopNew;
                eps1Bottom = epsBottomNew;
                m1 = result.M;
            }
        }

        result.Name = $"Design point (max iterations {maxIterations} reached)";
        return result;
    }

    /// <summary>
    /// Výpočet charakteristických bodů interakčního diagramu
    /// </summary>
    /// <param name="densities">Pole s počtem dílů pro každý interval (mezi Bod1-Bod2, Bod2-Bod2b, atd.)</param>
    /// <returns>Seznam bodů interakčního diagramu</returns>
    public List<InteractionPoint> Calculate(int[]? densities = null)
    {
        // Charakteristické body
        var characteristicPoints = GetCharacteristicPoints();

        // Výchozí zahuštění 10 dílů pro každý interval
        if (densities == null)
        {
            densities = Enumerable.Repeat(10, characteristicPoints.Count - 1).ToArray();
        }

        if (densities.Length != characteristicPoints.Count - 1)
        {
            throw new ArgumentException(
                $"Počet zahuštění ({densities.Length}) musí být roven počtu intervalů ({characteristicPoints.Count - 1})");
        }

        var points = new List<InteractionPoint>();

        // Generování zahuštěných bodů
        for (int i = 0; i < characteristicPoints.Count - 1; i++)
        {
            var point1 = characteristicPoints[i];
            var point2 = characteristicPoints[i + 1];
            int density = densities[i];

            // Přidat první charakteristický bod
            points.Add(CalculatePoint(point1.name, point1.epsTop, point1.epsBottom));

            // Přidat mezilehlé body
            for (int j = 1; j < density; j++)
            {
                double t = (double)j / density;
                double epsTopInterp = point1.epsTop + t * (point2.epsTop - point1.epsTop);
                double epsBottomInterp = point1.epsBottom + t * (point2.epsBottom - point1.epsBottom);

                string name = $"{point1.name}-{point2.name} ({j}/{density})";
                points.Add(CalculatePoint(name, epsTopInterp, epsBottomInterp));
            }
        }

        // Přidat poslední charakteristický bod
        var lastPoint = characteristicPoints[^1];
        points.Add(CalculatePoint(lastPoint.name, lastPoint.epsTop, lastPoint.epsBottom));

        return points;
    }

    /// <summary>
    /// Definice charakteristických bodů
    /// </summary>
    private List<(string name, double epsTop, double epsBottom)> GetCharacteristicPoints()
    {
        var points = new List<(string, double, double)>();

        double epsYd = _steel.EpsYd;
        double epsCu = _concrete.EpsCu;
        double epsC2 = _concrete.EpsC2;
        double epsUd = _steel.EpsUd;
        double y1Norm = _geometry.Y1Norm;
        double y2Norm = _geometry.Y2Norm;

        // BOD 1: Dostředný tlak
        points.Add(("Bod 1", epsCu, epsCu));

        // BOD 2: TOP = εcu, BOTTOM = εc2
        points.Add(("Bod 2", epsCu, epsC2));

        // BOD 2b: TOP = εcu, BOTTOM = 0
        points.Add(("Bod 2b", epsCu, 0));

        // BOD 3: TOP = εcu, εs2 = εyd
        double epsBottomBod3 = (epsYd - epsCu * y2Norm) / (1 - y2Norm);
        points.Add(("Bod 3", epsCu, epsBottomBod3));

        // BOD 4: TOP = εcu, εs2 = εud
        double epsBottomBod4 = (epsUd - epsCu * y2Norm) / (1 - y2Norm);
        points.Add(("Bod 4", epsCu, epsBottomBod4));

        // BOD 5: TOP = εc2, εs2 = εud
        double epsBottomBod5 = (epsUd - epsC2 * y2Norm) / (1 - y2Norm);
        points.Add(("Bod 5", epsC2, epsBottomBod5));

        // BOD 6: TOP = 0, εs2 = εud
        double epsBottomBod6 = (epsUd - 0 * y2Norm) / (1 - y2Norm);
        points.Add(("Bod 6", 0, epsBottomBod6));

        // BOD 7: εs1 = εyd, εs2 = εud
        double epsTopBod7 = (epsYd * y2Norm - epsUd * y1Norm) / (y2Norm - y1Norm);
        double epsBottomBod7 = (epsUd - epsTopBod7 * (1 - y2Norm)) / y2Norm;
        points.Add(("Bod 7", epsTopBod7, epsBottomBod7));

        // BOD 8: Čistý tah
        points.Add(("Bod 8", epsUd, epsUd));

        return points;
    }

    /// <summary>
    /// Výpočet jednoho bodu interakčního diagramu
    /// </summary>
    private InteractionPoint CalculatePoint(string name, double epsTop, double epsBottom)
    {
        // Převod na ‰ pro zobrazení
        double epsTopPm = epsTop * 1000;
        double epsBottomPm = epsBottom * 1000;

        // Přetvoření ve výztuži
        double epsS1 = SteelStress.CalculateStrainAtY(epsTop, epsBottom, _geometry.Y1Norm);
        double epsS2 = SteelStress.CalculateStrainAtY(epsTop, epsBottom, _geometry.Y2Norm);
        double epsS1Pm = epsS1 * 1000;
        double epsS2Pm = epsS2 * 1000;

        // Parametry přetvoření k, q
        var (k, q) = SteelStress.CalculateStrainParameters(epsTop, epsBottom, _geometry.H);

        // Síly od betonu
        var concreteForces = ConcreteIntegration.FastConcreteNM(
            _geometry.B, _geometry.H, k, q, _concrete.Fcd);
        double fc = concreteForces.N / 1000; // kN

        // Napětí ve výztuži
        double sigma1 = SteelStress.CalculateStress(epsS1, _steel);
        double sigma2 = SteelStress.CalculateStress(epsS2, _steel);

        // Lokální souřadnice
        double h2 = _geometry.H / 2;
        double y1Local = _geometry.Y1 - (_geometry.H - h2);
        double y2Local = _geometry.Y2 - (_geometry.H - h2);

        // Návrhové zatížení
        double nPa = _nDesign * 1000;  // kN -> N

        // ═══════════════════════════════════════════════════════════════
        // VÝPOČET VARIANTY 2: Pouze dolní výztuž (As1 = 0)
        // ═══════════════════════════════════════════════════════════════

        var single = ReinforcementCalculator.CalculateSingleLayer(
            nPa, concreteForces, sigma2, y2Local);

        // Výsledky
        double as2 = single.IsValid ? single.As : double.NaN;
        double fs2 = single.IsValid ? single.Fs / 1000 : double.NaN; // N -> kN
        double mdSimple = single.IsValid ? single.Md / 1000 : double.NaN; // Nm -> kNm

        // Celkové síly (N kontrola, M vypočtené)
        double nTotal = fc + (double.IsNaN(fs2) ? 0 : fs2);
        double mTotal = -concreteForces.M / 1000 + (double.IsNaN(fs2) ? 0 : fs2 * (-y2Local));

        return new InteractionPoint
        {
            Name = name,
            EpsTop = epsTopPm,
            EpsBottom = epsBottomPm,
            EpsS1 = epsS1Pm,
            EpsS2 = epsS2Pm,
            Fc = fc,
            Mc = -concreteForces.M / 1000, // Nm -> kNm
            Fs2 = double.IsNaN(fs2) ? double.NaN : fs2,
            N = nTotal,
            M = mTotal,
            As2 = double.IsNaN(as2) ? double.NaN : as2 * 10000, // m² -> cm²
            Md = mdSimple
        };
    }
}

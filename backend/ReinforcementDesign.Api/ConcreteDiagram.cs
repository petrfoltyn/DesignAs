namespace ReinforcementDesign;

/// <summary>
/// Třída pro výpočet interakčního diagramu N-M pouze pro beton (bez výpočtu ploch výztuže)
/// </summary>
public class ConcreteDiagram
{
    private readonly CrossSectionGeometry _geometry;
    private readonly ConcreteProperties _concrete;
    private readonly SteelProperties _steel;

    public ConcreteDiagram(
        CrossSectionGeometry geometry,
        ConcreteProperties concrete,
        SteelProperties steel)
    {
        _geometry = geometry;
        _concrete = concrete;
        _steel = steel;
    }

    /// <summary>
    /// Výpočet charakteristických bodů interakčního diagramu - pouze beton
    /// </summary>
    /// <param name="densities">Pole s počtem dílů pro každý interval (mezi Bod1-Bod2, Bod2-Bod2b, atd.)</param>
    /// <returns>Seznam bodů interakčního diagramu</returns>
    public List<ConcretePoint> Calculate(int[]? densities = null)
    {
        // Charakteristické body (k, q)
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

        var points = new List<ConcretePoint>();

        // Generování zahuštěných bodů
        for (int i = 0; i < characteristicPoints.Count - 1; i++)
        {
            var point1 = characteristicPoints[i];
            var point2 = characteristicPoints[i + 1];
            int density = densities[i];

            // Přidat první charakteristický bod
            points.Add(CalculatePoint(point1.name, point1.k, point1.q));

            // Přidat mezilehlé body
            for (int j = 1; j < density; j++)
            {
                double t = (double)j / density;
                double kInterp = point1.k + t * (point2.k - point1.k);
                double qInterp = point1.q + t * (point2.q - point1.q);

                string name = $"{point1.name}-{point2.name} ({j}/{density})";
                points.Add(CalculatePoint(name, kInterp, qInterp));
            }
        }

        // Přidat poslední charakteristický bod
        var lastPoint = characteristicPoints[^1];
        points.Add(CalculatePoint(lastPoint.name, lastPoint.k, lastPoint.q));

        return points;
    }

    /// <summary>
    /// Definice charakteristických bodů (k, q)
    /// </summary>
    private List<(string name, double k, double q)> GetCharacteristicPoints()
    {
        var points = new List<(string, double, double)>();

        double epsYd = _steel.EpsYd;
        double epsCu = _concrete.EpsCu;
        double epsC2 = _concrete.EpsC2;
        double epsUd = _steel.EpsUd;

        double h = _geometry.H;
        double h2 = h / 2;
        double y1Local = _geometry.Y1 - h2;
        double y2Local = _geometry.Y2 - h2;

        // BOD 1: Dostředný tlak (εtop = εbottom = εcu)
        double bod1_k = 0.0;
        double bod1_q = epsCu;
        points.Add(("Bod 1", bod1_k, bod1_q));

        // BOD 2: TOP = εcu, BOTTOM = εc2
        double bod2_k = (epsCu - epsC2) / h;
        double bod2_q = epsCu - bod2_k * h2;
        points.Add(("Bod 2", bod2_k, bod2_q));

        // BOD 2b: TOP = εcu, BOTTOM = 0
        double bod2b_k = (epsCu - 0.0) / h;
        double bod2b_q = epsCu - bod2b_k * h2;
        points.Add(("Bod 2b", bod2b_k, bod2b_q));

        // BOD 3: TOP = εcu, εs2 = εyd
        double bod3_k = (epsYd - epsCu) / (y2Local - h2);
        double bod3_q = epsCu - bod3_k * h2;
        points.Add(("Bod 3", bod3_k, bod3_q));

        // BOD 4: TOP = εcu, εs2 = εud
        double bod4_k = (epsUd - epsCu) / (y2Local - h2);
        double bod4_q = epsCu - bod4_k * h2;
        points.Add(("Bod 4", bod4_k, bod4_q));

        // BOD 5: TOP = εc2, εs2 = εud
        double bod5_k = (epsUd - epsC2) / (y2Local - h2);
        double bod5_q = epsC2 - bod5_k * h2;
        points.Add(("Bod 5", bod5_k, bod5_q));

        // BOD 6: TOP = 0, εs2 = εud
        double bod6_k = (epsUd - 0.0) / (y2Local - h2);
        double bod6_q = 0.0 - bod6_k * h2;
        points.Add(("Bod 6", bod6_k, bod6_q));

        // BOD 7: εs1 = εyd, εs2 = εud
        double bod7_k = (epsYd - epsUd) / (y1Local - y2Local);
        double bod7_q = epsYd - bod7_k * y1Local;
        points.Add(("Bod 7", bod7_k, bod7_q));

        // BOD 8: Čistý tah (εtop = εbottom = εud)
        double bod8_k = 0.0;
        double bod8_q = epsUd;
        points.Add(("Bod 8", bod8_k, bod8_q));

        return points;
    }

    /// <summary>
    /// Výpočet jednoho bodu interakčního diagramu - pouze beton
    /// </summary>
    private ConcretePoint CalculatePoint(string name, double k, double q)
    {
        // Výpočet přetvoření na okrajích
        // ε(y) = k*y + q, kde y je lokální souřadnice od těžiště
        double h2 = _geometry.H / 2;
        double epsTop = k * h2 + q;      // Přetvoření nahoře
        double epsBottom = k * (-h2) + q; // Přetvoření dole

        // Výpočet přetvoření v pozicích výztuže (v lokálních souřadnicích)
        double y1Local = _geometry.Y1 - h2; // Horní výztuž
        double y2Local = _geometry.Y2 - h2; // Dolní výztuž

        double epsAs1 = k * y1Local + q;
        double epsAs2 = k * y2Local + q;

        // Výpočet napětí ve výztuži
        double sigAs1 = SteelStress.CalculateStress(epsAs1, _steel);
        double sigAs2 = SteelStress.CalculateStress(epsAs2, _steel);

        // Síly od betonu
        var concreteForces = ConcreteIntegration.FastConcreteNM(
            _geometry.B, _geometry.H, k, q, _concrete.Fcd);

        return new ConcretePoint
        {
            Name = name,
            K = k,
            Q = q,
            EpsTop = epsTop * 1000,        // [-] -> [‰]
            EpsBottom = epsBottom * 1000,  // [-] -> [‰]
            EpsAs1 = epsAs1 * 1000,        // [-] -> [‰]
            EpsAs2 = epsAs2 * 1000,        // [-] -> [‰]
            SigAs1 = sigAs1 / 1e6,         // [Pa] -> [MPa]
            SigAs2 = sigAs2 / 1e6,         // [Pa] -> [MPa]
            N = concreteForces.N / 1000,   // N -> kN
            M = concreteForces.M / 1000    // Nm -> kNm
        };
    }
}

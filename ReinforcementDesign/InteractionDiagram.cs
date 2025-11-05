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

        // === VARIANTA 1: Optimální As1 a As2 (pro návrhové zatížení) ===
        double nPa = _nDesign * 1000;  // kN -> N
        double mPa = _mDesign * 1000;  // kNm -> Nm

        double rhsN = nPa - concreteForces.N;
        double rhsM = -mPa - concreteForces.M;

        double det = sigma1 * sigma2 * (y2Local - y1Local);

        double as1, as2, fs1, fs2;

        if (Math.Abs(det) < 1e-6)
        {
            as1 = as2 = fs1 = fs2 = double.NaN;
        }
        else
        {
            as1 = (rhsN * y2Local - rhsM) / (sigma1 * (y2Local - y1Local));
            as2 = (rhsM - y1Local * rhsN) / (sigma2 * (y2Local - y1Local));
            fs1 = as1 * sigma1 / 1000; // kN
            fs2 = as2 * sigma2 / 1000; // kN
        }

        // Celkové síly
        double nTotal = fc + (double.IsNaN(fs1) ? 0 : fs1) + (double.IsNaN(fs2) ? 0 : fs2);
        double mTotal = -concreteForces.M / 1000 +
                       (double.IsNaN(fs1) ? 0 : fs1 * (-y1Local)) +
                       (double.IsNaN(fs2) ? 0 : fs2 * (-y2Local));

        // === VARIANTA 2: Pouze dolní výztuž (As1 = 0) ===
        double asSimple, mdSimple;

        if (Math.Abs(sigma2) > 1e-6)
        {
            asSimple = (nPa - concreteForces.N) / sigma2; // m²
            double fs2Simple = asSimple * sigma2; // N
            double ms2Simple = fs2Simple * (-y2Local); // Nm
            mdSimple = -concreteForces.M / 1000 + ms2Simple / 1000; // kNm
        }
        else
        {
            asSimple = double.NaN;
            mdSimple = double.NaN;
        }

        // === VARIANTA 3: Rovnoměrné rozložení (As1 = As2 = Astot/2) ===
        double astot, mdtot;
        double sigmaSum = sigma1 + sigma2;

        if (Math.Abs(sigmaSum) > 1e-6)
        {
            astot = 2 * (nPa - concreteForces.N) / sigmaSum; // m²

            double as1Tot = astot / 2;
            double as2Tot = astot / 2;
            double fs1Tot = as1Tot * sigma1; // N
            double fs2Tot = as2Tot * sigma2; // N
            double ms1Tot = fs1Tot * (-y1Local); // Nm
            double ms2Tot = fs2Tot * (-y2Local); // Nm
            mdtot = -concreteForces.M / 1000 + ms1Tot / 1000 + ms2Tot / 1000; // kNm
        }
        else
        {
            astot = double.NaN;
            mdtot = double.NaN;
        }

        return new InteractionPoint
        {
            Name = name,
            EpsTop = epsTopPm,
            EpsBottom = epsBottomPm,
            EpsS1 = epsS1Pm,
            EpsS2 = epsS2Pm,
            Fc = fc,
            Fs1 = double.IsNaN(fs1) ? double.NaN : fs1,
            Fs2 = double.IsNaN(fs2) ? double.NaN : fs2,
            As1 = double.IsNaN(as1) ? double.NaN : as1 * 10000, // m² -> cm²
            As2 = double.IsNaN(as2) ? double.NaN : as2 * 10000, // m² -> cm²
            N = nTotal,
            M = mTotal,
            As = double.IsNaN(asSimple) ? double.NaN : asSimple * 10000, // m² -> cm²
            Md = mdSimple,
            Astot = double.IsNaN(astot) ? double.NaN : astot * 10000, // m² -> cm²
            Mdtot = mdtot
        };
    }
}

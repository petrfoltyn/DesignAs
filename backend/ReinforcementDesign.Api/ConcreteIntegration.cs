namespace ReinforcementDesign;

/// <summary>
/// Výsledek integrace vnitřních sil (beton nebo ocel)
/// </summary>
public class Forces
{
    /// <summary>
    /// Normálová síla [N]
    /// </summary>
    public double N { get; set; }

    /// <summary>
    /// Moment [Nm]
    /// </summary>
    public double M { get; set; }
}

/// <summary>
/// Třída pro výpočet vnitřních sil betonu podle parabolicko-obdélníkového diagramu EC2
/// </summary>
public static class ConcreteIntegration
{
    private const double EC2 = -0.002;      // εc2 = -2‰
    private const double INV_EC2 = -500.0;  // 1/εc2
    private const double TOLERANCE = 1e-12;

    /// <summary>
    /// Rychlý výpočet vnitřních sil betonu pomocí parabolicko-obdélníkového diagramu EC2
    /// </summary>
    /// <param name="b">Šířka průřezu [m]</param>
    /// <param name="h">Výška průřezu [m]</param>
    /// <param name="k">Sklon přetvoření [1/m]</param>
    /// <param name="q">Přetvoření v těžišti [-]</param>
    /// <param name="fcd">Návrhová pevnost betonu v tlaku [Pa] (záporná)</param>
    /// <returns>Normálová síla a moment od betonu</returns>
    public static Forces FastConcreteNM(double b, double h, double k, double q, double fcd)
    {
        double h2 = 0.5 * h;
        double x1 = -h2;  // BOTTOM
        double x2 = h2;   // TOP

        double x0, xEc2;

        // Výpočet kritických bodů
        if (IsNonZero(k))
        {
            x0 = -q / k;
            xEc2 = (EC2 - q) / k;
        }
        else
        {
            x0 = IsZero(q) ? 0 : (q > 0 ? double.NegativeInfinity : double.PositiveInfinity);
            xEc2 = double.PositiveInfinity;
        }

        double N = 0.0;
        double M = 0.0;

        // Konstantní přetvoření
        if (IsZero(k))
        {
            if (q >= 0)
            {
                // Tah nebo nula
                return new Forces { N = 0, M = 0 };
            }
            else if (q > EC2)
            {
                // Parabolická sekce
                double epsilonNorm = q / EC2;
                double sigma = fcd * (1 - (1 - epsilonNorm) * (1 - epsilonNorm));
                N = sigma * b * h;
                M = 0;
            }
            else
            {
                // Konstantní sekce
                N = fcd * b * h;
                M = 0;
            }
            return new Forces { N = N, M = M };
        }

        // SEGMENT 2: Parabolická tlaková sekce
        double xaPara = Math.Max(x1, Math.Min(xEc2, x0));
        double xbPara = Math.Min(x2, Math.Max(xEc2, x0));

        if (IsLess(xaPara, xbPara))
        {
            double a = k * INV_EC2;
            double c = q * INV_EC2;

            double dx = xbPara - xaPara;
            double dx2 = xbPara * xbPara - xaPara * xaPara;
            double dx3 = (xbPara * xbPara * xbPara - xaPara * xaPara * xaPara) / 3.0;

            double nPara = fcd * b * (
                (2 * a - 2 * a * c) * dx2 * 0.5 +
                (2 * c - c * c) * dx -
                a * a * dx3
            );

            double xa2 = xaPara * xaPara;
            double xb2 = xbPara * xbPara;
            double xa4 = xa2 * xa2;
            double xb4 = xb2 * xb2;
            double dx4 = 0.25 * (xb4 - xa4);

            double mPara = fcd * b * (
                (2 * a - 2 * a * c) * dx3 +
                (2 * c - c * c) * dx2 * 0.5 -
                a * a * dx4
            );

            N += nPara;
            M += mPara;
        }

        // SEGMENT 3: Konstantní tlaková sekce (ε ≤ εc2)
        double xaConst, xbConst;

        if (Math.Abs(k) < 1e-10)
        {
            xaConst = xbConst = 0;
        }
        else if (k > 0)
        {
            // ε roste s x, větší tlak (ε ≤ εc2) je pro x ≤ xEc2
            xaConst = x1;
            xbConst = Math.Min(xEc2, x2);
        }
        else
        {
            // ε klesá s x, větší tlak (ε ≤ εc2) je pro x ≥ xEc2
            xaConst = Math.Max(xEc2, x1);
            xbConst = x2;
        }

        if (IsLess(xaConst, xbConst))
        {
            double dx = xbConst - xaConst;
            double centroid = 0.5 * (xaConst + xbConst);
            double nConst = fcd * b * dx;
            double mConst = nConst * centroid;

            N += nConst;
            M += mConst;
        }

        return new Forces { N = N, M = M };
    }

    private static bool IsNonZero(double val) => Math.Abs(val) >= TOLERANCE;
    private static bool IsZero(double val) => Math.Abs(val) < TOLERANCE;
    private static bool IsLess(double a, double b) => a < b - TOLERANCE;
}

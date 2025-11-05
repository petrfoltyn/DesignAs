namespace ReinforcementDesign;

/// <summary>
/// Pomocné metody pro výpočet napětí ve výztuži
/// </summary>
public static class SteelStress
{
    /// <summary>
    /// Výpočet napětí ve výztuži podle bilineárního diagramu
    /// </summary>
    /// <param name="eps">Přetvoření [-]</param>
    /// <param name="steel">Vlastnosti výztuže</param>
    /// <returns>Napětí [Pa]</returns>
    public static double CalculateStress(double eps, SteelProperties steel)
    {
        double sigmaElastic = eps * steel.Es;
        return Math.Max(Math.Min(sigmaElastic, steel.Fyd), -steel.Fyd);
    }

    /// <summary>
    /// Výpočet přetvoření v bodě y při lineární distribuci
    /// </summary>
    /// <param name="epsTop">Přetvoření v horním okraji [-]</param>
    /// <param name="epsBottom">Přetvoření v dolním okraji [-]</param>
    /// <param name="yNormalized">Normalizovaná pozice (0 = spodek, 1 = vršek)</param>
    /// <returns>Přetvoření [-]</returns>
    public static double CalculateStrainAtY(double epsTop, double epsBottom, double yNormalized)
    {
        return epsBottom + (epsTop - epsBottom) * yNormalized;
    }

    /// <summary>
    /// Výpočet parametrů lineární distribuce přetvoření
    /// ε(y) = k*y + q
    /// </summary>
    /// <param name="epsTop">Přetvoření nahoře [-]</param>
    /// <param name="epsBottom">Přetvoření dole [-]</param>
    /// <param name="h">Výška průřezu [m]</param>
    /// <returns>Tuple (k, q) kde k je sklon [1/m] a q je přetvoření v těžišti [-]</returns>
    public static (double k, double q) CalculateStrainParameters(double epsTop, double epsBottom, double h)
    {
        double h2 = h / 2;
        double yTopLocal = h2;
        double yBottomLocal = -h2;

        double k = (epsTop - epsBottom) / (yTopLocal - yBottomLocal);
        double q = epsTop - k * yTopLocal;

        return (k, q);
    }
}

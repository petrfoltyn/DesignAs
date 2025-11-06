namespace ReinforcementDesign;

/// <summary>
/// Třída pro výpočet vnitřních sil od výztuže
/// </summary>
public static class SteelIntegration
{
    /// <summary>
    /// Rychlý výpočet vnitřních sil od výztuže pro daný stav přetvoření
    /// </summary>
    /// <param name="As">Plocha výztuže [m²]</param>
    /// <param name="y">Poloha výztuže v lokálních souřadnicích [m] (0 = těžiště)</param>
    /// <param name="k">Sklon přetvoření [1/m]</param>
    /// <param name="q">Přetvoření v těžišti [-]</param>
    /// <param name="steel">Vlastnosti výztuže</param>
    /// <returns>Normálová síla a moment od výztuže</returns>
    public static Forces FastSteelNM(double As, double y, double k, double q, SteelProperties steel)
    {
        // Výpočet přetvoření ve výztuži: ε(y) = k*y + q
        double eps = k * y + q;

        // Výpočet napětí podle bilineárního diagramu
        double sigma = SteelStress.CalculateStress(eps, steel);

        // Vnitřní síly
        double N = As * sigma;          // [N]
        double M = N * y;                // [Nm] (moment kolem těžiště)

        return new Forces { N = N, M = M };
    }

    /// <summary>
    /// Výpočet celkových sil od obou vrstev výztuže
    /// </summary>
    /// <param name="As1">Plocha horní výztuže [m²]</param>
    /// <param name="As2">Plocha dolní výztuže [m²]</param>
    /// <param name="y1">Poloha horní výztuže [m]</param>
    /// <param name="y2">Poloha dolní výztuže [m]</param>
    /// <param name="k">Sklon přetvoření [1/m]</param>
    /// <param name="q">Přetvoření v těžišti [-]</param>
    /// <param name="steel">Vlastnosti výztuže</param>
    /// <returns>Celkové síly od obou vrstev výztuže</returns>
    public static Forces FastSteelTotalNM(
        double As1, double As2,
        double y1, double y2,
        double k, double q,
        SteelProperties steel)
    {
        // Síly od horní výztuže
        var forces1 = FastSteelNM(As1, y1, k, q, steel);

        // Síly od dolní výztuže
        var forces2 = FastSteelNM(As2, y2, k, q, steel);

        // Součet
        return new Forces
        {
            N = forces1.N + forces2.N,
            M = forces1.M + forces2.M
        };
    }

    /// <summary>
    /// Výpočet napětí ve výztuži pro daný stav přetvoření
    /// </summary>
    /// <param name="y">Poloha výztuže [m]</param>
    /// <param name="k">Sklon přetvoření [1/m]</param>
    /// <param name="q">Přetvoření v těžišti [-]</param>
    /// <param name="steel">Vlastnosti výztuže</param>
    /// <returns>Napětí [Pa]</returns>
    public static double CalculateSigma(double y, double k, double q, SteelProperties steel)
    {
        double eps = k * y + q;
        return SteelStress.CalculateStress(eps, steel);
    }

    /// <summary>
    /// Výpočet přetvoření ve výztuži pro daný stav
    /// </summary>
    /// <param name="y">Poloha výztuže [m]</param>
    /// <param name="k">Sklon přetvoření [1/m]</param>
    /// <param name="q">Přetvoření v těžišti [-]</param>
    /// <returns>Přetvoření [-]</returns>
    public static double CalculateEpsilon(double y, double k, double q)
    {
        return k * y + q;
    }
}

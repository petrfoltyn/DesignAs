namespace ReinforcementDesign;

/// <summary>
/// Třída pro výpočet ploch výztuže podle různých variant
/// </summary>
public static class ReinforcementCalculator
{
    /// <summary>
    /// Výsledek výpočtu výztuže - varianta 1 (optimální As1, As2)
    /// </summary>
    public class OptimalResult
    {
        public double As1 { get; set; }  // [m²]
        public double As2 { get; set; }  // [m²]
        public double Fs1 { get; set; }  // [N]
        public double Fs2 { get; set; }  // [N]
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Výsledek výpočtu výztuže - varianta 2 (pouze dolní As)
    /// </summary>
    public class SingleLayerResult
    {
        public double As { get; set; }   // [m²]
        public double Md { get; set; }   // [Nm]
        public double Fs { get; set; }   // [N]
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Výsledek výpočtu výztuže - varianta 3 (rovnoměrné rozložení Astot)
    /// </summary>
    public class UniformResult
    {
        public double Astot { get; set; } // [m²]
        public double Mdtot { get; set; } // [Nm]
        public double As1 { get; set; }   // [m²] = Astot/2
        public double As2 { get; set; }   // [m²] = Astot/2
        public double Fs1 { get; set; }   // [N]
        public double Fs2 { get; set; }   // [N]
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// VARIANTA 1: Optimální řešení - výpočet As1 a As2 pro návrhové zatížení
    /// Řeší soustavu rovnic pro minimální celkovou plochu výztuže
    /// </summary>
    /// <param name="nDesign">Návrhová normálová síla [N]</param>
    /// <param name="mDesign">Návrhový moment [Nm]</param>
    /// <param name="concreteForces">Síly od betonu</param>
    /// <param name="sigma1">Napětí v horní výztuži [Pa]</param>
    /// <param name="sigma2">Napětí v dolní výztuži [Pa]</param>
    /// <param name="y1Local">Lokální souřadnice horní výztuže [m]</param>
    /// <param name="y2Local">Lokální souřadnice dolní výztuže [m]</param>
    /// <returns>Výsledek s As1, As2</returns>
    public static OptimalResult CalculateOptimal(
        double nDesign,
        double mDesign,
        Forces concreteForces,
        double sigma1,
        double sigma2,
        double y1Local,
        double y2Local)
    {
        // Pravá strana rovnic (bez neznámých)
        double rhsN = nDesign - concreteForces.N;
        double rhsM = -mDesign - concreteForces.M;

        // Determinant soustavy: D = σ1·σ2·(y2 - y1)
        double det = sigma1 * sigma2 * (y2Local - y1Local);

        if (Math.Abs(det) < 1e-6)
        {
            return new OptimalResult
            {
                IsValid = false,
                ErrorMessage = "Singulární soustava rovnic"
            };
        }

        // Cramerovo pravidlo
        double as1 = (rhsN * y2Local - rhsM) / (sigma1 * (y2Local - y1Local));
        double as2 = (rhsM - y1Local * rhsN) / (sigma2 * (y2Local - y1Local));

        return new OptimalResult
        {
            As1 = as1,
            As2 = as2,
            Fs1 = as1 * sigma1,
            Fs2 = as2 * sigma2,
            IsValid = true
        };
    }

    /// <summary>
    /// VARIANTA 2: Pouze dolní výztuž (As1 = 0)
    /// Výpočet As pro danou normálovou sílu, výpočet možného momentu Md
    /// </summary>
    /// <param name="nDesign">Návrhová normálová síla [N]</param>
    /// <param name="concreteForces">Síly od betonu</param>
    /// <param name="sigma2">Napětí v dolní výztuži [Pa]</param>
    /// <param name="y2Local">Lokální souřadnice dolní výztuže [m]</param>
    /// <returns>Výsledek s As a Md</returns>
    public static SingleLayerResult CalculateSingleLayer(
        double nDesign,
        Forces concreteForces,
        double sigma2,
        double y2Local)
    {
        if (Math.Abs(sigma2) < 1e-6)
        {
            return new SingleLayerResult
            {
                IsValid = false,
                ErrorMessage = "Napětí v dolní výztuži je nulové"
            };
        }

        // Rovnováha sil: As·σ2 + Fc = N
        // => As = (N - Fc) / σ2
        double asSimple = (nDesign - concreteForces.N) / sigma2;

        // Moment s touto výztuží
        double fs2Simple = asSimple * sigma2;
        double ms2Simple = fs2Simple * (-y2Local);
        double mdSimple = -concreteForces.M + ms2Simple;

        return new SingleLayerResult
        {
            As = asSimple,
            Md = mdSimple,
            Fs = fs2Simple,
            IsValid = true
        };
    }

    /// <summary>
    /// VARIANTA 3: Rovnoměrné rozložení (As1 = As2 = Astot/2)
    /// Výpočet celkové plochy pro danou normálovou sílu, výpočet možného momentu Mdtot
    /// </summary>
    /// <param name="nDesign">Návrhová normálová síla [N]</param>
    /// <param name="concreteForces">Síly od betonu</param>
    /// <param name="sigma1">Napětí v horní výztuži [Pa]</param>
    /// <param name="sigma2">Napětí v dolní výztuži [Pa]</param>
    /// <param name="y1Local">Lokální souřadnice horní výztuže [m]</param>
    /// <param name="y2Local">Lokální souřadnice dolní výztuže [m]</param>
    /// <returns>Výsledek s Astot a Mdtot</returns>
    public static UniformResult CalculateUniform(
        double nDesign,
        Forces concreteForces,
        double sigma1,
        double sigma2,
        double y1Local,
        double y2Local)
    {
        double sigmaSum = sigma1 + sigma2;

        if (Math.Abs(sigmaSum) < 1e-6)
        {
            return new UniformResult
            {
                IsValid = false,
                ErrorMessage = "Součet napětí je nulový"
            };
        }

        // Rovnováha sil: (Astot/2)·σ1 + (Astot/2)·σ2 + Fc = N
        // => Astot·(σ1 + σ2)/2 = N - Fc
        // => Astot = 2·(N - Fc) / (σ1 + σ2)
        double astot = 2 * (nDesign - concreteForces.N) / sigmaSum;

        double as1Tot = astot / 2;
        double as2Tot = astot / 2;

        double fs1Tot = as1Tot * sigma1;
        double fs2Tot = as2Tot * sigma2;

        double ms1Tot = fs1Tot * (-y1Local);
        double ms2Tot = fs2Tot * (-y2Local);
        double mdtot = -concreteForces.M + ms1Tot + ms2Tot;

        return new UniformResult
        {
            Astot = astot,
            Mdtot = mdtot,
            As1 = as1Tot,
            As2 = as2Tot,
            Fs1 = fs1Tot,
            Fs2 = fs2Tot,
            IsValid = true
        };
    }

    /// <summary>
    /// Výpočet všech tří variant najednou
    /// </summary>
    public static (OptimalResult optimal, SingleLayerResult single, UniformResult uniform) CalculateAll(
        double nDesign,
        double mDesign,
        Forces concreteForces,
        double sigma1,
        double sigma2,
        double y1Local,
        double y2Local)
    {
        var optimal = CalculateOptimal(nDesign, mDesign, concreteForces, sigma1, sigma2, y1Local, y2Local);
        var single = CalculateSingleLayer(nDesign, concreteForces, sigma2, y2Local);
        var uniform = CalculateUniform(nDesign, concreteForces, sigma1, sigma2, y1Local, y2Local);

        return (optimal, single, uniform);
    }
}

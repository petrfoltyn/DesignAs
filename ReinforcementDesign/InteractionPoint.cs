namespace ReinforcementDesign;

/// <summary>
/// Bod interakčního diagramu s kompletními výsledky
/// </summary>
public class InteractionPoint
{
    /// <summary>
    /// Název bodu
    /// </summary>
    public string Name { get; set; } = "";

    // Přetvoření [‰]
    public double EpsTop { get; set; }
    public double EpsBottom { get; set; }
    public double EpsS1 { get; set; }
    public double EpsS2 { get; set; }

    // Síly [kN]
    public double Fc { get; set; }
    public double Fs1 { get; set; }
    public double Fs2 { get; set; }

    // Celkové vnitřní síly [kN] a [kNm]
    public double N { get; set; }
    public double M { get; set; }

    // Návrhová výztuž - optimální řešení [cm²]
    public double As1 { get; set; }
    public double As2 { get; set; }

    // Varianta 1: Pouze dolní výztuž (As1 = 0) [cm²] a [kNm]
    public double As { get; set; }
    public double Md { get; set; }

    // Varianta 2: Rovnoměrné rozložení (As1 = As2 = Astot/2) [cm²] a [kNm]
    public double Astot { get; set; }
    public double Mdtot { get; set; }

    public override string ToString()
    {
        return $"{Name,-20} | εtop={EpsTop,7:F2}‰ εbot={EpsBottom,7:F2}‰ | " +
               $"N={N,8:F2}kN M={M,8:F2}kNm | " +
               $"As1={As1,7:F2}cm² As2={As2,7:F2}cm² | " +
               $"As={As,7:F2}cm² Md={Md,7:F2}kNm | " +
               $"Astot={Astot,7:F2}cm² Mdtot={Mdtot,7:F2}kNm";
    }

    /// <summary>
    /// CSV hlavička
    /// </summary>
    public static string CsvHeader =>
        "Bod;εtop[‰];εbottom[‰];εs1[‰];εs2[‰];" +
        "Fc[kN];Fs1[kN];Fs2[kN];" +
        "As1[cm²];As2[cm²];" +
        "N[kN];M[kNm];" +
        "As[cm²];Md[kNm];" +
        "Astot[cm²];Mdtot[kNm]";

    /// <summary>
    /// CSV řádek
    /// </summary>
    public string ToCsv()
    {
        return $"{Name};{EpsTop:F2};{EpsBottom:F2};{EpsS1:F2};{EpsS2:F2};" +
               $"{Fc:F2};{Fs1:F2};{Fs2:F2};" +
               $"{As1:F2};{As2:F2};" +
               $"{N:F2};{M:F2};" +
               $"{As:F2};{Md:F2};" +
               $"{Astot:F2};{Mdtot:F2}";
    }
}

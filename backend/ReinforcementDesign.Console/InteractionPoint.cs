namespace ReinforcementDesign;

/// <summary>
/// Bod interakčního diagramu s výsledky pro Variantu 2 (pouze dolní výztuž)
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

    // Síly od betonu [kN] a [kNm]
    public double Fc { get; set; }
    public double Mc { get; set; }

    // Síla od dolní výztuže [kN]
    public double Fs2 { get; set; }

    // Celkové vnitřní síly [kN] a [kNm]
    public double N { get; set; }
    public double M { get; set; }

    // VARIANTA 2: Pouze dolní výztuž (As1 = 0)
    public double As2 { get; set; }  // [cm²]
    public double Md { get; set; }   // [kNm] - moment odpovídající dané normálové síle N

    public override string ToString()
    {
        return $"{Name,-20} | εtop={EpsTop,7:F2}‰ εbot={EpsBottom,7:F2}‰ | " +
               $"εs1={EpsS1,7:F2}‰ εs2={EpsS2,7:F2}‰ | " +
               $"Fc={Fc,8:F2}kN Mc={Mc,8:F2}kNm | " +
               $"Fs2={Fs2,7:F2}kN | " +
               $"N={N,8:F2}kN M={M,8:F2}kNm | " +
               $"As2={As2,7:F2}cm² Md={Md,8:F2}kNm";
    }

    /// <summary>
    /// CSV hlavička
    /// </summary>
    public static string CsvHeader =>
        "Bod;εtop[‰];εbottom[‰];εs1[‰];εs2[‰];" +
        "Fc[kN];Mc[kNm];Fs2[kN];" +
        "N[kN];M[kNm];" +
        "As2[cm²];Md[kNm]";

    /// <summary>
    /// CSV řádek
    /// </summary>
    public string ToCsv()
    {
        return $"{Name};{EpsTop:F2};{EpsBottom:F2};{EpsS1:F2};{EpsS2:F2};" +
               $"{Fc:F2};{Mc:F2};{Fs2:F2};" +
               $"{N:F2};{M:F2};" +
               $"{As2:F2};{Md:F2}";
    }
}

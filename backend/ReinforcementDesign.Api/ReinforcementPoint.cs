namespace ReinforcementDesign;

/// <summary>
/// Bod interakčního diagramu s výpočtem výztuže
/// Rozšiřuje ConcretePoint o výpočty ploch výztuže
/// </summary>
public class ReinforcementPoint : ConcretePoint
{
    /// <summary>
    /// Plocha horní výztuže [cm²] - optimální řešení pro návrhové zatížení
    /// </summary>
    public double As1 { get; set; }

    /// <summary>
    /// Plocha dolní výztuže [cm²] - optimální řešení pro návrhové zatížení
    /// </summary>
    public double As2 { get; set; }

    /// <summary>
    /// Plocha pouze dolní výztuže [cm²] - varianta As1 = 0
    /// </summary>
    public double As { get; set; }

    /// <summary>
    /// Moment pro variantu pouze dolní výztuž [kNm]
    /// </summary>
    public double Md { get; set; }

    /// <summary>
    /// Celková plocha výztuže [cm²] - varianta As1 = As2 = Astot/2
    /// </summary>
    public double Astot { get; set; }

    /// <summary>
    /// Moment pro variantu rovnoměrné rozložení [kNm]
    /// </summary>
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
    /// CSV hlavička pro body s výztuží
    /// </summary>
    public new static string CsvHeader =>
        "Bod;εtop[‰];εbottom[‰];εs1[‰];εs2[‰];" +
        "Fc[kN];Fs1[kN];Fs2[kN];" +
        "As1[cm²];As2[cm²];" +
        "N[kN];M[kNm];" +
        "As[cm²];Md[kNm];" +
        "Astot[cm²];Mdtot[kNm]";

    /// <summary>
    /// CSV řádek s výztuží
    /// </summary>
    public override string ToCsv()
    {
        // Pro kompatibilitu s InteractionPoint - zjednodušená verze
        return $"{Name};{EpsTop:F2};{EpsBottom:F2};{EpsAs1:F2};{EpsAs2:F2};" +
               $"{N:F2};0.00;0.00;" +  // Fc, Fs1, Fs2 - zatím nevypočítáváme
               $"{As1:F2};{As2:F2};" +
               $"{N:F2};{M:F2};" +
               $"{As:F2};{Md:F2};" +
               $"{Astot:F2};{Mdtot:F2}";
    }
}

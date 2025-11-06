namespace ReinforcementDesign;

/// <summary>
/// Bod interakčního diagramu - základní třída s betonovými silami
/// </summary>
public class ConcretePoint
{
    /// <summary>
    /// Název bodu
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Sklon přetvoření [1/m]
    /// </summary>
    public double K { get; set; }

    /// <summary>
    /// Přetvoření v těžišti [-]
    /// </summary>
    public double Q { get; set; }

    /// <summary>
    /// Přetvoření na horním okraji [‰]
    /// </summary>
    public double EpsTop { get; set; }

    /// <summary>
    /// Přetvoření na dolním okraji [‰]
    /// </summary>
    public double EpsBottom { get; set; }

    /// <summary>
    /// Přetvoření v horní výztuži [‰]
    /// </summary>
    public double EpsAs1 { get; set; }

    /// <summary>
    /// Přetvoření v dolní výztuži [‰]
    /// </summary>
    public double EpsAs2 { get; set; }

    /// <summary>
    /// Napětí v horní výztuži [MPa]
    /// </summary>
    public double SigAs1 { get; set; }

    /// <summary>
    /// Napětí v dolní výztuži [MPa]
    /// </summary>
    public double SigAs2 { get; set; }

    /// <summary>
    /// Normálová síla [kN] - pouze od betonu
    /// </summary>
    public double N { get; set; }

    /// <summary>
    /// Moment [kNm] - pouze od betonu
    /// </summary>
    public double M { get; set; }

    public override string ToString()
    {
        return $"{Name,-20} | k={K,12:F6} q={Q,10:F6} | " +
               $"εtop={EpsTop,7:F2}‰ εbot={EpsBottom,7:F2}‰ | " +
               $"εAs1={EpsAs1,7:F2}‰ εAs2={EpsAs2,7:F2}‰ | " +
               $"σAs1={SigAs1,7:F1}MPa σAs2={SigAs2,7:F1}MPa | " +
               $"N={N,8:F2} kN | M={M,8:F2} kNm";
    }

    /// <summary>
    /// CSV hlavička
    /// </summary>
    public static string CsvHeader =>
        "Bod;k[1/m];q[-];εtop[‰];εbottom[‰];εAs1[‰];εAs2[‰];σAs1[MPa];σAs2[MPa];N[kN];M[kNm]";

    /// <summary>
    /// CSV řádek
    /// </summary>
    public virtual string ToCsv()
    {
        return $"{Name};{K:F6};{Q:F6};{EpsTop:F2};{EpsBottom:F2};{EpsAs1:F2};{EpsAs2:F2};" +
               $"{SigAs1:F2};{SigAs2:F2};{N:F2};{M:F2}";
    }
}

namespace ReinforcementDesign;

/// <summary>
/// Vlastnosti betonu podle EC2
/// </summary>
public class ConcreteProperties
{
    /// <summary>
    /// Návrhová pevnost v tlaku [Pa] (záporná pro tlak)
    /// </summary>
    public double Fcd { get; set; }

    /// <summary>
    /// Přetvoření na konci 2. větve [-]
    /// </summary>
    public double EpsC2 { get; set; }

    /// <summary>
    /// Mezní tlakové přetvoření [-]
    /// </summary>
    public double EpsCu { get; set; }

    public ConcreteProperties()
    {
        Fcd = -20e6;      // -20 MPa
        EpsC2 = -0.002;   // -2‰
        EpsCu = -0.0035;  // -3.5‰
    }
}

/// <summary>
/// Vlastnosti betonářské výztuže
/// </summary>
public class SteelProperties
{
    /// <summary>
    /// Návrhová mez kluzu [Pa]
    /// </summary>
    public double Fyd { get; set; }

    /// <summary>
    /// Maximální napětí na šikmé větvi [Pa]
    /// </summary>
    public double KFyd { get; set; }

    /// <summary>
    /// Modul pružnosti [Pa]
    /// </summary>
    public double Es { get; set; }

    /// <summary>
    /// Mezní přetvoření výztuže [-]
    /// </summary>
    public double EpsUd { get; set; }

    /// <summary>
    /// Mez kluzu (přetvoření) [-]
    /// </summary>
    public double EpsYd => Fyd / Es;

    public SteelProperties()
    {
        Fyd = 435e6;      // 435 MPa
        KFyd = 500e6;     // 500 MPa
        Es = 200e9;       // 200 GPa
        EpsUd = 0.01;     // 10‰
    }
}

/// <summary>
/// Geometrie průřezu
/// </summary>
public class CrossSectionGeometry
{
    /// <summary>
    /// Šířka průřezu [m]
    /// </summary>
    public double B { get; set; }

    /// <summary>
    /// Výška průřezu [m]
    /// </summary>
    public double H { get; set; }

    /// <summary>
    /// Vzdálenost horní výztuže od horního okraje [m]
    /// </summary>
    public double Layer1Distance { get; set; }

    /// <summary>
    /// Vzdálenost dolní výztuže od dolního okraje [m]
    /// </summary>
    public double Layer2YPos { get; set; }

    /// <summary>
    /// Absolutní souřadnice horní výztuže [m]
    /// </summary>
    public double Y1 => H - Layer1Distance;

    /// <summary>
    /// Absolutní souřadnice dolní výztuže [m]
    /// </summary>
    public double Y2 => Layer2YPos;

    /// <summary>
    /// Normalizovaná pozice horní výztuže (0 = spodek, 1 = vršek)
    /// </summary>
    public double Y1Norm => Y1 / H;

    /// <summary>
    /// Normalizovaná pozice dolní výztuže (0 = spodek, 1 = vršek)
    /// </summary>
    public double Y2Norm => Y2 / H;

    public CrossSectionGeometry()
    {
        B = 0.3;              // 300 mm
        H = 0.5;              // 500 mm
        Layer1Distance = 0.05; // 50 mm
        Layer2YPos = 0.05;     // 50 mm
    }
}

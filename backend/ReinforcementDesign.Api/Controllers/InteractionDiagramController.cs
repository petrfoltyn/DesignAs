using Microsoft.AspNetCore.Mvc;

namespace ReinforcementDesign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InteractionDiagramController : ControllerBase
{
    [HttpPost("calculate")]
    public ActionResult<InteractionDiagramResponse> Calculate([FromBody] InteractionDiagramRequest request)
    {
        try
        {
            // Vytvoření geometrie
            var geometry = new CrossSectionGeometry
            {
                B = request.B ?? 0.3,
                H = request.H ?? 0.5,
                Layer1Distance = request.Layer1Distance ?? 0.05,
                Layer2YPos = request.Layer2YPos ?? 0.05
            };

            // Vlastnosti materiálů
            var concrete = new ConcreteProperties
            {
                Fcd = request.Fcd ?? -20e6,
                EpsC2 = request.EpsC2 ?? -0.002,
                EpsCu = request.EpsCu ?? -0.0035
            };

            var steel = new SteelProperties
            {
                Fyd = request.Fyd ?? 435e6,
                Es = request.Es ?? 200e9,
                EpsUd = request.EpsUd ?? 0.01
            };

            // Vytvoření diagramu
            var diagram = new InteractionDiagram(geometry, concrete, steel);
            diagram.SetDesignLoads(request.NDesign ?? 0, request.MDesign ?? 30);

            // Výpočet s hustotou 10 pro všechny intervaly
            int[] densities = request.Densities ?? new int[] { 10, 10, 10, 10, 10, 10, 10, 10 };
            var points = diagram.Calculate(densities);

            // Vrátit výsledky
            return Ok(new InteractionDiagramResponse
            {
                Points = points,
                Geometry = new GeometryInfo
                {
                    B = geometry.B,
                    H = geometry.H,
                    Y1 = geometry.Y1,
                    Y2 = geometry.Y2
                },
                Materials = new MaterialInfo
                {
                    Fcd = concrete.Fcd,
                    Fyd = steel.Fyd,
                    Es = steel.Es
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("calculate-concrete-only")]
    public ActionResult<ConcreteOnlyDiagramResponse> CalculateConcreteOnly([FromBody] InteractionDiagramRequest request)
    {
        try
        {
            // Vytvoření geometrie
            var geometry = new CrossSectionGeometry
            {
                B = request.B ?? 0.3,
                H = request.H ?? 0.5,
                Layer1Distance = request.Layer1Distance ?? 0.05,
                Layer2YPos = request.Layer2YPos ?? 0.05
            };

            // Vlastnosti betonu
            var concrete = new ConcreteProperties
            {
                Fcd = request.Fcd ?? -20e6,
                EpsC2 = request.EpsC2 ?? -0.002,
                EpsCu = request.EpsCu ?? -0.0035
            };

            // Vlastnosti oceli (potřeba pro výpočet charakteristických bodů)
            var steel = new SteelProperties
            {
                Fyd = request.Fyd ?? 435e6,
                Es = request.Es ?? 200e9,
                EpsUd = request.EpsUd ?? 0.01
            };

            // Zahuštění (8 intervalů mezi 9 body: 1-2, 2-2b, 2b-3, 3-4, 4-5, 5-6, 6-7, 7-8)
            int[] densities = request.Densities ?? new int[] { 10, 10, 10, 10, 10, 10, 10, 10 };

            // Charakteristické body přetvoření
            // Převod z εtop, εbottom na k, q podle vzorce:
            // k = (εtop - εbottom) / h
            // q = εtop - k * (h/2)

            // BOD 1: Dostředný tlak (εtop = εbottom = εcu)
            var bod1_k = 0.0;
            var bod1_q = concrete.EpsCu;

            // BOD 2: TOP = εcu, BOTTOM = εc2
            var bod2_k = (concrete.EpsCu - concrete.EpsC2) / geometry.H;
            var bod2_q = concrete.EpsCu - bod2_k * (geometry.H / 2);

            // BOD 2b: TOP = εcu, BOTTOM = 0
            var bod2b_k = (concrete.EpsCu - 0.0) / geometry.H;
            var bod2b_q = concrete.EpsCu - bod2b_k * (geometry.H / 2);

            // BOD 3: TOP = εcu, εs2 = εyd (dolní výztuž v mezi kluzu)
            // Musíme najít εbottom tak, aby εs2 = εyd
            // ε(y) = k*y + q, kde y je lokální souřadnice od těžiště
            // y2 = geometry.Y2 - geometry.H/2 (lokální souřadnice dolní výztuže)
            // εs2 = k*y2 + q
            // Pro εtop: εtop = k*(h/2) + q
            // Pro εbottom: εbottom = k*(-h/2) + q
            // Z εtop a εbottom: k = (εtop - εbottom)/h, q = εtop - k*(h/2)
            // Chceme: εs2 = εyd, εtop = εcu
            // εyd = k*y2 + q = k*y2 + (εcu - k*(h/2))
            // εyd = k*y2 + εcu - k*(h/2)
            // εyd = εcu + k*(y2 - h/2)
            // k = (εyd - εcu) / (y2 - h/2)
            // Ale y2 = geometry.Y2 - h/2, takže y2 - h/2 = geometry.Y2 - h
            // k = (εyd - εcu) / (geometry.Y2 - geometry.H)
            // εbottom = k*(-h/2) + q = k*(-h/2) + εcu - k*(h/2) = εcu - k*h
            var epsYd = steel.Fyd / steel.Es;
            var y2_local = geometry.Y2 - geometry.H / 2;
            var bod3_k = (epsYd - concrete.EpsCu) / (y2_local - geometry.H / 2);
            var bod3_epsBottom = concrete.EpsCu - bod3_k * geometry.H;
            var bod3_q = concrete.EpsCu - bod3_k * (geometry.H / 2);

            // BOD 4: TOP = εcu, εs2 = εud (dolní výztuž v mezi pevnosti)
            var epsUd = steel.EpsUd;
            var bod4_k = (epsUd - concrete.EpsCu) / (y2_local - geometry.H / 2);
            var bod4_epsBottom = concrete.EpsCu - bod4_k * geometry.H;
            var bod4_q = concrete.EpsCu - bod4_k * (geometry.H / 2);

            // BOD 5: TOP = εc2, εs2 = εud
            var bod5_k = (epsUd - concrete.EpsC2) / (y2_local - geometry.H / 2);
            var bod5_epsBottom = concrete.EpsC2 - bod5_k * geometry.H;
            var bod5_q = concrete.EpsC2 - bod5_k * (geometry.H / 2);

            // BOD 6: TOP = 0, εs2 = εud
            var bod6_k = (epsUd - 0.0) / (y2_local - geometry.H / 2);
            var bod6_epsBottom = 0.0 - bod6_k * geometry.H;
            var bod6_q = 0.0 - bod6_k * (geometry.H / 2);

            // BOD 7: εs1 = εyd, εs2 = εud (obě výztuže v mezi)
            // εs1 = k*y1 + q = εyd
            // εs2 = k*y2 + q = εud
            // Z těchto dvou rovnic:
            // k*(y1 - y2) = εyd - εud
            // k = (εyd - εud) / (y1 - y2)
            var y1_local = geometry.Y1 - geometry.H / 2;
            var bod7_k = (epsYd - epsUd) / (y1_local - y2_local);
            // q = εyd - k*y1
            var bod7_q = epsYd - bod7_k * y1_local;
            // εtop = k*(h/2) + q
            var bod7_epsTop = bod7_k * (geometry.H / 2) + bod7_q;
            // εbottom = k*(-h/2) + q
            var bod7_epsBottom = bod7_k * (-geometry.H / 2) + bod7_q;

            // BOD 8: Čistý tah (εtop = εbottom = εud)
            var bod8_k = 0.0;
            var bod8_q = epsUd;

            var characteristicPoints = new[]
            {
                (k: bod1_k, q: bod1_q, name: "Bod 1"),
                (k: bod2_k, q: bod2_q, name: "Bod 2"),
                (k: bod2b_k, q: bod2b_q, name: "Bod 2b"),
                (k: bod3_k, q: bod3_q, name: "Bod 3"),
                (k: bod4_k, q: bod4_q, name: "Bod 4"),
                (k: bod5_k, q: bod5_q, name: "Bod 5"),
                (k: bod6_k, q: bod6_q, name: "Bod 6"),
                (k: bod7_k, q: bod7_q, name: "Bod 7"),
                (k: bod8_k, q: bod8_q, name: "Bod 8")
            };

            var points = new List<ConcreteOnlyPoint>();

            // Výpočet charakteristických bodů
            for (int i = 0; i < characteristicPoints.Length; i++)
            {
                var (k, q, name) = characteristicPoints[i];
                var forces = ConcreteIntegration.FastConcreteNM(
                    geometry.B, geometry.H, k, q, concrete.Fcd);

                points.Add(new ConcreteOnlyPoint
                {
                    Name = name,
                    K = k,
                    Q = q,
                    N = forces.N / 1000.0,  // kN
                    M = forces.M / 1000.0   // kNm
                });

                // Interpolace mezi charakteristickými body
                if (i < characteristicPoints.Length - 1)
                {
                    var (kNext, qNext, _) = characteristicPoints[i + 1];
                    int density = densities[Math.Min(i, densities.Length - 1)];

                    for (int j = 1; j <= density; j++)
                    {
                        double t = (double)j / (density + 1);
                        double kInterp = k + t * (kNext - k);
                        double qInterp = q + t * (qNext - q);

                        var forcesInterp = ConcreteIntegration.FastConcreteNM(
                            geometry.B, geometry.H, kInterp, qInterp, concrete.Fcd);

                        points.Add(new ConcreteOnlyPoint
                        {
                            Name = null,
                            K = kInterp,
                            Q = qInterp,
                            N = forcesInterp.N / 1000.0,
                            M = forcesInterp.M / 1000.0
                        });
                    }
                }
            }

            // Vrátit výsledky
            return Ok(new ConcreteOnlyDiagramResponse
            {
                Points = points,
                Geometry = new GeometryInfo
                {
                    B = geometry.B,
                    H = geometry.H,
                    Y1 = geometry.Y1,
                    Y2 = geometry.Y2
                },
                Materials = new MaterialInfo
                {
                    Fcd = concrete.Fcd,
                    Fyd = 0,
                    Es = 0
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("design-reinforcement")]
    public ActionResult<ReinforcementDesignResponse> DesignReinforcement([FromBody] InteractionDiagramRequest request)
    {
        try
        {
            // Vytvoření geometrie
            var geometry = new CrossSectionGeometry
            {
                B = request.B ?? 0.3,
                H = request.H ?? 0.5,
                Layer1Distance = request.Layer1Distance ?? 0.05,
                Layer2YPos = request.Layer2YPos ?? 0.05
            };

            // Vlastnosti materiálů
            var concrete = new ConcreteProperties
            {
                Fcd = request.Fcd ?? -20e6,
                EpsC2 = request.EpsC2 ?? -0.002,
                EpsCu = request.EpsCu ?? -0.0035
            };

            var steel = new SteelProperties
            {
                Fyd = request.Fyd ?? 435e6,
                Es = request.Es ?? 200e9,
                EpsUd = request.EpsUd ?? 0.01
            };

            // Návrhové zatížení
            double nDesign = request.NDesign ?? 0;
            double mDesign = request.MDesign ?? 30;

            // Vytvoření diagramu a nalezení návrhového bodu
            var diagram = new InteractionDiagram(geometry, concrete, steel);
            var designPoint = diagram.FindDesignPoint(
                nDesign,
                mDesign,
                toleranceRel: 0.01,
                toleranceAbs: 0.1,
                maxIterations: 50);

            // Vrátit výsledky
            return Ok(new ReinforcementDesignResponse
            {
                As2 = designPoint.As2,
                EpsTop = designPoint.EpsTop,
                EpsBottom = designPoint.EpsBottom,
                EpsS2 = designPoint.EpsS2,
                SigAs2 = SteelStress.CalculateStress(designPoint.EpsS2 / 1000, steel) / 1e6, // MPa
                N = designPoint.N,
                M = designPoint.M,
                NDesign = nDesign,
                MDesign = mDesign,
                ErrorAbs = Math.Abs(designPoint.M - mDesign),
                ErrorRel = Math.Abs(mDesign) > 1e-6 ? Math.Abs(designPoint.M - mDesign) / Math.Abs(mDesign) : 0,
                Name = designPoint.Name,
                Fc = designPoint.Fc,
                Mc = designPoint.Mc,
                Fs2 = designPoint.Fs2
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class ReinforcementDesignResponse
{
    public double As2 { get; set; }           // [cm²]
    public double EpsTop { get; set; }        // [‰]
    public double EpsBottom { get; set; }     // [‰]
    public double EpsS2 { get; set; }         // [‰]
    public double SigAs2 { get; set; }        // [MPa]
    public double N { get; set; }             // [kN]
    public double M { get; set; }             // [kNm]
    public double NDesign { get; set; }       // [kN]
    public double MDesign { get; set; }       // [kNm]
    public double ErrorAbs { get; set; }      // [kNm]
    public double ErrorRel { get; set; }      // [-]
    public string Name { get; set; } = "";
    public double Fc { get; set; }            // [kN]
    public double Mc { get; set; }            // [kNm]
    public double Fs2 { get; set; }           // [kN]
}

public class InteractionDiagramRequest
{
    // Geometrie
    public double? B { get; set; }
    public double? H { get; set; }
    public double? Layer1Distance { get; set; }
    public double? Layer2YPos { get; set; }

    // Beton
    public double? Fcd { get; set; }
    public double? EpsC2 { get; set; }
    public double? EpsCu { get; set; }

    // Ocel
    public double? Fyd { get; set; }
    public double? Es { get; set; }
    public double? EpsUd { get; set; }

    // Návrhové zatížení
    public double? NDesign { get; set; }
    public double? MDesign { get; set; }

    // Zahuštění
    public int[]? Densities { get; set; }
}

public class InteractionDiagramResponse
{
    public List<InteractionPoint> Points { get; set; } = new();
    public GeometryInfo Geometry { get; set; } = new();
    public MaterialInfo Materials { get; set; } = new();
}

public class GeometryInfo
{
    public double B { get; set; }
    public double H { get; set; }
    public double Y1 { get; set; }
    public double Y2 { get; set; }
}

public class MaterialInfo
{
    public double Fcd { get; set; }
    public double Fyd { get; set; }
    public double Es { get; set; }
}

public class ConcreteOnlyPoint
{
    public string? Name { get; set; }
    public double K { get; set; }
    public double Q { get; set; }
    public double N { get; set; }
    public double M { get; set; }
}

public class ConcreteOnlyDiagramResponse
{
    public List<ConcreteOnlyPoint> Points { get; set; } = new();
    public GeometryInfo Geometry { get; set; } = new();
    public MaterialInfo Materials { get; set; } = new();
}

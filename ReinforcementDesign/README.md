# ReinforcementDesign - Výpočet interakčního diagramu N-M

C# konzolová aplikace pro výpočet interakčního diagramu železobetonového průřezu podle Eurokódu EC2.

## Funkce

### 1. Integrace vnitřních sil betonu
- Parabolicko-obdélníkový diagram napětí podle EC2
- Přesná integrace s analytickým řešením
- Výpočet normálové síly N a momentu M od betonu

### 2. Interakční diagram N-M
- 8 charakteristických bodů (Bod 1 až Bod 8)
- Možnost individuálního zahuštění v každém intervalu
- Export výsledků do CSV

### 3. Tři metody návrhu výztuže

#### Metoda 1: Optimální As1 a As2
Řeší soustavu rovnic pro optimální rozložení výztuže:
```
Fc + As1·σ1 + As2·σ2 = N
Fc·yc + As1·σ1·y1 + As2·σ2·y2 = M
```

#### Metoda 2: Pouze dolní výztuž (As1 = 0)
Výztuž pouze ve spodní vrstvě:
```
Fc + As·σ2 = N  →  As = (N - Fc) / σ2
Md = Fc·yc + As·σ2·y2
```

#### Metoda 3: Rovnoměrné rozložení (As1 = As2 = Astot/2)
Stejná výztuž v obou vrstvách:
```
Fc + (Astot/2)·(σ1 + σ2) = N  →  Astot = 2·(N - Fc) / (σ1 + σ2)
Mdtot = Fc·yc + (Astot/2)·(σ1·y1 + σ2·y2)
```

## Struktura projektu

```
ReinforcementDesign/
├── MaterialProperties.cs      - Vlastnosti materiálů (beton, ocel, geometrie)
├── ConcreteIntegration.cs     - Integrace betonových sil (EC2 diagram)
├── SteelStress.cs            - Výpočet napětí ve výztuži (bilineární diagram)
├── InteractionPoint.cs        - Datová třída pro bod diagramu
├── InteractionDiagram.cs      - Hlavní třída pro výpočet diagramu
├── Program.cs                 - Ukázková aplikace
└── README.md                  - Dokumentace
```

## Použití

### Základní příklad

```csharp
// Vytvoření geometrie
var geometry = new CrossSectionGeometry
{
    B = 0.3,              // 300 mm
    H = 0.5,              // 500 mm
    Layer1Distance = 0.05, // 50 mm od horního okraje
    Layer2YPos = 0.05      // 50 mm od dolního okraje
};

// Vlastnosti materiálů
var concrete = new ConcreteProperties
{
    Fcd = -20e6,      // -20 MPa
    EpsC2 = -0.002,   // -2‰
    EpsCu = -0.0035   // -3.5‰
};

var steel = new SteelProperties
{
    Fyd = 435e6,      // 435 MPa
    Es = 200e9,       // 200 GPa
    EpsUd = 0.01      // 10‰
};

// Vytvoření diagramu
var diagram = new InteractionDiagram(geometry, concrete, steel);
diagram.SetDesignLoads(N_design: 0, M_design: 30);

// Výpočet s různým zahuštěním
int[] densities = new int[] { 5, 5, 5, 10, 10, 5, 5 };
var points = diagram.Calculate(densities);

// Export do CSV
using var writer = new StreamWriter("diagram.csv");
writer.WriteLine(InteractionPoint.CsvHeader);
foreach (var point in points)
{
    writer.WriteLine(point.ToCsv());
}
```

### Individuální zahuštění intervalů

Pole `densities` definuje počet dílů pro každý interval mezi charakteristickými body:

```csharp
int[] densities = new int[]
{
    5,  // Interval Bod 1 → Bod 2
    5,  // Interval Bod 2 → Bod 2b
    5,  // Interval Bod 2b → Bod 3
    10, // Interval Bod 3 → Bod 4 (hustější síť)
    10, // Interval Bod 4 → Bod 5 (hustější síť)
    5,  // Interval Bod 5 → Bod 6
    5   // Interval Bod 6 → Bod 7
};
```

## Spuštění

```bash
cd ReinforcementDesign
dotnet run
```

## Výstup

Program vypíše:
- Zadání parametrů
- Charakteristické body diagramu
- Statistiky (max/min hodnoty)
- Porovnání tří metod návrhu
- Export do CSV na plochu (Desktop)

### Formát CSV

CSV soubor obsahuje všechny vypočtené body s těmito sloupci:
- Bod, εtop, εbottom, εs1, εs2
- Fc, Fs1, Fs2
- As1, As2 (optimální návrh)
- N, M (celkové síly)
- As, Md (varianta jen dolní výztuž)
- Astot, Mdtot (varianta rovnoměrné rozložení)

## Charakteristické body

| Bod | Popis | εtop | εbottom |
|-----|-------|------|---------|
| Bod 1 | Dostředný tlak | εcu | εcu |
| Bod 2 | Maximální tlak v betonu | εcu | εc2 |
| Bod 2b | Neutralní osa na dolním okraji | εcu | 0 |
| Bod 3 | Dolní výztuž začíná téct | εcu | εyd |
| Bod 4 | Dolní výztuž na mezi únosnosti | εcu | εud |
| Bod 5 | Přechod parabolická → lineární | εc2 | εud |
| Bod 6 | Neutralní osa na horním okraji | 0 | εud |
| Bod 7 | Horní výztuž začíná téct | εyd | εud |
| Bod 8 | Čistý tah | εud | εud |

## Požadavky

- .NET 8.0 nebo novější

## Výstup programu

Program vypíše:

```
═══════════════════════════════════════════════════════════════════
  VÝPOČET INTERAKČNÍHO DIAGRAMU N-M PRO ŽELEZOBETONOVÝ PRŮŘEZ
  Design podle Eurokódu EC2 - Parabolicko-obdélníkový diagram
═══════════════════════════════════════════════════════════════════

ZADÁNÍ:
────────────────────────────────────────────────────────────────────
Geometrie:      b = 300 mm, h = 500 mm
Výztuž:         y1 = 450 mm, y2 = 50 mm
Beton:          fcd = 20.0 MPa
Ocel:           fyd = 435 MPa, Es = 200 GPa
Návrhové síly:  N = 0.0 kN, M = 30.0 kNm

VÝPOČET INTERAKČNÍHO DIAGRAMU:
────────────────────────────────────────────────────────────────────
Počítám body diagramu ✓ (51 bodů)

CHARAKTERISTICKÉ BODY:
────────────────────────────────────────────────────────────────────
Bod 1     | εtop=  -3.50‰ εbot=  -3.50‰ | N=    0.00kN M=   30.00kNm | ...
Bod 2     | εtop=  -3.50‰ εbot=  -2.00‰ | N=    0.00kN M=   30.00kNm | ...
...

✓ Data exportována do CSV: C:\Users\...\Desktop\interaction_diagram.csv
  Celkem 51 bodů

STATISTIKY:
────────────────────────────────────────────────────────────────────
Maximální normálová síla:
  N_max = ... kN při Bod ...

POROVNÁNÍ METOD NÁVRHU (pro Bod 4):
────────────────────────────────────────────────────────────────────
Metoda 1: Optimální As1 a As2
  As1 = -12.34 cm²
  As2 = 1.68 cm²
  Celkem: -10.67 cm²

Metoda 2: Pouze dolní výztuž (As1 = 0)
  As = 13.03 cm²
  Md = 227.50 kNm

Metoda 3: Rovnoměrné rozložení (As1 = As2 = Astot/2)
  Astot = 323.81 cm² (po 161.90 cm² v každé vrstvě)
  Mdtot = 2817.98 kNm
```

## Struktura výstupního CSV

CSV soubor je exportován na plochu s názvem `interaction_diagram.csv` a obsahuje následující sloupce:

- **Bod** - Název bodu
- **εtop[‰], εbottom[‰]** - Přetvoření na horním a dolním okraji
- **εs1[‰], εs2[‰]** - Přetvoření v horní a dolní výztuži
- **Fc[kN], Fs1[kN], Fs2[kN]** - Síly od betonu a výztuže
- **As1[cm²], As2[cm²]** - Optimální plochy výztuže (metoda 1)
- **N[kN], M[kNm]** - Celkové vnitřní síly
- **As[cm²], Md[kNm]** - Varianta pouze dolní výztuž (metoda 2)
- **Astot[cm²], Mdtot[kNm]** - Varianta rovnoměrné rozložení (metoda 3)

## Autor

Projekt vychází z webové aplikace DesignAs pro návrh podélné výztuže železobetonového průřezu.

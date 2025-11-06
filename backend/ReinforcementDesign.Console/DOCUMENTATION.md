# Dokumentace výpočtu železobetonového průřezu

## Přehled

Tento dokument popisuje výpočetní algoritmus pro návrh podélné výztuže železobetonového obdélníkového průřezu namáhaného kombinací normálové síly (N) a ohybového momentu (M) podle Eurokódu EC2.

## Teoretický základ

### Předpoklady

1. **Rovinnost průřezu**: Průřezy kolmé k ose prvku zůstávají i po deformaci rovinné a kolmé k deformované ose (hypotéza rovinných průřezů, Bernoulliho-Navierova hypotéza)
2. **Lineární rozdělení přetvoření**: Přetvoření se v průřezu mění lineárně
3. **Materiálové modely**:
   - Beton v tahu se zanedbává (porušený průřez)
   - Beton v tlaku: parabolicko-obdélníkový diagram (EC2)
   - Ocel: bilineární diagram s mezí kluzu

### Souřadnicové systémy

#### Globální souřadnice (geometrie)
- Y = 0 na dolním okraji průřezu
- Y = h na horním okraji průřezu
- Výztuž:
  - y₁ = vzdálenost horní výztuže od dolního okraje [m]
  - y₂ = vzdálenost dolní výztuže od dolního okraje [m]

#### Lokální souřadnice (výpočet)
- Y = 0 v těžišti průřezu
- Y = +h/2 na horním okraji
- Y = -h/2 na dolním okraji

**Převod**:
```
y_local = y_global - h/2
```

### Přetvoření

Lineární rozdělení přetvoření v průřezu je popsáno dvěma parametry:

```
ε(y) = k·y + q
```

Kde:
- **k** = sklon přetvoření [1/m]
- **q** = přetvoření v těžišti [-]
- **y** = lokální souřadnice [m]

**Výpočet z krajních přetvoření**:

```
k = (ε_top - ε_bottom) / h
q = (ε_top + ε_bottom) / 2
```

Nebo inverzně:

```
ε_top = k·(h/2) + q
ε_bottom = k·(-h/2) + q = -k·(h/2) + q
```

### Materiálové modely

#### Beton (Parabolicko-obdélníkový diagram EC2)

**Parametry**:
- f_cd = návrhová pevnost betonu v tlaku [Pa] (záporná hodnota)
- ε_c2 = mezní přetvoření konce paraboly (typicky -0.002 = -2‰)
- ε_cu = mezní přetvoření betonu (typicky -0.0035 = -3.5‰)

**Napěťově-deformační vztah**:

```
Pro ε > 0 (tah):
    σ_c = 0                                          (beton v tahu se zanedbává)

Pro ε_c2 < ε ≤ 0 (parabola):
    σ_c = f_cd · [1 - (1 - ε/ε_c2)²]

Pro ε_cu ≤ ε ≤ ε_c2 (konstanta):
    σ_c = f_cd
```

**Integrace sil od betonu**:

Síly od betonu se počítají integrací přes tlačenou část průřezu:

```
F_c = ∫∫ σ_c(y) dA = b · ∫ σ_c(y) dy

M_c = ∫∫ σ_c(y) · y dA = b · ∫ σ_c(y) · y dy
```

Implementováno v `ConcreteIntegration.FastConcreteNM()` metodě pomocí numerické integrace (Simpson 1/3).

#### Ocel (Bilineární diagram)

**Parametry**:
- f_yd = návrhová mez kluzu oceli [Pa]
- E_s = modul pružnosti oceli [Pa] (typicky 200 GPa)
- ε_yd = f_yd / E_s = přetvoření při mezi kluzu
- ε_ud = mezní přetvoření oceli (typicky 0.01 = 10‰)

**Napěťově-deformační vztah**:

```
Pro ε < -ε_yd (tlak nad mezí kluzu):
    σ_s = -f_yd

Pro -ε_yd ≤ ε ≤ ε_yd (pružná oblast):
    σ_s = E_s · ε

Pro ε > ε_yd (tah nad mezí kluzu):
    σ_s = f_yd
```

Implementováno v `SteelStress.CalculateStress()`.

## Výpočetní algoritmus

### Základní postup

#### 1. Výpočet interakčního diagramu betonu

**Třída**: `ConcreteDiagram`

Pro daný stav přetvoření (ε_top, ε_bottom) se spočítají:

1. Parametry přetvoření k, q
2. Síly od betonu (F_c, M_c)
3. Přetvoření a napětí ve výztuži (ε_s1, ε_s2, σ_s1, σ_s2)
4. Celkové vnitřní síly průřezu (N, M)

**Charakteristické body** (definovány geometrií a materiálem):

- **Bod 1**: Dostředný tlak (ε_top = ε_cu, ε_bottom = ε_cu)
- **Bod 2**: ε_top = ε_cu, ε_bottom = ε_c2
- **Bod 2b**: ε_top = ε_cu, ε_bottom = 0
- **Bod 3**: ε_top = ε_cu, ε_s2 = ε_yd (dolní výztuž na mezi kluzu)
- **Bod 4**: ε_top = ε_cu, ε_s2 = ε_ud (dolní výztuž na mezní přetvoření)
- **Bod 5**: ε_top = ε_c2, ε_s2 = ε_ud
- **Bod 6**: ε_top = 0, ε_s2 = ε_ud (nulové přetvoření v horním okraji)
- **Bod 7**: ε_s1 = ε_yd, ε_s2 = ε_ud (obě výztuže na mezi kluzu/mezním přetvoření)
- **Bod 8**: Dostředný tah (ε_top = ε_ud, ε_bottom = ε_ud)

Mezi charakteristickými body se generují zahuštěné body lineární interpolací přetvoření.

#### 2. Návrh výztuže (Varianta 2)

**Třída**: `InteractionDiagram`, metoda `CalculatePoint()`

Pro zadaný stav přetvoření a návrhové zatížení (N_design, M_design) se vypočítá:

**Krok 2.1**: Síly od betonu
```
(F_c, M_c) = ConcreteIntegration.FastConcreteNM(b, h, k, q, f_cd)
```

**Krok 2.2**: Napětí ve výztuži
```
ε_s2 = k · y₂_local + q
σ_s2 = SteelStress.CalculateStress(ε_s2, steel)
```

**Krok 2.3**: Výpočet plochy dolní výztuže z rovnováhy sil

**VARIANTA 2: Pouze dolní výztuž (As1 = 0)**

Rovnice rovnováhy sil:
```
F_c + F_s2 = N_design
F_c + As2 · σ_s2 = N_design
```

Z toho:
```
As2 = (N_design - F_c) / σ_s2
```

**Krok 2.4**: Výpočet momentu

Moment od výztuže (kolem těžiště):
```
M_s2 = F_s2 · (-y₂_local) = As2 · σ_s2 · (-y₂_local)
```

Celkový moment:
```
M = M_c + M_s2
```

Moment Md (možný moment pro danou normálovou sílu):
```
Md = M = M_c + As2 · σ_s2 · (-y₂_local)
```

#### 3. Iterační hledání návrhového bodu

**Třída**: `InteractionDiagram`, metoda `FindDesignPoint()`

**Cíl**: Najít stav přetvoření (ε_top, ε_bottom), aby vypočtený moment M přesně odpovídal M_design.

**Algoritmus**: Regula falsi (metoda sečen)

**Krok 3.1**: Inicializace
```
1. Spočítat interakční diagram betonu
2. Najít dva sousední body i, i+1, kde:
   M[i] ≤ M_design ≤ M[i+1]  (nebo opačně)
```

**Krok 3.2**: Iterace
```
Pro iter = 1 až max_iterations:

    # Regula falsi: lineární interpolace
    t = (M_design - M₁) / (M₂ - M₁)
    ε_top_new = ε₁_top + t · (ε₂_top - ε₁_top)
    ε_bottom_new = ε₁_bottom + t · (ε₂_bottom - ε₁_bottom)

    # Vypočítat nový bod
    point_new = CalculatePoint(ε_top_new, ε_bottom_new)
    M_new = point_new.M

    # Kontrola konvergence
    error_abs = |M_new - M_design|
    error_rel = error_abs / |M_design|

    Pokud error_abs < tolerance_abs NEBO error_rel < tolerance_rel:
        RETURN point_new  # Úspěch!

    # Aktualizace intervalů
    Pokud (M₁ - M_design) · (M_new - M_design) < 0:
        # Řešení leží mezi point₁ a point_new
        ε₂_top = ε_top_new
        ε₂_bottom = ε_bottom_new
        M₂ = M_new
    Jinak:
        # Řešení leží mezi point_new a point₂
        ε₁_top = ε_top_new
        ε₁_bottom = ε_bottom_new
        M₁ = M_new
```

**Parametry konvergence**:
- `tolerance_rel` = 0.01 (1%)
- `tolerance_abs` = 0.1 kN (100 N)
- `max_iterations` = 50

**Typická konvergence**: 1-5 iterací pro hladkou funkci M(ε)

## Datové struktury

### CrossSectionGeometry
Geometrie průřezu:
```csharp
public class CrossSectionGeometry
{
    double B;                // Šířka průřezu [m]
    double H;                // Výška průřezu [m]
    double Layer1Distance;   // Vzdálenost horní výztuže od horního okraje [m]
    double Layer2YPos;       // Vzdálenost dolní výztuže od dolního okraje [m]

    // Vypočtené vlastnosti
    double Y1;               // Globální souřadnice horní výztuže [m]
    double Y2;               // Globální souřadnice dolní výztuže [m]
    double Y1Norm;           // Normalizovaná poloha horní výztuže [-]
    double Y2Norm;           // Normalizovaná poloha dolní výztuže [-]
}
```

### ConcreteProperties
Vlastnosti betonu:
```csharp
public class ConcreteProperties
{
    double Fcd;    // Návrhová pevnost v tlaku [Pa] (záporná)
    double EpsC2;  // Mezní přetvoření konce paraboly [-] (typicky -0.002)
    double EpsCu;  // Mezní přetvoření betonu [-] (typicky -0.0035)
}
```

### SteelProperties
Vlastnosti oceli:
```csharp
public class SteelProperties
{
    double Fyd;    // Návrhová mez kluzu [Pa]
    double Es;     // Modul pružnosti [Pa]
    double EpsUd;  // Mezní přetvoření [-] (typicky 0.01)

    // Vypočtené vlastnosti
    double EpsYd;  // Přetvoření na mezi kluzu [-]
}
```

### Forces
Vnitřní síly:
```csharp
public class Forces
{
    double N;  // Normálová síla [N]
    double M;  // Moment [Nm]
}
```

### ConcretePoint
Bod interakčního diagramu betonu (bez výztuže):
```csharp
public class ConcretePoint
{
    string Name;        // Název bodu
    double K;           // Sklon přetvoření [1/m]
    double Q;           // Přetvoření v těžišti [-]
    double EpsTop;      // Přetvoření horního okraje [‰]
    double EpsBottom;   // Přetvoření dolního okraje [‰]
    double EpsAs1;      // Přetvoření horní výztuže [‰]
    double EpsAs2;      // Přetvoření dolní výztuže [‰]
    double SigAs1;      // Napětí horní výztuže [Pa]
    double SigAs2;      // Napětí dolní výztuže [Pa]
    double N;           // Normálová síla od betonu [kN]
    double M;           // Moment od betonu [kNm]
}
```

### InteractionPoint
Bod s návrhovou výztuží (rozšíření ConcretePoint):
```csharp
public class InteractionPoint : ConcretePoint
{
    double Fc;   // Síla od betonu [kN]
    double Mc;   // Moment od betonu [kNm]
    double Fs2;  // Síla od dolní výztuže [kN]
    double As2;  // Plocha dolní výztuže [cm²]
    double Md;   // Moment odpovídající N_design [kNm]
}
```

## Hlavní třídy a metody

### ConcreteIntegration
Výpočet sil od betonu:

```csharp
public static Forces FastConcreteNM(
    double b,      // Šířka [m]
    double h,      // Výška [m]
    double k,      // Sklon přetvoření [1/m]
    double q,      // Přetvoření v těžišti [-]
    double fcd)    // Pevnost betonu [Pa]
```

### SteelIntegration
Výpočet sil od výztuže:

```csharp
public static Forces FastSteelNM(
    double As,     // Plocha výztuže [m²]
    double y,      // Lokální souřadnice [m]
    double k,      // Sklon přetvoření [1/m]
    double q,      // Přetvoření v těžišti [-]
    SteelProperties steel)

public static double CalculateSigma(
    double y,      // Lokální souřadnice [m]
    double k,      // Sklon přetvoření [1/m]
    double q,      // Přetvoření v těžišti [-]
    SteelProperties steel)
```

### ReinforcementCalculator
Výpočet výztuže podle různých variant:

```csharp
// VARIANTA 1: Optimální As1 a As2 (minimální celková plocha)
public static OptimalResult CalculateOptimal(
    double nDesign,
    double mDesign,
    Forces concreteForces,
    double sigma1,
    double sigma2,
    double y1Local,
    double y2Local)

// VARIANTA 2: Pouze dolní výztuž (As1 = 0)
public static SingleLayerResult CalculateSingleLayer(
    double nDesign,
    Forces concreteForces,
    double sigma2,
    double y2Local)

// VARIANTA 3: Rovnoměrné rozložení (As1 = As2)
public static UniformResult CalculateUniform(
    double nDesign,
    Forces concreteForces,
    double sigma1,
    double sigma2,
    double y1Local,
    double y2Local)
```

### ConcreteDiagram
Interakční diagram pouze betonu:

```csharp
public List<ConcretePoint> Calculate(int[]? densities = null)
```

### InteractionDiagram
Interakční diagram s výztuží:

```csharp
public void SetDesignLoads(double n, double m)

public List<InteractionPoint> Calculate(int[]? densities = null)

public InteractionPoint FindDesignPoint(
    double nDesign,
    double mDesign,
    double toleranceRel = 0.01,
    double toleranceAbs = 0.1,
    int maxIterations = 50)
```

## Příklad výpočtu

### Zadání

**Geometrie**:
- b = 0.3 m (300 mm)
- h = 0.5 m (500 mm)
- Krytí horní výztuže: 50 mm → y₁ = 450 mm
- Krytí dolní výztuže: 50 mm → y₂ = 50 mm

**Materiály**:
- Beton: f_cd = -20 MPa, ε_c2 = -2‰, ε_cu = -3.5‰
- Ocel: f_yd = 435 MPa, E_s = 200 GPa, ε_ud = 10‰

**Zatížení**:
- N_design = 0 kN (čistý ohyb)
- M_design = 30 kNm

### Výpočet pomocí FindDesignPoint()

**Krok 1**: Inicializace
```
diagram = new InteractionDiagram(geometry, concrete, steel)
points = diagram.Calculate()  // 51 bodů
```

**Krok 2**: Nalezení intervalů
```
Hledám M_design = 30 kNm v seznamu bodů:
- Bod: "Bod 5-Bod 6 (4/5)", M = 8.61 kNm
- Bod: "Bod 5-Bod 6 (2/5)", M = 60.11 kNm
→ Řešení leží mezi těmito body
```

**Krok 3**: Iterace 1
```
t = (30 - 8.61) / (60.11 - 8.61) = 0.4153

Interpolace přetvoření:
ε_top = -0.40‰ + 0.4153 · (-1.20‰ - (-0.40‰)) = -0.79‰
ε_bottom = 11.16‰ + 0.4153 · (11.24‰ - 11.16‰) = 11.20‰

Výpočet nového bodu:
k = (-0.79‰ - 11.20‰) / 500 mm = -0.024 [1/m]
q = (-0.79‰ + 11.20‰) / 2 = 5.21‰

Přetvoření ve výztuži:
ε_s2 = k · (-0.2) + q = 10.00‰ (mezní přetvoření!)
σ_s2 = 435 MPa (mez kluzu)

Síly od betonu:
F_c = -68.33 kN
M_c = 16.30 kNm

Výpočet výztuže:
As2 = (0 - (-68.33)) / 435 = 0.000157 m² = 1.57 cm²

Moment:
M = 16.30 + 68.33 · 0.2 = 29.97 kNm

Kontrola:
error_abs = |29.97 - 30.00| = 0.0345 kNm ✓ < 0.1 kN
error_rel = 0.0345 / 30.00 = 0.11% ✓ < 1%

→ KONVERGENCE!
```

### Výsledek

**Nalezený bod**:
- Název: "Design point (converged after 1 iterations)"
- Přetvoření: ε_top = -0.79‰, ε_bottom = 11.20‰
- Přetvoření ve výztuži: ε_s1 = 0.41‰, ε_s2 = 10.00‰
- Síly od betonu: F_c = -68.33 kN, M_c = 16.30 kNm
- Síla od výztuže: F_s2 = 68.33 kN
- **Návrhová výztuž: As2 = 1.57 cm²**
- Celkové síly: N = 0.00 kN, M = 29.97 kNm
- Chyba: ΔM = 0.0345 kNm (0.11%)

## Ověření výsledků

### Kontrola rovnováhy sil

**Normálová síla**:
```
N = F_c + F_s2 = -68.33 + 68.33 = 0.00 kN ✓
```

**Moment** (kolem těžiště):
```
y₂_local = 50 - 250 = -200 mm = -0.2 m
M = M_c + F_s2 · (-y₂_local)
M = 16.30 + 68.33 · 0.2
M = 16.30 + 13.67
M = 29.97 kNm ✓
```

### Kontrola napětí a přetvoření

**Přetvoření na dolním okraji**:
```
ε_bottom = 11.20‰ ≈ ε_ud = 10‰ ✓ (blízko meznímu)
```

**Přetvoření dolní výztuže**:
```
ε_s2 = 10.00‰ = ε_ud ✓ (mezní přetvoření)
```

**Napětí dolní výztuže**:
```
σ_s2 = f_yd = 435 MPa ✓ (mez kluzu)
```

**Přetvoření horního okraje**:
```
ε_top = -0.79‰ > ε_cu = -3.5‰ ✓ (v mezích)
```

## Výhody algoritmu

1. **Rychlost**: Konvergence typicky v 1-5 iteracích
2. **Přesnost**: Relativní chyba < 1%, absolutní < 100 N
3. **Robustnost**: Automatické hledání vhodného intervalu
4. **Univerzálnost**: Funguje pro libovolné N a M v rozsahu interakčního diagramu

## Omezení

1. **Obdélníkový průřez**: Pouze obdélníkové průřezy
2. **Dvě vrstvy výztuže**: y₁ (horní), y₂ (dolní)
3. **Varianta 2**: Pouze dolní výztuž (As1 = 0)
4. **EC2**: Parabolicko-obdélníkový diagram betonu
5. **Rozsah**: Řešení musí ležet v rozsahu interakčního diagramu

## Reference

- Eurocode 2: Design of concrete structures (EN 1992-1-1)
- ČSN EN 1992-1-1: Navrhování betonových konstrukcí - Část 1-1: Obecná pravidla a pravidla pro pozemní stavby

## Autoři a verze

- **Verze**: 1.0
- **Datum**: 2025
- **Platforma**: .NET 8.0
- **Jazyk**: C#

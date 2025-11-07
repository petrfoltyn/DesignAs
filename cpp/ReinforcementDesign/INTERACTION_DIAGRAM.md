# Interaction Diagram Generation

## Overview

The program generates N-M interaction diagrams with **characteristic points** and **densification** (interpolation between points).

## Features

### 1. Characteristic Points (8 points)

Following EC2 methodology:

| Point | Description | Top Strain | Bottom Strain |
|-------|-------------|------------|---------------|
| **P1** | Pure compression | εcu | εcu |
| **P2** | Top ultimate, bottom peak | εcu | εc2 |
| **P2b** | Top ultimate, bottom zero | εcu | 0 |
| **P3** | Top ultimate, bottom steel yields | εcu | (εs2 = εyd) |
| **P4** | Top ultimate, bottom steel ultimate | εcu | (εs2 = εud) |
| **P5** | Top peak, bottom steel ultimate | εc2 | (εs2 = εud) |
| **P6** | Top zero, bottom steel ultimate | 0 | (εs2 = εud) |
| **P7** | Both reinforcement layers active | (εs1 = εyd) | (εs2 = εud) |
| **P8** | Pure tension | εud | εud |

### 2. Densification

Between each pair of characteristic points, the program interpolates **N additional points** (default: 10).

**Total points** = 8 characteristic + 7×10 interpolated = **78 points**

This creates a smooth N-M curve suitable for plotting.

### 3. Two Diagram Types

#### Concrete-Only Diagram
- As1 = 0, As2 = 0
- Shows capacity without reinforcement
- File: `interaction_diagram_concrete_only.csv`

#### With Reinforcement
- As1 = 0, As2 = user-specified (default: 10 cm²)
- Shows capacity with reinforcement
- File: `interaction_diagram_with_reinforcement.csv`

## Output Format

CSV files with columns:

```
Name, epsTop[o/oo], epsBot[o/oo], epsS1[o/oo], epsS2[o/oo],
sigS1[MPa], sigS2[MPa], N[kN], M[kNm], Fc[kN], Mc[kNm],
Fs1[kN], Fs2[kN], As1[cm^2], As2[cm^2]
```

### Key Columns:
- **N[kN]**: Axial force (+ tension, - compression)
- **M[kNm]**: Bending moment
- **epsTop, epsBot**: Strain at top/bottom fiber [per mille]
- **epsS1, epsS2**: Strain in reinforcement [per mille]
- **sigS1, sigS2**: Stress in reinforcement [MPa]
- **Fc, Mc**: Concrete forces
- **Fs1, Fs2**: Steel forces
- **As1, As2**: Reinforcement areas [cm²]

## Usage

### In Code

```cpp
// Create geometry, materials, etc.
SectionGeometry geom;
ConcreteProperties concrete;
SteelProperties steel;

// Generate concrete-only diagram
InteractionDiagram diagram(geom, concrete, steel, 0.0, 0.0);
auto points = diagram.Generate(10);  // 10 interpolation points
InteractionDiagram::ExportToCSV(points, "output.csv");

// Generate with reinforcement (As2 = 10 cm²)
double As2 = 10.0 / 10000.0;  // cm² to m²
InteractionDiagram diagramReinf(geom, concrete, steel, 0.0, As2);
auto pointsReinf = diagramReinf.Generate(10);
InteractionDiagram::ExportToCSV(pointsReinf, "output_reinf.csv");
```

### Using ReinforcementDesigner

```cpp
// Create designer - generates diagram once
ReinforcementDesigner designer(geom, concrete, steel, 10);

// Design for single load case (verbose output)
DesignLoads loads;
loads.N = 0.0;
loads.M = 30000.0;  // 30 kNm
DesignResult result = designer.Design(loads);  // verbose = true (default)

// Design for batch processing (silent mode)
std::vector<DesignLoads> batchLoads;
// ... populate loads ...
for (const auto& ld : batchLoads) {
    DesignResult res = designer.Design(ld, false);  // verbose = false for speed
    // ... process result ...
}
```

### Adjusting Densification

Change the number of interpolation points:

```cpp
auto points = diagram.Generate(5);   // Less dense (faster)
auto points = diagram.Generate(20);  // More dense (smoother)
```

## Plotting the Diagram

### Python Example

```python
import pandas as pd
import matplotlib.pyplot as plt

# Load data
df = pd.read_csv('interaction_diagram_with_reinforcement.csv')

# Plot N-M curve
plt.figure(figsize=(10, 8))
plt.plot(df['M[kNm]'], df['N[kN]'], 'b-', linewidth=2)
plt.xlabel('Moment M [kNm]')
plt.ylabel('Axial Force N [kN]')
plt.title('Interaction Diagram (N-M)')
plt.grid(True)
plt.axhline(y=0, color='k', linestyle='-', linewidth=0.5)
plt.axvline(x=0, color='k', linestyle='-', linewidth=0.5)
plt.show()
```

### Excel/Spreadsheet

1. Open CSV file in Excel
2. Select columns `M[kNm]` and `N[kN]`
3. Insert → Chart → Scatter plot with smooth lines

## Sign Convention

- **Positive N (+)**: Tension
- **Negative N (-)**: Compression
- **Positive M (+)**: Tension at bottom fiber, compression at top
- **Negative strain**: Compression
- **Positive strain**: Tension

## Typical Diagram Shape

```
      N (kN)
       ^
       |
  Tension (+)
       |
   P8  •
       |\
       | \
       |  \  P7
       |   •\
       |    \\
       |     \\  P6
-------|------•\----------> M (kNm)
       |      /•  P5
       |     //
       |    //
       |   •/  P4
       |  /
       | /  P3
       |/
   P1  •  P2  P2b
       |
  Compression (-)
       |
```

## Performance

- **Generation time**: ~1-10 ms for 78 points
- **File size**: ~10-20 KB per CSV

## Validation

Compare with C# backend results:
- Characteristic points should match exactly
- Interpolated points should follow smooth curve
- Check sign conventions match

## Limitations

- Rectangular cross-section only
- Uniaxial bending (M-N, not My-Mz-N)
- Linear strain distribution (plane sections remain plane)
- EC2 parabolic-rectangular concrete model
- Bilinear steel model

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **modular HTML5 application** for designing longitudinal reinforcement in reinforced concrete cross-sections according to Eurocode EC2. The application calculates required steel reinforcement areas (As1, As2) to resist combined normal force (N) and bending moment (M) loads.

**Language:** Czech (UI, variables, comments)
**Dependencies:** None - vanilla JavaScript with no external libraries

## File Structure

```
DesignAs/
├── index.html              # Main HTML file with structure and forms
├── css/
│   └── styles.css         # All application styles
├── js/
│   ├── calculations.js    # Mathematical calculations and algorithms
│   └── ui.js              # UI interactions and canvas drawing
└── reinforcement_design.html  # Original single-file version (backup)
```

## Architecture

### Application Structure

The application is organized into three main tabs:

1. **Zadání** (Input) - Define geometry, material properties, loads, and reinforcement layer positions
2. **Analýza přetvoření a napětí** (Strain/Stress Analysis) - Analyze strain distribution at selected points
3. **Návrh výztuže** (Reinforcement Design) - Calculate required reinforcement areas

### Core Algorithm

**Two-step process:**

1. **Strain Analysis** - Linear strain distribution across section:
   - ε(y) = k·y + q
   - k = slope [1/m], q = strain at centroid
   - Computed from two user-selected reference points

2. **Equilibrium System** - Solve for As1, As2:
   ```
   Force:  Fc + As1·σ1 + As2·σ2 = N
   Moment: Fc·yc + As1·σ1·y1 + As2·σ2·y2 = M
   ```
   - Fc = concrete force (parabolic-rectangular stress diagram)
   - Uses Cramer's rule for 2×2 linear system

### Key Functions

- `calculateReinforcement()` - Main solver, calculates As1, As2
- `calculateKQ()` - Computes strain parameters k, q from selected points
- `fastConcreteNM()` - Integrates parabolic-rectangular concrete stress diagram (EC2)
- `drawDesignCombinedDiagram()` - Renders stress distribution and internal forces on canvas
- `updateEquilibrium()` - Updates and displays equilibrium equations

### Material Models

**Concrete (EC2 parabolic-rectangular):**
- Parabolic zone: σ = fcd·[1 - (1 - ε/εc2)²] for εc2 < ε < 0
- Constant zone: σ = fcd for ε ≤ εc2 (typically εc2 = -0.002)
- No tension: σ = 0 for ε > 0

**Steel (bilinear):**
- Elastic: σ = ε·Es
- Yield plateau: σ = ±fyd (typically fyd = 435 MPa)

## Coordinate Systems

**Global (geometry):**
- Y = 0 at bottom edge
- Y = h at top edge

**Local (analysis):**
- Y = 0 at centroid
- Y = +h/2 at top, Y = -h/2 at bottom

Conversions are handled throughout the code.

## Development

### Making Changes

The application is now modular with separated concerns:
- **HTML structure:** `index.html` - Form inputs, tabs, and page structure
- **Styles:** `css/styles.css` - All visual styling and responsive design
- **Calculations:** `js/calculations.js` - Core mathematical functions
- **UI/Drawing:** `js/ui.js` - Event handlers, canvas drawing, DOM manipulation

No build process required - open `index.html` directly in a web browser. Changes take effect on page reload.

### Testing

- Open `index.html` in a modern browser (Chrome, Firefox, Edge)
- Test with typical values:
  - Geometry: b=0.3m, h=0.5m
  - Concrete: fcd=-20 MPa (-20e6 Pa)
  - Steel: fyd=435 MPa (435e6 Pa), Es=200 GPa
  - Loads: Various N and M combinations
- Verify equilibrium errors are near zero (displayed after calculation)

### Common Tasks

**Modify material models:**
- Edit `js/calculations.js`
- Locate `fastConcreteNM()` for concrete stress integration (line ~19)
- Bilinear steel model in `calculateReinforcement()` (lines ~245-250)

**Update UI layout:**
- Edit `css/styles.css` for visual changes
- Main tabs: `.main-tab-button` and `.main-tab-content` classes
- Canvas styles in `.canvas-container` and `.visualization` classes

**Adjust calculations:**
- All calculation functions are in `js/calculations.js`:
  - `calculateReinforcement()` - Main solver (line ~171)
  - `calculateKQ()` - Strain analysis (line ~144)
  - `fastConcreteNM()` - EC2 integration (line ~19)
  - Helper functions: `getPointY()`, `getPointName()`, `calculateZadani()`, `updateEquilibrium()`

**Modify UI behavior:**
- All UI logic is in `js/ui.js`:
  - Tab switching (lines ~40-72)
  - Canvas drawing: `drawSection()`, `drawStrainDiagram()`, `drawStressDiagram()`, `drawDesignCombinedDiagram()`
  - Event handlers and initialization (lines ~270+)

### Debug Output

The application includes extensive `console.log()` statements. Open browser DevTools to see:
- Input parameters
- Strain distribution (k, q values)
- Concrete forces (Fc, yc)
- Steel stresses and forces
- Equilibrium verification

## Input Parameters

Units are critical - all calculations use SI base units internally:

- **Geometry:** meters (m)
- **Stresses:** Pascals (Pa) - note fcd is negative for compression
- **Forces:** kilonewtons (kN) for input, converted to N internally
- **Moments:** kilonewton-meters (kNm)
- **Areas:** Output in cm² or mm²

## Known Constraints

- Designed for rectangular cross-sections only
- Two reinforcement layers (top and bottom)
- Linear strain distribution assumed (plane sections remain plane)
- No shear reinforcement design
- Concrete in tension ignored (cracked section analysis)

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **full-stack web application** for designing longitudinal reinforcement in reinforced concrete cross-sections according to Eurocode EC2. The application:
- Generates interaction diagrams (N-M diagrams) showing the relationship between axial force and bending moment capacity
- Designs required steel reinforcement areas (As1, As2) for specific load combinations using iterative algorithms
- Provides visual feedback through interactive diagrams and detailed strain/stress analysis

**Language:** Czech (UI, variables, comments)
**Architecture:** Frontend (HTML5/JavaScript) + Backend (ASP.NET Core Web API)

## File Structure

```
DesignAs/
├── frontend/
│   ├── index.html         # Main HTML interface with input forms and result displays
│   └── diagram.js         # JavaScript for API communication and diagram visualization
├── backend/
│   └── ReinforcementDesign.Api/
│       ├── Program.cs                              # ASP.NET Core entry point with CORS
│       ├── Controllers/
│       │   └── InteractionDiagramController.cs     # REST API endpoints
│       ├── InteractionDiagram.cs                   # Main diagram calculation with FindDesignPoint
│       ├── ReinforcementCalculator.cs              # Three-variant reinforcement calculations
│       ├── ConcreteIntegration.cs                  # Parabolic-rectangular concrete stress integration
│       ├── SteelIntegration.cs                     # Steel force calculations
│       ├── SteelStress.cs                          # Bilinear steel stress-strain model
│       ├── MaterialProperties.cs                   # Concrete and steel property classes
│       ├── ConcretePoint.cs                        # Concrete-only diagram points
│       ├── InteractionPoint.cs                     # Full interaction diagram points
│       ├── ReinforcementPoint.cs                   # Reinforcement calculation results
│       └── ConcreteDiagram.cs                      # Concrete-only diagram generation
└── README.md              # Project documentation
```

## Architecture

### Application Structure

The application consists of two main components:

**Frontend (HTML5/JavaScript):**
- Single-page application with form inputs for geometry and material properties
- Canvas-based visualization of interaction diagrams
- Two main workflows:
  1. **Generate Diagram** - Visualizes N-M interaction curves (concrete-only or with reinforcement)
  2. **Design Reinforcement** - Calculates required As2 for given N, M loads

**Backend (ASP.NET Core Web API):**
- RESTful API running on `http://localhost:5180`
- Two main endpoints:
  1. `POST /api/InteractionDiagram/calculate` - Generates full interaction diagram
  2. `POST /api/InteractionDiagram/design-reinforcement` - Designs reinforcement for specific loads
- CORS enabled for local development

### Core Algorithms

**1. Interaction Diagram Generation:**
- Computes characteristic points (pure compression, balanced, pure tension, etc.)
- Interpolates 10 points between each characteristic point for smooth curves
- Supports two diagram types:
  - **Concrete-only**: Shows capacity without reinforcement
  - **With reinforcement**: Shows capacity with specified As1 and As2

**2. Reinforcement Design (Regula Falsi Method):**
- Iteratively finds required As2 for given N, M by adjusting εtop
- Algorithm in `InteractionDiagram.FindDesignPoint()`:
  ```
  1. Start with two εtop bounds (εtop_min = εcu, εtop_max = εud)
  2. Calculate M for both bounds with As2 = 0 (concrete-only)
  3. Use regula falsi to interpolate new εtop
  4. Check if calculated M matches target M
  5. Update bounds and repeat until convergence
  ```
- Convergence criteria: |M_calc - M_target| < 1% relative or 0.1 kN absolute
- Maximum 50 iterations

**3. Section Analysis:**
- Linear strain distribution: ε(y) = εtop + (εbot - εtop) · y / h
- Concrete force integration using parabolic-rectangular diagram (EC2)
- Steel forces: Fs1, Fs2 based on bilinear stress-strain model
- Equilibrium:
  ```
  N = Fc + Fs1 + Fs2
  M = Fc·ec + Fs1·e1 + Fs2·e2  (about centroid)
  ```

### Key Classes and Methods

**Backend (C#):**
- `InteractionDiagram` - Main class coordinating diagram generation
  - `CalculatePoint()` - Calculates N, M for given strain distribution
  - `FindDesignPoint()` - Iterative solver for reinforcement design
  - `GenerateCharacteristicPoints()` - Creates key diagram points

- `ReinforcementCalculator` - Solves for As1, As2 given N, M, strain state
  - `Variant1()` - Both layers variable
  - `Variant2()` - Only As2 variable (As1 = 0) **[Currently used]**
  - `Variant3()` - Only As1 variable (As2 = 0)

- `ConcreteIntegration` - Numerical integration of concrete stress block
  - `CalculateForce()` - Returns Fc, Mc for parabolic-rectangular diagram

- `SteelStress` - Bilinear steel model
  - `CalculateStress()` - Returns σs for given εs

**Frontend (JavaScript):**
- `calculateDiagram()` - Calls API to generate and draw interaction diagram
- `designReinforcement()` - Calls API to design As2 for user-specified N, M
- `drawDiagram()` - Renders N-M curve on HTML5 canvas

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

### Setup and Running

**Backend API:**
```bash
cd backend/ReinforcementDesign.Api
dotnet run
```
API will start on `http://localhost:5180`

**Frontend:**
```bash
cd frontend
start index.html  # Windows
open index.html   # macOS
xdg-open index.html  # Linux
```

### Making Changes

**Backend (C# .NET 8.0):**
- **Calculations:** Edit classes in `backend/ReinforcementDesign.Api/`
  - `InteractionDiagram.cs` - Main diagram logic, FindDesignPoint algorithm
  - `ReinforcementCalculator.cs` - Three-variant solver for As1, As2
  - `ConcreteIntegration.cs` - Parabolic-rectangular stress integration
  - `SteelStress.cs` - Bilinear steel stress-strain model
- **API endpoints:** Edit `Controllers/InteractionDiagramController.cs`
- **Build:** `dotnet build` in project directory
- **Run:** `dotnet run` (with hot reload in development)

**Frontend (JavaScript):**
- **HTML structure:** `frontend/index.html` - Form inputs and result displays
- **API communication:** `frontend/diagram.js` - fetch calls and result rendering
  - `API_URL` constant (line ~1) - Update if API port changes
  - `calculateDiagram()` - Generates interaction diagram
  - `designReinforcement()` - Designs reinforcement for given loads
  - `drawDiagram()` - Canvas rendering

No build process for frontend - changes take effect on page reload.

### Testing

**Full workflow test:**
1. Start API backend: `cd backend/ReinforcementDesign.Api && dotnet run`
2. Open `frontend/index.html` in browser
3. Test diagram generation:
   - Use default values (b=0.3m, h=0.5m, layers=0.05m)
   - Select diagram type (concrete-only or with reinforcement)
   - Click "Vygenerovat diagram"
   - Verify diagram renders on canvas
4. Test reinforcement design:
   - Enter design loads (e.g., N=0 kN, M=30 kNm)
   - Click "Návrh výztuže"
   - Verify results display: As2, εtop, εbot, εAs2, σAs2
   - Check error percentage (should be <1% for convergence)

**Typical test values:**
- Geometry: b=0.3m, h=0.5m, layer1=0.05m, layer2=0.05m
- Concrete: fcd=-20 MPa, εc2=-0.002, εcu=-0.0035
- Steel: fyd=435 MPa, Es=200 GPa, εud=0.01
- Loads: N=0 to ±500 kN, M=0 to 100 kNm

### Common Tasks

**Add new API endpoint:**
1. Add method to `InteractionDiagramController.cs` with `[HttpPost("endpoint-name")]`
2. Create request/response DTO classes
3. Call calculation methods from existing classes
4. Add corresponding JavaScript function in `diagram.js`

**Modify material models:**
- **Concrete:** Edit `ConcreteIntegration.cs`
  - Parabolic zone calculation in `CalculateForce()` method
  - Change stress function: `σ = fcd * (1 - Math.Pow(1 - ε / εc2, 2))`
- **Steel:** Edit `SteelStress.cs`
  - Bilinear model in `CalculateStress()` method
  - Elastic branch: `σ = ε * Es`
  - Yield: `σ = ±fyd`

**Adjust convergence tolerances:**
- Edit `InteractionDiagram.FindDesignPoint()` parameters
- Default: `toleranceRel: 0.01` (1%), `toleranceAbs: 0.1` kN
- Increase for faster convergence, decrease for higher accuracy

**Change diagram point density:**
- Edit `InteractionDiagram.GenerateInterpolatedPoints()`
- Default: 10 points between each characteristic point
- Higher values = smoother curves but slower calculation

### Debug Output

**Backend console:**
- Detailed calculation logs for each FindDesignPoint iteration
- Shows εtop bounds, calculated M, error values
- Displays final convergence status

**Frontend browser console:**
- API request/response data
- Canvas drawing coordinates
- Error messages from failed API calls

## Input Parameters and Units

Units are critical - all calculations use SI base units internally, but UI displays common engineering units:

**Input (Frontend):**
- **Geometry:** meters (m)
- **Design loads:** kilonewtons (kN), kilonewton-meters (kNm)

**Internal (Backend):**
- **Geometry:** meters (m)
- **Stresses:** Pascals (Pa) - note fcd is negative for compression
- **Forces:** Newtons (N) internally, kN for API responses
- **Moments:** Newton-meters (Nm) internally, kNm for API responses
- **Strains:** Absolute values (e.g., -0.0035), displayed as permille (‰)

**Output (Frontend Display):**
- **Areas:** cm² (As1, As2)
- **Forces:** kN (N, Fc, Fs1, Fs2)
- **Moments:** kNm (M, Mc)
- **Stresses:** MPa (fcd, fyd, σs)
- **Strains:** ‰ permille (εtop, εbot, εs1, εs2)

## API Endpoints

**1. POST `/api/InteractionDiagram/calculate`**

Generates full interaction diagram with characteristic and interpolated points.

Request body:
```json
{
  "b": 0.3,              // [m] width
  "h": 0.5,              // [m] height
  "layer1Distance": 0.05, // [m] top reinforcement distance from top edge
  "layer2YPos": 0.05,    // [m] bottom reinforcement distance from bottom edge
  "as1": 5.0,            // [cm²] top reinforcement area (optional, for with-reinforcement diagram)
  "as2": 10.0,           // [cm²] bottom reinforcement area (optional)
  "fcd": -20e6,          // [Pa] concrete design strength (negative)
  "epsC2": -0.002,       // [-] concrete strain at peak stress
  "epsCu": -0.0035,      // [-] concrete ultimate strain
  "fyd": 435e6,          // [Pa] steel yield strength
  "es": 200e9,           // [Pa] steel elastic modulus
  "epsUd": 0.01,         // [-] steel ultimate strain
  "diagramType": "concrete-only"  // or "with-reinforcement"
}
```

Response: Array of points with N [kN], M [kNm], strains, stresses, etc.

**2. POST `/api/InteractionDiagram/design-reinforcement`**

Designs required As2 for given N, M using regula falsi iteration.

Request body:
```json
{
  "b": 0.3,
  "h": 0.5,
  "layer1Distance": 0.05,
  "layer2YPos": 0.05,
  "nDesign": 0,          // [kN] design axial force (+ tension, - compression)
  "mDesign": 30,         // [kNm] design moment
  "fcd": -20e6,
  "epsC2": -0.002,
  "epsCu": -0.0035,
  "fyd": 435e6,
  "es": 200e9,
  "epsUd": 0.01
}
```

Response:
```json
{
  "as2": 4.23,           // [cm²] required bottom reinforcement
  "epsTop": -1.85,       // [‰] strain at top fiber
  "epsBottom": 2.15,     // [‰] strain at bottom fiber
  "epsS2": 1.95,         // [‰] strain in As2
  "sigAs2": 390.5,       // [MPa] stress in As2
  "n": 0.0,              // [kN] calculated axial force
  "m": 29.98,            // [kNm] calculated moment
  "nDesign": 0.0,        // [kN] target axial force
  "mDesign": 30.0,       // [kNm] target moment
  "errorAbs": 0.02,      // [kNm] absolute error
  "errorRel": 0.0007,    // [-] relative error (0.07%)
  "name": "Design Point",
  "fc": -245.3,          // [kN] concrete force
  "mc": 28.5,            // [kNm] concrete moment
  "fs2": 16.5            // [kN] bottom steel force
}
```

## Known Constraints and Assumptions

- **Cross-section:** Rectangular only (no T-beams, L-beams, etc.)
- **Reinforcement:** Two discrete layers (top As1, bottom As2)
  - Current design workflow uses **Variant 2**: As1 = 0, only As2 is calculated
- **Strain distribution:** Linear (Bernoulli-Navier hypothesis - plane sections remain plane)
- **Material models:**
  - Concrete: Parabolic-rectangular per EC2 (no tension)
  - Steel: Bilinear elastic-perfectly plastic
- **Analysis:** Ultimate limit state (ULS) only, no serviceability checks
- **Loading:** Uniaxial bending + axial force (no biaxial bending)
- **Shear:** Not considered - only flexural capacity
- **Confinement:** Not considered - uses unconfined concrete properties

## Current Implementation Status

**Completed Features:**
- ✅ Full interaction diagram generation (concrete-only and with reinforcement)
- ✅ Characteristic point calculation (pure compression, balanced, pure tension, etc.)
- ✅ Point interpolation for smooth diagram curves (10 points between characteristic points)
- ✅ Reinforcement design using regula falsi iterative method (Variant 2: As1=0, As2 variable)
- ✅ RESTful API with CORS enabled
- ✅ Frontend visualization with HTML5 canvas
- ✅ Results display with strains, stresses, forces, moments
- ✅ Error checking and color-coded convergence indicators

**Not Yet Implemented:**
- ⏸️ Variant 1 design (both As1 and As2 variable) - algorithm exists but not exposed in UI/API
- ⏸️ Variant 3 design (As1 variable, As2=0) - algorithm exists but not exposed in UI/API
- ⏸️ Biaxial bending (My, Mz) - not planned
- ⏸️ Non-rectangular sections - not planned
- ⏸️ Shear design - not planned
- ⏸️ Serviceability limit state checks - not planned

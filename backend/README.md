# ReinforcementDesign Backend

Backend projekty pro návrh podélné výztuže železobetonového průřezu.

## 📁 Projekty v Solution

- **ReinforcementDesign.Api** - REST API server pro webovou aplikaci
- **ReinforcementDesign.Console** - Konzolová aplikace s CLI rozhraním

## 🚀 Spuštění pomocí Solution

### Otevřít v IDE

**Visual Studio:**
```bash
start ReinforcementDesign.sln
```

**Visual Studio Code:**
```bash
code .
```

**JetBrains Rider:**
```bash
rider ReinforcementDesign.sln
```

### Spuštění z příkazové řádky

**Build celého solution:**
```bash
dotnet build ReinforcementDesign.sln
```

**Spustit API server:**
```bash
dotnet run --project ReinforcementDesign.Api --urls "http://localhost:5000"
```

**Spustit Console aplikaci:**
```bash
dotnet run --project ReinforcementDesign.Console
```

## 📊 Projekty

### 1. ReinforcementDesign.Api

REST API server pro webovou aplikaci.

**Endpoints:**
- `POST /api/InteractionDiagram/calculate` - Výpočet interakčního diagramu s výztuží (tři metody)
- `POST /api/InteractionDiagram/calculate-concrete-only` - Výpočet interakčního diagramu pouze z betonu

**Swagger UI:**
- http://localhost:5000/swagger

**Spuštění:**
```bash
cd ReinforcementDesign.Api
dotnet run --urls "http://localhost:5000"
```

### 2. ReinforcementDesign.Console

Konzolová aplikace s pokročilými funkcemi.

**Funkce:**
- Výpočet interakčního diagramu
- Tři metody návrhu výztuže
- Export do CSV
- Individuální zahuštění intervalů

**Spuštění:**
```bash
cd ReinforcementDesign.Console
dotnet run
```

## 🛠️ Společné třídy

Oba projekty sdílejí tyto třídy:

- `MaterialProperties.cs` - Vlastnosti materiálů (beton, ocel, geometrie)
- `ConcreteIntegration.cs` - Integrace betonových sil (EC2)
- `SteelStress.cs` - Výpočet napětí ve výztuži
- `InteractionPoint.cs` - Datová třída pro body diagramu
- `InteractionDiagram.cs` - Hlavní třída pro výpočet

## 📋 Požadavky

- .NET 8.0 SDK

## 🔧 Vývoj

**Restore balíčků:**
```bash
dotnet restore ReinforcementDesign.sln
```

**Build:**
```bash
dotnet build ReinforcementDesign.sln
```

**Clean:**
```bash
dotnet clean ReinforcementDesign.sln
```

**Test (pokud existují):**
```bash
dotnet test ReinforcementDesign.sln
```

## 📖 Dokumentace

- **API projekt:** Swagger UI na http://localhost:5000/swagger
- **Console projekt:** `ReinforcementDesign.Console/README.md`

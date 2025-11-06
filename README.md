# DesignAs - Návrh podélné výztuže železobetonového průřezu

Komplexní projekt pro návrh a analýzu železobetonových průřezů podle Eurokódu EC2.

## 📁 Struktura projektu

```
DesignAs/
├── frontend/                  # Webová aplikace pro interakční diagram
│   ├── index.html            # Hlavní stránka
│   └── diagram.js            # JavaScript pro vykreslování
│
├── backend/
│   ├── ReinforcementDesign.Api/        # REST API backend
│   │   ├── Controllers/                # API kontrolery
│   │   ├── MaterialProperties.cs      # Třídy pro materiály
│   │   ├── ConcreteIntegration.cs     # EC2 integrace
│   │   ├── InteractionDiagram.cs      # Výpočet diagramu
│   │   └── Program.cs                  # API konfigurace
│   │
│   └── ReinforcementDesign.Console/    # Konzolová aplikace
│       ├── Program.cs                  # CLI aplikace
│       └── README.md                   # Dokumentace CLI
│
├── js/                        # Původní JavaScript aplikace
│   ├── calculations.js       # Výpočetní funkce
│   └── ui.js                 # UI funkce
│
├── css/
│   └── styles.css            # Styly původní aplikace
│
└── index.html                # Původní webová aplikace
```

## 🚀 Projekty

### 1. Webová aplikace pro interakční diagram (frontend/)

Jednoduchá webová aplikace pro vizualizaci interakčního diagramu N-M.

**Funkce:**
- Přepínání mezi diagramem **s výztuží** a **pouze betonem**
- Vykreslení interakčního diagramu s 10 body mezi charakteristickými body
- Interaktivní Canvas vizualizace s barevným rozlišením
- Statistiky (min/max hodnoty)
- Responzivní design
- Automatické přepočítání při změně typu diagramu

**Spuštění:**

1. Spustit backend API (viz níže)
2. Otevřít `frontend/index.html` v prohlížeči

### 2. REST API Backend (backend/ReinforcementDesign.Api/)

ASP.NET Core Web API pro výpočet interakčního diagramu.

**Funkce:**
- REST API endpoint pro výpočet diagramu
- CORS podpora pro frontend
- Swagger dokumentace

**Spuštění:**

```bash
cd backend/ReinforcementDesign.Api
dotnet run --urls "http://localhost:5000"
```

**API Endpoint:**

```
POST http://localhost:5000/api/InteractionDiagram/calculate
Content-Type: application/json

{
    "b": 0.3,
    "h": 0.5,
    "layer1Distance": 0.05,
    "layer2YPos": 0.05,
    "densities": [10, 10, 10, 10, 10, 10, 10, 10]
}
```

**Swagger UI:**
- http://localhost:5000/swagger

### 3. Konzolová aplikace (backend/ReinforcementDesign.Console/)

C# konzolová aplikace s pokročilými funkcemi.

**Funkce:**
- Výpočet interakčního diagramu
- Individuální zahuštění pro každý interval
- Tři metody návrhu výztuže (optimální, pouze dolní, rovnoměrné)
- Export do CSV

**Spuštění:**

```bash
cd backend/ReinforcementDesign.Console
dotnet run
```

### 4. Původní webová aplikace (index.html)

Komplexní JavaScript aplikace s kompletní funkcionalitou.

**Funkce:**
- Návrh výztuže pro zadané zatížení
- Analýza přetvoření a napětí
- Interakční diagram
- Canvas vizualizace průřezu

**Spuštění:**
- Otevřít `index.html` v prohlížeči

## 🛠️ Technologie

- **Frontend:** HTML5, CSS3, Vanilla JavaScript, Canvas API
- **Backend API:** ASP.NET Core 8.0, C#
- **Console App:** .NET 8.0, C#

## 📊 Výpočetní metody

### Integrace betonových sil
- Parabolicko-obdélníkový diagram podle EC2
- Analytická integrace pro přesné výsledky

### Interakční diagram
- 9 charakteristických bodů (Bod 1-8)
- Možnost zahuštění sítě mezi body
- Lineární interpolace přetvoření

### Tři metody návrhu výztuže

1. **Optimální As1 a As2** - Řeší soustavu rovnic pro minimální výztuž
2. **Pouze dolní výztuž (As1 = 0)** - Zjednodušený návrh
3. **Rovnoměrné rozložení (As1 = As2)** - Symetrická výztuž

## 📋 Požadavky

- .NET 8.0 SDK (pro backend a console)
- Moderní webový prohlížeč (Chrome, Firefox, Edge)

## 🎯 Rychlý start

### Webová aplikace s interakčním diagramem

**Varianta 1: Pomocí Solution (doporučeno)**
```bash
# 1. Otevřít solution v IDE
cd backend
start ReinforcementDesign.sln

# 2. Spustit projekt ReinforcementDesign.Api (F5 v IDE)

# 3. Otevřít frontend/index.html v prohlížeči
```

**Varianta 2: Z příkazové řádky**
```bash
# 1. Spustit API backend
cd backend
dotnet run --project ReinforcementDesign.Api --urls "http://localhost:5000"

# 2. Otevřít frontend v prohlížeči
# Otevřít frontend/index.html
```

### Konzolová aplikace

**Z Solution:**
```bash
cd backend
dotnet run --project ReinforcementDesign.Console
```

**Přímo:**
```bash
cd backend/ReinforcementDesign.Console
dotnet run
```

### Původní webová aplikace

```bash
# Jednoduše otevřít index.html v prohlížeči
```

## 📦 Solution

Backend projekty jsou seskupeny v `backend/ReinforcementDesign.sln`:

- **ReinforcementDesign.Api** - REST API server
- **ReinforcementDesign.Console** - CLI aplikace

**Otevřít v IDE:**
```bash
cd backend
start ReinforcementDesign.sln         # Visual Studio
code .                                 # VS Code
rider ReinforcementDesign.sln          # Rider
```

## 📖 Dokumentace

- **CLI aplikace:** `backend/ReinforcementDesign.Console/README.md`
- **Původní aplikace:** `CLAUDE.md`

## 👨‍💻 Autor

Projekt pro návrh podélné výztuže železobetonového průřezu podle Eurokódu EC2.

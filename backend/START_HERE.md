# 🚀 Jak spustit backend projekty

## 📝 Rychlý návod

### 1️⃣ Otevřít Solution

**Windows - Visual Studio:**
```bash
start ReinforcementDesign.sln
```

**Windows - Visual Studio Code:**
```bash
code .
```

**Windows - JetBrains Rider:**
```bash
rider ReinforcementDesign.sln
```

**Nebo:** Poklepat na `ReinforcementDesign.sln` v průzkumníku souborů

---

### 2️⃣ Vybrat projekt ke spuštění

V IDE nastavte **startup projekt**:

**Visual Studio:**
- Pravý klik na projekt → "Set as Startup Project"
- Nebo: Pravý klik na Solution → "Configure Startup Projects" → Multiple startup projects

**Visual Studio Code:**
- F5 → vybrat projekt z nabídky

**Rider:**
- Pravý klik na projekt → "Run"

---

### 3️⃣ Spustit projekt

**ReinforcementDesign.Api** (Web API):
- Spustí server na http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Nutné pro frontend aplikaci

**ReinforcementDesign.Console** (CLI):
- Spustí konzolovou aplikaci
- Vypočítá diagram a exportuje do CSV

---

## ⚡ Spuštění z příkazové řádky

### API Server
```bash
dotnet run --project ReinforcementDesign.Api --urls "http://localhost:5000"
```

### Console App
```bash
dotnet run --project ReinforcementDesign.Console
```

### Build celého solution
```bash
dotnet build ReinforcementDesign.sln
```

---

## 📖 Další informace

- **Dokumentace:** `README.md`
- **Console projekt:** `ReinforcementDesign.Console/README.md`
- **Hlavní README:** `../README.md`

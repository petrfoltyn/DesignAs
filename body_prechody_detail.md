# Detailní rozbor bodů a přechodů interakčního diagramu

## Princip přechodů

Každý přechod mezi body je definován:
- **Fixními hodnotami** - hodnoty, které zůstávají konstantní během přechodu
- **Měnící se hodnoty** - hodnoty, které se lineárně interpolují
- **Podmínky ukončení** - kdy přechod končí (dosažení cílové hodnoty)

Přetvoření je vždy **lineárně rozloženo po výšce průřezu** podle Bernoulliho hypotézy.

---

## Bod 1: Dostředný tlak

### Stav
- **εtop** = -3,5 ‰
- **εbottom** = -3,5 ‰

### Popis
Čistý dostředný tlak, všechna přetvoření jsou stejná po celé výšce průřezu.

---

## Přechod 1→2: Začátek otáčení neutrální osy

### Fixní hodnoty
- **εtop** = -3,5 ‰ (konstantní)

### Měnící se hodnoty
- **εbottom**: z -3,5 ‰ → na -2,0 ‰

### Výpočet ostatních přetvoření
Během tohoto přechodu se všechna přetvoření (εs1, εs2) mění lineárně podle pozice mezi TOP a BOTTOM.

### Koncový stav (Bod 2)
- **εtop** = -3,5 ‰
- **εbottom** = -2,0 ‰
- **εs1** = -3,4 ‰ (vypočteno lineárně)
- **εs2** = -2,1 ‰ (vypočteno lineárně)

### Popis
TOP zůstává na εcu (-3,5‰), BOTTOM se mění na εc2 (-2,0‰).

---

## Přechod 2→2b: BOTTOM na nulu

### Fixní hodnoty
- **εtop** = -3,5 ‰ (konstantní)

### Měnící se hodnoty
- **εbottom**: z -2,0 ‰ → na 0,0 ‰

### Výpočet ostatních přetvoření
εs1 a εs2 se lineárně mění podle aktuální hodnoty εbottom.

### Koncový stav (Bod 2b - BOTTOM nula)
- **εtop** = -3,5 ‰
- **εbottom** = 0,0 ‰
- **εs1** = (vypočteno lineárně podle pozice)
- **εs2** = (vypočteno lineárně podle pozice)

### Popis
Neutrální osa prochází dolním vláknem (εbottom = 0).

---

## Přechod 2b→3: A₂ dosahuje meze kluzu

### Fixní hodnoty
- **εtop** = -3,5 ‰ (konstantní)

### Měnící se hodnoty
- **εbottom**: postupně se zvyšuje z 0,0 ‰

### Podmínka ukončení
- **εs2 = 2,2 ‰** (mez kluzu εyd = 2,17 ‰)

### Výpočet
1. εtop zůstává na -3,5 ‰
2. εbottom se postupně zvyšuje
3. εs2 se vypočítává lineárně podle aktuálního εbottom
4. Když εs2 dosáhne 2,2 ‰, přechod končí

### Koncový stav (Bod 3)
- **εtop** = -3,5 ‰
- **εbottom** = 2,7 ‰ (výsledná hodnota, aby εs2 = 2,2 ‰)
- **εs1** = -3,0 ‰ (vypočteno lineárně)
- **εs2** = 2,2 ‰ (podmínka splněna - mez kluzu)

### Popis
Dolní výztuž (A₂) dosáhla meze kluzu. BOTTOM se zvýšil tak, aby εs2 na pozici dolní výztuže bylo přesně 2,2 ‰.

---

## Přechod 3→4: A₂ do plastické oblasti

### Fixní hodnoty
- **εtop** = -3,5 ‰ (konstantní)

### Měnící se hodnoty
- **εbottom**: postupně se zvyšuje z 2,7 ‰

### Podmínka ukončení
- **εs2 = 10,0 ‰** (plná plastifikace)

### Výpočet
1. εtop zůstává na -3,5 ‰
2. εbottom se postupně zvyšuje z 2,7 ‰
3. εs2 se vypočítává lineárně
4. Když εs2 dosáhne 10,0 ‰, přechod končí

### Koncový stav (Bod 4)
- **εtop** = -3,5 ‰
- **εbottom** = 11,2 ‰ (výsledná hodnota, aby εs2 = 10,0 ‰)
- **εs1** = -2,3 ‰ (vypočteno lineárně)
- **εs2** = 10,0 ‰ (podmínka splněna - plastická oblast)

### Popis
Dolní výztuž (A₂) je plně plastifikovaná (εs2 = 10 ‰).

---

## Přechod 4→5: TOP na εc2

### Fixní hodnoty
- **εs2** = 10,0 ‰ (konstantní - zůstává plastická)

### Měnící se hodnoty
- **εtop**: z -3,5 ‰ → na -2,0 ‰

### Výpočet
1. εs2 musí zůstat na 10,0 ‰ (fixní)
2. εtop se mění z -3,5 ‰ na -2,0 ‰
3. εbottom se musí přizpůsobit tak, aby εs2 zůstalo 10,0 ‰

### Koncový stav (Bod 5)
- **εtop** = -2,0 ‰ (εc2)
- **εbottom** = 11,1 ‰ (přizpůsobeno, aby εs2 = 10,0 ‰)
- **εs1** = -0,9 ‰ (vypočteno lineárně)
- **εs2** = 10,0 ‰ (fixní)

### Popis
TOP se snížilo na εc2 (parabolický diagram betonu), A₂ zůstává plastická.

---

## Přechod 5→6: TOP na nulu

### Fixní hodnoty
- **εs2** = 10,0 ‰ (konstantní - zůstává plastická)

### Měnící se hodnoty
- **εtop**: z -2,0 ‰ → na 0,0 ‰

### Výpočet
1. εs2 musí zůstat na 10,0 ‰ (fixní)
2. εtop se mění z -2,0 ‰ na 0,0 ‰
3. εbottom se přizpůsobuje

### Koncový stav (Bod 6)
- **εtop** = 0,0 ‰ (nula)
- **εbottom** = 10,9 ‰ (přizpůsobeno, aby εs2 = 10,0 ‰)
- **εs1** = 0,9 ‰ (vypočteno lineárně)
- **εs2** = 10,0 ‰ (fixní)

### Popis
Horní vlákno bez přetvoření, průřez přechází do čistého ohybu.

---

## Přechod 6→7: A₁ dosahuje meze kluzu

### Fixní hodnoty
- **εs2** = 10,0 ‰ (konstantní - zůstává plastická)

### Měnící se hodnoty
- **εtop**: postupně se zvyšuje z 0,0 ‰

### Podmínka ukončení
- **εs1 = 2,2 ‰** (mez kluzu horní výztuže)

### Výpočet
1. εs2 musí zůstat na 10,0 ‰ (fixní)
2. εtop se postupně zvyšuje z 0,0 ‰
3. εs1 se vypočítává lineárně
4. εbottom se přizpůsobuje, aby εs2 = 10,0 ‰
5. Když εs1 dosáhne 2,2 ‰, přechod končí

### Koncový stav (Bod 7)
- **εtop** = 1,4 ‰ (výsledná hodnota, aby εs1 = 2,2 ‰)
- **εbottom** = 10,8 ‰ (přizpůsobeno, aby εs2 = 10,0 ‰)
- **εs1** = 2,2 ‰ (podmínka splněna - mez kluzu)
- **εs2** = 10,0 ‰ (fixní)

### Popis
Horní výztuž (A₁) dosáhla meze kluzu.

---

## Přechod 7→8: Obě výztuže do plastické oblasti

### Fixní hodnoty
- **εs2** = 10,0 ‰ (konstantní)

### Měnící se hodnoty
- **εtop**: postupně se zvyšuje z 1,4 ‰

### Podmínka ukončení
- **εs1 = 10,0 ‰** (plná plastifikace horní výztuže)

### Výpočet
1. εs2 musí zůstat na 10,0 ‰ (fixní)
2. εtop se postupně zvyšuje z 1,4 ‰
3. εs1 se vypočítává lineárně
4. εbottom se přizpůsobuje
5. Když εs1 dosáhne 10,0 ‰, přechod končí

### Koncový stav (Bod 8)
- **εtop** = 10,0 ‰
- **εbottom** = 10,0 ‰
- **εs1** = 10,0 ‰ (plná plastifikace)
- **εs2** = 10,0 ‰ (fixní)

### Popis
Čistý tah, všechna přetvoření jsou stejná (10 ‰).

---

## Shrnutí typů přechodů

### Typ 1: Jednoduchá lineární interpolace
- Fixní: εtop
- Měnící se: εbottom přímo
- Příklad: Přechod 1→2, 2→2b

### Typ 2: Interpolace s podmínkou na výztuž
- Fixní: εtop
- Měnící se: εbottom tak, aby εs2 dosáhlo cílové hodnoty
- Příklad: Přechod 2b→3, 3→4

### Typ 3: Změna TOP s fixní výztuží
- Fixní: εs2
- Měnící se: εtop, εbottom se přizpůsobuje
- Příklad: Přechod 4→5, 5→6

### Typ 4: Změna s podmínkou na druhou výztuž
- Fixní: εs2
- Měnící se: εtop tak, aby εs1 dosáhlo cílové hodnoty
- Příklad: Přechod 6→7, 7→8

---

## Vzorce pro lineární interpolaci

Pro výpočet přetvoření v libovolné pozici `y` (normalizovaná 0-1):

```
ε(y) = εtop + (εbottom - εtop) × y
```

Pozice výztuží:
- A₁ (horní): y₁ = a₁ / h = 0,05 / 0,6 ≈ 0,083
- A₂ (dolní): y₂ = (h - a₂) / h = 0,55 / 0,6 ≈ 0,917

Tedy:
```
εs1 = εtop + (εbottom - εtop) × 0,083
εs2 = εtop + (εbottom - εtop) × 0,917
```

---

## Poznámky pro implementaci

1. **Přírůstky**: Každý přechod by měl být rozdělen na N přírůstků
2. **Kontrola podmínek**: V každém kroku kontrolovat, zda byla splněna ukončovací podmínka
3. **Konzistence**: Vždy zajistit lineární rozdělení přetvoření
4. **Fixní hodnoty**: Pozor na to, které hodnoty jsou fixní a které se mění

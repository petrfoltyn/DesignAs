// API endpoints
const API_URL_WITH_REINFORCEMENT = 'http://localhost:5180/api/InteractionDiagram/calculate';
const API_URL_CONCRETE_ONLY = 'http://localhost:5180/api/InteractionDiagram/calculate-concrete-only';
const API_URL_DESIGN = 'http://localhost:5180/api/InteractionDiagram/design-reinforcement';

let currentData = null;

/**
 * Volání API pro výpočet diagramu
 */
async function calculateDiagram() {
    const btn = document.querySelector('.btn-calculate');
    const loading = document.getElementById('loading');
    const errorDiv = document.getElementById('errorMessage');

    // Skrýt chyby a tabulku
    errorDiv.classList.remove('show');
    document.getElementById('characteristicPointsTable').style.display = 'none';

    // Zobrazit loading
    loading.classList.add('show');
    btn.disabled = true;

    try {
        // Načíst typ diagramu
        const diagramType = document.getElementById('diagramType').value;
        const apiUrl = diagramType === 'concrete-only' ? API_URL_CONCRETE_ONLY : API_URL_WITH_REINFORCEMENT;

        // Načíst hodnoty z formuláře
        const request = {
            b: parseFloat(document.getElementById('b').value),
            h: parseFloat(document.getElementById('h').value),
            layer1Distance: parseFloat(document.getElementById('layer1').value),
            layer2YPos: parseFloat(document.getElementById('layer2').value),
            // Použít výchozí hodnoty pro materiály
            fcd: -20e6,
            epsC2: -0.002,
            epsCu: -0.0035,
            fyd: 435e6,
            es: 200e9,
            epsUd: 0.01,
            nDesign: 0,
            mDesign: 30,
            // Hustota 10 pro všechny intervaly
            densities: [10, 10, 10, 10, 10, 10, 10, 10]
        };

        // Zavolat API
        const response = await fetch(apiUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(request)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Chyba při výpočtu');
        }

        const data = await response.json();
        currentData = data;

        // Vykreslit diagram
        drawDiagram(data);

        // Zobrazit tabulku charakteristických bodů
        displayCharacteristicPoints(data);

    } catch (error) {
        console.error('Error:', error);
        errorDiv.textContent = '❌ ' + error.message;
        errorDiv.classList.add('show');
    } finally {
        loading.classList.remove('show');
        btn.disabled = false;
    }
}

/**
 * Vykreslení interakčního diagramu na canvas
 */
function drawDiagram(data) {
    const canvas = document.getElementById('diagramCanvas');
    const ctx = canvas.getContext('2d');

    // Vymazat canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const points = data.points;
    if (!points || points.length === 0) {
        return;
    }

    // Najít rozsah hodnot - OTOČENÉ OSY: M na X, N na Y
    const nValues = points.map(p => p.n);
    const mValues = points.map(p => p.m);

    const nMin = Math.min(...nValues);
    const nMax = Math.max(...nValues);
    const mMin = Math.min(...mValues);
    const mMax = Math.max(...mValues);

    // Margin
    const margin = 80;
    const plotWidth = canvas.width - 2 * margin;
    const plotHeight = canvas.height - 2 * margin;

    // Škálování - OTOČENÉ: M na horizontální ose, N na vertikální ose
    const nRange = nMax - nMin;
    const mRange = mMax - mMin;
    const nPadding = nRange * 0.1;
    const mPadding = mRange * 0.1;

    const nMinPlot = nMin - nPadding;
    const nMaxPlot = nMax + nPadding;
    const mMinPlot = mMin - mPadding;
    const mMaxPlot = mMax + mPadding;

    const scaleM = plotWidth / (mMaxPlot - mMinPlot);
    const scaleN = plotHeight / (nMaxPlot - nMinPlot);

    // Funkce pro převod souřadnic - OTOČENÉ
    const toCanvasX = (m) => margin + (m - mMinPlot) * scaleM;
    const toCanvasY = (n) => canvas.height - margin - (n - nMinPlot) * scaleN;

    // Nakreslit osy - OTOČENÉ
    ctx.strokeStyle = '#333';
    ctx.lineWidth = 2;

    // Osa N (vertikální) - VLEVO
    ctx.beginPath();
    ctx.moveTo(margin, margin);
    ctx.lineTo(margin, canvas.height - margin);
    ctx.stroke();

    // Osa M (horizontální) - DOLE
    ctx.beginPath();
    ctx.moveTo(margin, canvas.height - margin);
    ctx.lineTo(canvas.width - margin, canvas.height - margin);
    ctx.stroke();

    // Popisky os - OTOČENÉ
    ctx.fillStyle = '#333';
    ctx.font = 'bold 16px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('M [kNm]', canvas.width / 2, canvas.height - 20);

    ctx.save();
    ctx.translate(20, canvas.height / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.fillText('N [kN]', 0, 0);
    ctx.restore();

    // Nakreslit mřížku - OTOČENÉ
    ctx.strokeStyle = '#e0e0e0';
    ctx.lineWidth = 1;
    ctx.setLineDash([5, 5]);

    const gridLinesM = 10;
    const gridLinesN = 10;

    // Svislé čáry pro M (horizontální osa)
    for (let i = 0; i <= gridLinesM; i++) {
        const m = mMinPlot + (i / gridLinesM) * (mMaxPlot - mMinPlot);
        const x = toCanvasX(m);
        ctx.beginPath();
        ctx.moveTo(x, margin);
        ctx.lineTo(x, canvas.height - margin);
        ctx.stroke();

        // Popisek
        ctx.fillStyle = '#666';
        ctx.font = '12px Arial';
        ctx.textAlign = 'center';
        ctx.fillText(m.toFixed(0), x, canvas.height - margin + 20);
    }

    // Vodorovné čáry pro N (vertikální osa)
    for (let i = 0; i <= gridLinesN; i++) {
        const n = nMinPlot + (i / gridLinesN) * (nMaxPlot - nMinPlot);
        const y = toCanvasY(n);
        ctx.beginPath();
        ctx.moveTo(margin, y);
        ctx.lineTo(canvas.width - margin, y);
        ctx.stroke();

        // Popisek
        ctx.fillStyle = '#666';
        ctx.font = '12px Arial';
        ctx.textAlign = 'right';
        ctx.fillText(n.toFixed(0), margin - 10, y + 4);
    }

    ctx.setLineDash([]);

    // Nakreslit křivku diagramu
    const diagramTypeForColor = document.getElementById('diagramType').value;
    ctx.strokeStyle = diagramTypeForColor === 'concrete-only' ? '#ff6b6b' : '#667eea';
    ctx.lineWidth = 3;
    ctx.beginPath();

    for (let i = 0; i < points.length; i++) {
        const x = toCanvasX(points[i].m);  // M na X
        const y = toCanvasY(points[i].n);  // N na Y

        if (i === 0) {
            ctx.moveTo(x, y);
        } else {
            ctx.lineTo(x, y);
        }
    }

    ctx.stroke();

    // Nakreslit body
    const pointColor = diagramTypeForColor === 'concrete-only' ? '#c92a2a' : '#764ba2';
    ctx.fillStyle = pointColor;
    for (let i = 0; i < points.length; i++) {
        const point = points[i];
        if (point.name && point.name.startsWith('Bod ')) {
            const x = toCanvasX(point.m);  // M na X
            const y = toCanvasY(point.n);  // N na Y

            ctx.beginPath();
            ctx.arc(x, y, 5, 0, 2 * Math.PI);
            ctx.fill();

            // Popisek charakteristického bodu
            ctx.fillStyle = pointColor;
            ctx.font = 'bold 11px Arial';
            ctx.textAlign = 'left';
            ctx.fillText(point.name, x + 10, y - 10);
        }
    }

    // Titulek
    const diagramType = document.getElementById('diagramType').value;
    const titleText = diagramType === 'concrete-only'
        ? 'Interakční diagram N-M (pouze beton)'
        : 'Interakční diagram N-M (s výztuží)';

    ctx.fillStyle = '#333';
    ctx.font = 'bold 20px Arial';
    ctx.textAlign = 'center';
    ctx.fillText(titleText, canvas.width / 2, 30);

    // Info o geometrii
    ctx.font = '12px Arial';
    ctx.textAlign = 'left';
    ctx.fillStyle = '#666';
    const geom = data.geometry;
    ctx.fillText(`b = ${(geom.b * 1000).toFixed(0)} mm, h = ${(geom.h * 1000).toFixed(0)} mm`, margin, 60);
}

/**
 * Výpočet napětí oceli z přetvoření
 */
function calculateSteelStress(eps, fyd = 435, Es = 200000) {
    const epsYield = fyd / Es;
    if (eps >= epsYield) return fyd;
    if (eps <= -epsYield) return -fyd;
    return eps * Es;
}

/**
 * Výpočet napětí betonu
 */
function calculateConcreteStress(eps, fcd = -20, epsC2 = -0.002) {
    if (eps >= 0) return 0; // Tah - beton nepracuje
    if (eps > epsC2) {
        // Parabolická sekce
        const epsNorm = eps / epsC2;
        return fcd * (1 - (1 - epsNorm) * (1 - epsNorm));
    }
    // Konstantní sekce
    return fcd;
}

/**
 * Zobrazení tabulky charakteristických bodů
 */
function displayCharacteristicPoints(data) {
    const table = document.getElementById('characteristicPointsTable');
    const tbody = document.getElementById('characteristicPointsBody');
    const points = data.points;

    if (!points || points.length === 0) {
        return;
    }

    // Najít pouze charakteristické body (Bod 1-8)
    const characteristicPoints = points.filter(p => p.name && p.name.startsWith('Bod '));

    // Vymazat staré řádky
    tbody.innerHTML = '';

    // Geometrie pro výpočet přetvoření z k a q
    const h = data.geometry.h;
    const y1 = data.geometry.y1;
    const y2 = data.geometry.y2;

    // Přidat řádky pro charakteristické body
    characteristicPoints.forEach(point => {
        const row = document.createElement('tr');
        row.style.borderBottom = '1px solid #e9ecef';

        // Určit k a q podle typu dat
        let k = 'N/A';
        let q = 'N/A';

        if (point.k !== undefined && point.q !== undefined) {
            k = point.k.toFixed(6);
            q = point.q.toFixed(6);
        }

        // Přetvoření [‰] - API vrací hodnoty v promile
        // Pokud nejsou dostupné, vypočítat z k a q
        let epsTop, epsBottom, eps1, eps2;

        if (point.epsTop !== undefined) {
            // Režim "s výztuží" - přetvoření jsou dostupná
            epsTop = point.epsTop.toFixed(2);
            epsBottom = point.epsBottom.toFixed(2);
            eps1 = point.epsS1 !== undefined ? point.epsS1.toFixed(2) : 'N/A';
            eps2 = point.epsS2 !== undefined ? point.epsS2.toFixed(2) : 'N/A';
        } else if (point.k !== undefined && point.q !== undefined) {
            // Režim "pouze beton" - vypočítat přetvoření z k a q
            // ε(y) = k·y + q
            const epsTopCalc = (point.k * (h / 2) + point.q) * 1000; // převést na promile
            const epsBottomCalc = (point.k * (-h / 2) + point.q) * 1000;
            const eps1Calc = (point.k * y1 + point.q) * 1000;
            const eps2Calc = (point.k * y2 + point.q) * 1000;

            epsTop = epsTopCalc.toFixed(2);
            epsBottom = epsBottomCalc.toFixed(2);
            eps1 = eps1Calc.toFixed(2);
            eps2 = eps2Calc.toFixed(2);
        } else {
            epsTop = 'N/A';
            epsBottom = 'N/A';
            eps1 = 'N/A';
            eps2 = 'N/A';
        }

        // Napětí - výpočet z přetvoření
        let sigmaTop = 'N/A';
        let sigmaBottom = 'N/A';
        let sigma1 = 'N/A';
        let sigma2 = 'N/A';

        if (epsTop !== 'N/A') {
            sigmaTop = calculateConcreteStress(parseFloat(epsTop) / 1000).toFixed(2);
        }
        if (epsBottom !== 'N/A') {
            sigmaBottom = calculateConcreteStress(parseFloat(epsBottom) / 1000).toFixed(2);
        }
        if (eps1 !== 'N/A') {
            sigma1 = calculateSteelStress(parseFloat(eps1) / 1000).toFixed(2);
        }
        if (eps2 !== 'N/A') {
            sigma2 = calculateSteelStress(parseFloat(eps2) / 1000).toFixed(2);
        }

        // Plochy výztuže
        const as1 = point.as1 !== undefined ? point.as1.toFixed(2) : 'N/A';
        const as2 = point.as2 !== undefined ? point.as2.toFixed(2) : 'N/A';

        row.innerHTML = `
            <td style="padding: 8px; font-weight: 600;">${point.name}</td>
            <td style="padding: 8px; text-align: right;">${point.n.toFixed(2)}</td>
            <td style="padding: 8px; text-align: right;">${point.m.toFixed(2)}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${k}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${q}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${epsTop}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${sigmaTop}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${epsBottom}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${sigmaBottom}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${as1}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${eps1}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${sigma1}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${as2}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${eps2}</td>
            <td style="padding: 8px; text-align: right; font-family: monospace; font-size: 0.9em;">${sigma2}</td>
        `;
        tbody.appendChild(row);
    });

    // Zobrazit tabulku
    table.style.display = 'block';
}

/**
 * Návrh výztuže pro zadané zatížení
 */
async function designReinforcement() {
    const errorDiv = document.getElementById('errorMessage');
    const resultsDiv = document.getElementById('designResults');

    // Skrýt chyby a předchozí výsledky
    errorDiv.classList.remove('show');
    resultsDiv.style.display = 'none';

    try {
        // Načíst hodnoty z formuláře
        const request = {
            b: parseFloat(document.getElementById('b').value),
            h: parseFloat(document.getElementById('h').value),
            layer1Distance: parseFloat(document.getElementById('layer1').value),
            layer2YPos: parseFloat(document.getElementById('layer2').value),
            nDesign: parseFloat(document.getElementById('nDesign').value),
            mDesign: parseFloat(document.getElementById('mDesign').value),
            // Použít výchozí hodnoty pro materiály
            fcd: -20e6,
            epsC2: -0.002,
            epsCu: -0.0035,
            fyd: 435e6,
            es: 200e9,
            epsUd: 0.01
        };

        // Zavolat API
        const response = await fetch(API_URL_DESIGN, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(request)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Chyba při výpočtu');
        }

        const result = await response.json();

        // Zobrazit výsledky
        document.getElementById('asResult').textContent = `As2 = ${result.as2.toFixed(2)} cm²`;
        document.getElementById('epsTop').textContent = result.epsTop.toFixed(2);
        document.getElementById('epsBot').textContent = result.epsBottom.toFixed(2);
        document.getElementById('epsAs').textContent = result.epsS2.toFixed(2);
        document.getElementById('sigAs').textContent = result.sigAs2.toFixed(1);
        document.getElementById('mCalc').textContent = result.m.toFixed(2);

        // Zobrazit chybu v procentech
        const errorPercent = (result.errorRel * 100).toFixed(2);
        const errorSpan = document.getElementById('errorPercent');
        errorSpan.textContent = `Chyba: ${errorPercent}%`;

        // Změnit barvu podle velikosti chyby
        if (result.errorRel < 0.01) {
            errorSpan.style.background = '#d4edda';
            errorSpan.style.color = '#155724';
        } else if (result.errorRel < 0.05) {
            errorSpan.style.background = '#fff3cd';
            errorSpan.style.color = '#856404';
        } else {
            errorSpan.style.background = '#f8d7da';
            errorSpan.style.color = '#721c24';
        }

        // Zobrazit panel s výsledky
        resultsDiv.style.display = 'block';

    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.classList.add('show');
    }
}

// Při načtení stránky vygenerovat diagram s výchozími hodnotami
window.addEventListener('load', () => {
    calculateDiagram();

    // Přepočítat diagram při změně typu
    document.getElementById('diagramType').addEventListener('change', () => {
        calculateDiagram();
    });
});

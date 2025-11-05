// ========================================================================
// CALCULATION FUNCTIONS - Core Mathematical Operations
// ========================================================================

// Constants for concrete calculations
const EC2 = -0.002;      // εc2 = -2‰ (transition parabola → constant)
const INV_EC2 = -500.0;  // 1/εc2

/**
 * Fast calculation of concrete internal forces (N and M)
 * Using parabolic-rectangular stress diagram (EC2)
 * @param {number} b - width [m]
 * @param {number} h - height [m]
 * @param {number} k - strain slope [1/m]
 * @param {number} q - strain at centroid [-]
 * @param {number} fcd - concrete design compressive strength [Pa]
 * @returns {object} {N: normal force [N], M: moment [Nm]}
 */
function fastConcreteNM(b, h, k, q, fcd) {
    const h_2 = 0.5 * h;
    const x1 = -h_2;  // BOTTOM
    const x2 = h_2;   // TOP

    let x_0, x_ec2;

    const tolerance = 1e-12;
    const isNonZero = (val) => Math.abs(val) >= tolerance;
    const isZero = (val) => Math.abs(val) < tolerance;
    const isLess = (a, b) => a < b - tolerance;

    if (isNonZero(k)) {
        x_0 = -q / k;
        x_ec2 = (EC2 - q) / k;
    } else {
        x_0 = isZero(q) ? 0 : (q > 0 ? -Infinity : Infinity);
        x_ec2 = Infinity;
    }

    let N = 0.0;
    let M = 0.0;

    if (isZero(k)) {
        // Constant deformation
        if (q >= 0) {
            // Tension or zero
            return {N: 0, M: 0};
        } else if (q > EC2) {
            // Parabolic section
            // σ = fcd * [1 - (1 - ε/εc2)²]
            const epsilon_norm = q / EC2;
            const sigma = fcd * (1 - (1 - epsilon_norm) * (1 - epsilon_norm));
            N = sigma * b * h;
            M = 0;
        } else {
            // Constant section (ε ≤ εc2, continues to infinity)
            N = fcd * b * h;
            M = 0;
        }
        return {N: N, M: M};
    }

    // SEGMENT 2: Parabolic compression section
    const xa_para = Math.max(x1, Math.min(x_ec2, x_0));
    const xb_para = Math.min(x2, Math.max(x_ec2, x_0));

    if (isLess(xa_para, xb_para)) {
        const a = k * INV_EC2;
        const c = q * INV_EC2;

        const dx = xb_para - xa_para;
        const dx2 = xb_para * xb_para - xa_para * xa_para;
        const dx3 = (xb_para * xb_para * xb_para - xa_para * xa_para * xa_para) / 3.0;

        const N_para = fcd * b * (
            (2 * a - 2 * a * c) * dx2 * 0.5 +
            (2 * c - c * c) * dx -
            a * a * dx3
        );

        const xa2 = xa_para * xa_para;
        const xb2 = xb_para * xb_para;
        const xa4 = xa2 * xa2;
        const xb4 = xb2 * xb2;
        const dx4 = 0.25 * (xb4 - xa4);

        const M_para = fcd * b * (
            (2 * a - 2 * a * c) * dx3 +
            (2 * c - c * c) * dx2 * 0.5 -
            a * a * dx4
        );

        N += N_para;
        M += M_para;
    }

    // SEGMENT 3: Constant compression section (ε ≤ εc2) - continues to infinity
    let xa_const, xb_const;

    if (Math.abs(k) < 1e-10) {
        // Already handled above for k=0
        xa_const = xb_const = 0;
    } else if (k > 0) {
        // ε grows with x, so greater compression (ε ≤ εc2) is for x ≤ x_ec2
        xa_const = x1;  // from bottom edge
        xb_const = Math.min(x_ec2, x2);  // to x_ec2 or top edge
    } else {
        // ε decreases with x, so greater compression (ε ≤ εc2) is for x ≥ x_ec2
        xa_const = Math.max(x_ec2, x1);  // from x_ec2 or bottom edge
        xb_const = x2;  // to top edge
    }

    if (isLess(xa_const, xb_const)) {
        const dx = xb_const - xa_const;
        const centroid = 0.5 * (xa_const + xb_const);
        const N_const = fcd * b * dx;
        const M_const = N_const * centroid;

        N += N_const;
        M += M_const;
    }

    return {N: N, M: M};
}

/**
 * Calculate strain parameters k and q from two selected points
 * Strain equation: ε(y) = k*y + q
 * @returns {object} {k, q, point1, point2, eps1, eps2, y1, y2} or null if invalid
 */
function calculateKQ() {
    console.log('calculateKQ called');
    const topRadio = document.querySelector('input[name="top-point"]:checked');
    const bottomRadio = document.querySelector('input[name="bottom-point"]:checked');
    console.log('topRadio:', topRadio);
    console.log('bottomRadio:', bottomRadio);

    if (!topRadio || !bottomRadio) {
        console.log('Missing radio selection');
        alert('Musíte vybrat 1 horní bod a 1 dolní bod!');
        return null;
    }

    const point1 = topRadio.getAttribute('data-point');
    const point2 = bottomRadio.getAttribute('data-point');
    console.log('point1:', point1, 'point2:', point2);

    const eps1 = parseFloat(document.getElementById(`strain_${point1}`).value) / 1000; // convert ‰ to [-]
    const eps2 = parseFloat(document.getElementById(`strain_${point2}`).value) / 1000;
    console.log('eps1:', eps1, 'eps2:', eps2);

    const y1 = getPointY(point1);
    const y2 = getPointY(point2);
    console.log('y1:', y1, 'y2:', y2);

    // Calculate slope k and strain at centroid q
    // ε(y) = k*y + q
    // eps1 = k*y1 + q
    // eps2 = k*y2 + q
    // => k = (eps1 - eps2) / (y1 - y2)
    // => q = eps1 - k*y1

    if (Math.abs(y1 - y2) < 1e-10) {
        alert('Body musí mít různé Y souřadnice!');
        return null;
    }

    const k = (eps1 - eps2) / (y1 - y2);
    const q = eps1 - k * y1;
    console.log('Calculated k:', k, 'q:', q);

    return {
        k: k,
        q: q,
        point1: point1,
        point2: point2,
        eps1: eps1,
        eps2: eps2,
        y1: y1,
        y2: y2
    };
}

/**
 * Main reinforcement design calculation
 * Solves system of equations for As1 and As2
 * Equilibrium: Fc + Fs1 + Fs2 = N
 * Moment equilibrium: Fc*(-yc) + Fs1*(-y1) + Fs2*(-y2) = M
 */
function calculateReinforcement() {
    // 1. Get strain data from analysis dialog
    const kqData = calculateKQ();

    if (!kqData) {
        console.log('kqData is null, showing alert');
        alert('Nejprve nastavte přetvoření v záložce "Analýza přetvoření a napětí průřezu"!');
        return;
    }
    console.log('kqData is valid, continuing...');

    // 2. Get geometry and materials
    const N = parseFloat(document.getElementById('load_N').value) || 0; // kN
    const M = parseFloat(document.getElementById('load_M').value) || 0; // kNm
    const b = parseFloat(document.getElementById('b').value) || 0.3; // m
    const h = parseFloat(document.getElementById('h').value) || 0.5; // m
    const fcd = parseFloat(document.getElementById('fcd').value) || -20000000; // Pa
    const fyd = parseFloat(document.getElementById('fyd').value) || 435000000; // Pa
    const Es = parseFloat(document.getElementById('Es').value) || 200000000000; // Pa
    const layer1_distance = parseFloat(document.getElementById('layer1_yPos').value) || 0.05; // m
    const y1 = h - layer1_distance; // absolute coordinate
    const y2 = parseFloat(document.getElementById('layer2_yPos').value) || 0.05; // m

    // 3. Calculate stresses in reinforcement from strain
    const h_2 = h / 2;
    const y1_local = y1 - (h - h_2); // convert to local system
    const y2_local = y2 - (h - h_2);

    const eps_s1 = kqData.k * y1_local + kqData.q;
    const eps_s2 = kqData.k * y2_local + kqData.q;

    // Bilinear steel diagram
    const sigma1_elastic = eps_s1 * Es;
    const sigma1 = Math.max(Math.min(sigma1_elastic, fyd), -fyd);

    const sigma2_elastic = eps_s2 * Es;
    const sigma2 = Math.max(Math.min(sigma2_elastic, fyd), -fyd);

    // 4. Calculate internal forces from concrete
    const concreteForces = fastConcreteNM(b, h, kqData.k, kqData.q, fcd);
    const Fc = concreteForces.N; // N
    const Mc = concreteForces.M; // Nm

    // Calculate centroid of compression zone
    const yc = (Math.abs(Fc) > 1e-6) ? Mc / Fc : 0; // m

    // 5. SOLVE SYSTEM OF EQUATIONS FOR As1 and As2
    // Force equilibrium:     As1·σ1 + As2·σ2 + Fc = N
    // Moment equilibrium: As1·σ1·(-y1) + As2·σ2·(-y2) + Fc·(-yc) = M

    const N_Pa = N * 1000; // kN -> N
    const M_Pa = M * 1000; // kNm -> Nm

    // Right hand side (without unknowns)
    const RHS_N = N_Pa - Fc;        // N
    const RHS_M = -M_Pa - Mc;       // Nm

    // System determinant: D = σ1·σ2·(y2 - y1)
    const det = sigma1 * sigma2 * (y2_local - y1_local);

    let As1, As2, Fs1, Fs2, Ms1, Ms2;

    if (Math.abs(det) < 1e-6) {
        // Singular system - reinforcement has same stress and position
        alert('Nelze vypočítat výztuž - singulární soustava rovnic!\n' +
              'Zkuste změnit přetvoření nebo polohu výztuže.');
        console.error('Singular system: det =', det);
        return;
    }

    // Cramer's rule
    As1 = (RHS_N * y2_local - RHS_M) / (sigma1 * (y2_local - y1_local));
    As2 = (RHS_M - y1_local * RHS_N) / (sigma2 * (y2_local - y1_local));

    // Forces in reinforcement
    Fs1 = As1 * sigma1; // N
    Fs2 = As2 * sigma2; // N

    Ms1 = Fs1 * (-y1_local); // Nm
    Ms2 = Fs2 * (-y2_local); // Nm

    // Check solution
    const checkN = Fc + Fs1 + Fs2;
    const checkM = -Mc + Ms1 + Ms2;
    const errorN = Math.abs(checkN - N_Pa);
    const errorM = Math.abs(checkM - M_Pa);

    console.log('=== SOLUTION ===');
    console.log('Determinant:', det);
    console.log('As1 =', As1, 'm² =', (As1*10000).toFixed(2), 'cm²');
    console.log('As2 =', As2, 'm² =', (As2*10000).toFixed(2), 'cm²');
    console.log('Check N:', checkN/1000, 'kN vs', N, 'kN, error:', (errorN/1000).toFixed(6), 'kN');
    console.log('Check M:', checkM/1000, 'kNm vs', M, 'kNm, error:', (errorM/1000).toFixed(6), 'kNm');

    // 6. Update equilibrium equations with calculated values
    document.getElementById('eq_force').innerHTML =
        `Fc + As1·σ1 + As2·σ2 = N<br>` +
        `<span style="color: #e74c3c;">${(Fc/1000).toFixed(2)}</span> + ` +
        `<span style="color: #e74c3c;">${(As1*10000).toFixed(2)}</span>·<span style="color: #e74c3c;">${(sigma1/1e6).toFixed(1)}</span> + ` +
        `<span style="color: #3498db;">${(As2*10000).toFixed(2)}</span>·<span style="color: #3498db;">${(sigma2/1e6).toFixed(1)}</span> = ` +
        `<span style="color: #667eea; font-weight: bold;">${N.toFixed(1)}</span> kN ` +
        `<span style="color: #27ae60; font-weight: bold;">✓</span>`;

    document.getElementById('eq_moment').innerHTML =
        `Fc·(-yc) + As1·σ1·(-y1) + As2·σ2·(-y2) = M<br>` +
        `<span style="color: #e74c3c;">${(Fc/1000).toFixed(2)}</span>·<span style="color: #e74c3c;">${(-yc*1000).toFixed(1)}</span> + ` +
        `<span style="color: #e74c3c;">${(As1*10000).toFixed(2)}</span>·<span style="color: #e74c3c;">${(sigma1/1e6).toFixed(1)}</span>·<span style="color: #e74c3c;">${(-y1_local*1000).toFixed(1)}</span> + ` +
        `<span style="color: #3498db;">${(As2*10000).toFixed(2)}</span>·<span style="color: #3498db;">${(sigma2/1e6).toFixed(1)}</span>·<span style="color: #3498db;">${(-y2_local*1000).toFixed(1)}</span> = ` +
        `<span style="color: #667eea; font-weight: bold;">${M.toFixed(1)}</span> kNm ` +
        `<span style="color: #27ae60; font-weight: bold;">✓</span>`;

    // 7. Display detailed results
    const resultsDiv = document.getElementById('designResults');
    const resultsContent = document.getElementById('designResultsContent');

    resultsContent.innerHTML = `
        <div class="info-box" style="background: #e7f3ff;">
            <strong>📊 Přetvoření z analýzy:</strong><br>
            • k (sklon přetvoření) = ${kqData.k.toFixed(6)} [1/m]<br>
            • q (přetvoření v těžišti) = ${(kqData.q*1000).toFixed(3)} ‰<br>
            • Vybrané body: ${getPointName(kqData.point1)} + ${getPointName(kqData.point2)}
        </div>

        <div class="result-item" style="border-left-color: #2c3e50;">
            <strong>🏗️ Přetvoření ve výztuži:</strong><br>
            • ε<sub>s1</sub> (horní) = ${(eps_s1*1000).toFixed(3)} ‰<br>
            • ε<sub>s2</sub> (dolní) = ${(eps_s2*1000).toFixed(3)} ‰
        </div>

        <div class="result-item" style="border-left-color: #e74c3c;">
            <strong>🔴 Napětí ve výztuži:</strong><br>
            • σ<sub>1</sub> (horní) = ${(sigma1/1e6).toFixed(1)} MPa<br>
            • σ<sub>2</sub> (dolní) = ${(sigma2/1e6).toFixed(1)} MPa
        </div>

        <div class="result-item" style="border-left-color: #95a5a6;">
            <strong>🧱 Vnitřní síly z betonu:</strong><br>
            • F<sub>c</sub> = ${(Fc/1000).toFixed(2)} kN<br>
            • M<sub>c</sub> = ${(-Mc/1000).toFixed(2)} kNm<br>
            • y<sub>c</sub> = ${(-yc*1000).toFixed(1)} mm
        </div>

        <div class="result-item" style="border-left-color: #27ae60; background: #d4edda;">
            <strong>✅ VYPOČTENÁ VÝZTUŽ:</strong><br>
            • <strong style="color: #e74c3c;">As<sub>1</sub></strong> (horní) = <strong>${(As1*10000).toFixed(2)} cm²</strong> = ${(As1*1e6).toFixed(0)} mm²<br>
            • <strong style="color: #3498db;">As<sub>2</sub></strong> (dolní) = <strong>${(As2*10000).toFixed(2)} cm²</strong> = ${(As2*1e6).toFixed(0)} mm²<br>
            <span style="font-size: 12px; color: #666;">
            (Fs1 = ${(Fs1/1000).toFixed(2)} kN, Fs2 = ${(Fs2/1000).toFixed(2)} kN)
            </span>
        </div>

        <div class="result-item" style="border-left-color: #27ae60; background: #f0fff4;">
            <strong>⚖️ Kontrola rovnováhy:</strong><br>
            • <strong>Síly:</strong> ${(Fc/1000).toFixed(2)} + ${(Fs1/1000).toFixed(2)} + ${(Fs2/1000).toFixed(2)} = ${((Fc+Fs1+Fs2)/1000).toFixed(3)} kN<br>
            <span style="margin-left: 20px;">Požadováno: ${N.toFixed(3)} kN | Chyba: <strong style="color: ${errorN < 0.001 ? '#27ae60' : '#e74c3c'}">${(errorN/1000).toFixed(6)} kN</strong></span><br>
            • <strong>Momenty:</strong> ${(-Mc/1000).toFixed(2)} + ${(Ms1/1000).toFixed(2)} + ${(Ms2/1000).toFixed(2)} = ${((-Mc+Ms1+Ms2)/1000).toFixed(3)} kNm<br>
            <span style="margin-left: 20px;">Požadováno: ${M.toFixed(3)} kNm | Chyba: <strong style="color: ${errorM < 0.001 ? '#27ae60' : '#e74c3c'}">${(errorM/1000).toFixed(6)} kNm</strong></span>
        </div>
    `;

    resultsDiv.style.display = 'block';

    // Draw combined diagram
    try {
        drawDesignCombinedDiagram(kqData, h, b, fcd, sigma1, sigma2, y1_local, y2_local, Fc, Fs1, Fs2, yc, Mc, Ms1, Ms2);
    } catch (error) {
        console.error('Error in drawDesignCombinedDiagram:', error);
        alert('Chyba při vykreslování diagramu: ' + error.message);
    }
}

/**
 * Helper function - Get Y coordinate of selected point
 */
function getPointY(pointName) {
    const h = parseFloat(document.getElementById('h').value);
    const layer1_distance = parseFloat(document.getElementById('layer1_yPos').value);
    const y1 = h - layer1_distance;
    const y2 = parseFloat(document.getElementById('layer2_yPos').value);
    const h_2 = h / 2;

    // Convert to local coordinate system (center of cross-section = 0)
    switch(pointName) {
        case 'top': return h_2;
        case 's1': return y1 - (h - h_2);
        case 's2': return y2 - (h - h_2);
        case 'bottom': return -h_2;
        default: return 0;
    }
}

/**
 * Helper function - Get Czech name for point
 */
function getPointName(point) {
    const names = {
        'top': 'Horní okraj',
        's1': 'Horní výztuž',
        's2': 'Dolní výztuž',
        'bottom': 'Dolní okraj'
    };
    return names[point] || point;
}

/**
 * Calculate results for "Zadání" tab
 * Display geometry and material properties
 */
function calculateZadani() {
    // Get values
    const b = parseFloat(document.getElementById('b').value) || 0.3;
    const h = parseFloat(document.getElementById('h').value) || 0.5;
    const fcd = parseFloat(document.getElementById('fcd').value) || -20000000;
    const fyd = parseFloat(document.getElementById('fyd').value) || 435000000;
    const Es = parseFloat(document.getElementById('Es').value) || 200000000000;
    const layer1_distance = parseFloat(document.getElementById('layer1_yPos').value) || 0.05;
    const y1 = h - layer1_distance;
    const y2 = parseFloat(document.getElementById('layer2_yPos').value) || 0.05;

    // Display basic information
    const resultsDiv = document.getElementById('results');
    const resultsContent = document.getElementById('resultsContent');

    resultsContent.innerHTML = `
        <div class="result-item">
            <strong>📐 Geometrie:</strong><br>
            • Šířka: b = ${(b*1000).toFixed(0)} mm<br>
            • Výška: h = ${(h*1000).toFixed(0)} mm
        </div>

        <div class="result-item">
            <strong>🧱 Vlastnosti betonu:</strong><br>
            • f<sub>cd</sub> = ${(Math.abs(fcd)/1e6).toFixed(1)} MPa
        </div>

        <div class="result-item">
            <strong>🔩 Vlastnosti výztuže:</strong><br>
            • f<sub>yd</sub> = ${(fyd/1e6).toFixed(0)} MPa<br>
            • E<sub>s</sub> = ${(Es/1e9).toFixed(0)} GPa
        </div>

        <div class="result-item">
            <strong>📍 Poloha výztuže:</strong><br>
            • Horní: y<sub>1</sub> = ${(y1*1000).toFixed(0)} mm<br>
            • Dolní: y<sub>2</sub> = ${(y2*1000).toFixed(0)} mm<br>
            • Vzdálenost: ${((y1-y2)*1000).toFixed(0)} mm
        </div>

        <div class="info-box" style="background: #e7f3ff; margin-top: 15px;">
            ℹ️ Pro analýzu přetvoření přejděte na záložku "Analýza přetvoření a napětí průřezu".<br>
            Pro návrh výztuže přejděte na záložku "Návrh výztuže".
        </div>
    `;

    resultsDiv.classList.add('show');
    resultsDiv.style.display = 'block';
}

/**
 * Update equilibrium equations display with current load values
 */
function updateEquilibrium() {
    // Get load values
    const N = parseFloat(document.getElementById('load_N').value) || 0;
    const M = parseFloat(document.getElementById('load_M').value) || 0;

    // Update equation display with numerical values
    document.getElementById('eq_force').innerHTML =
        `Fc + As1·σ1 + As2·σ2 = <span style="color: #667eea; font-weight: bold;">${N.toFixed(1)} kN</span>`;
    document.getElementById('eq_moment').innerHTML =
        `Fc·yc + As1·σ1·y1 + As2·σ2·y2 = <span style="color: #667eea; font-weight: bold;">${M.toFixed(1)} kNm</span>`;
}

/**
 * Calculate strain at position y given εtop and εbottom
 * @param {number} epsTop - strain at top [‰]
 * @param {number} epsBottom - strain at bottom [‰]
 * @param {number} y_normalized - normalized position from bottom (0) to top (1)
 * @returns {number} strain at position y [‰]
 */
function calculateStrainAtY(epsTop, epsBottom, y_normalized) {
    return epsBottom + (epsTop - epsBottom) * y_normalized;
}

/**
 * Calculate interaction diagram characteristic points (Bod 1-8)
 * Based on detailed description in body_prechody_detail.md
 * @returns {Array} Array of point objects with strains, forces, and reinforcement
 */
function calculateInteractionDiagram() {
    console.log('=== CALCULATING INTERACTION DIAGRAM ===');

    // Get geometry and material properties
    const b = parseFloat(document.getElementById('b').value) || 0.3; // m
    const h = parseFloat(document.getElementById('h').value) || 0.5; // m
    const fcd = parseFloat(document.getElementById('fcd').value) || -20000000; // Pa
    const fyd = parseFloat(document.getElementById('fyd').value) || 435000000; // Pa
    const Es = parseFloat(document.getElementById('Es').value) || 200000000000; // Pa
    const epsC2 = parseFloat(document.getElementById('epsC2').value) || -0.002; // [-]
    const epsCu = parseFloat(document.getElementById('epsCu').value) || -0.0035; // [-]
    const epsUd = parseFloat(document.getElementById('epsUd').value) || 0.01; // [-]

    // Reinforcement layer positions
    const layer1_distance = parseFloat(document.getElementById('layer1_yPos').value) || 0.05; // m
    const y1_abs = h - layer1_distance; // absolute coordinate from bottom
    const y2_abs = parseFloat(document.getElementById('layer2_yPos').value) || 0.05; // m

    // Normalized positions (0 = bottom, 1 = top)
    const y1_norm = y1_abs / h; // horní výztuž
    const y2_norm = y2_abs / h; // dolní výztuž

    console.log('Geometry: b =', b, 'm, h =', h, 'm');
    console.log('Reinforcement positions: y1_norm =', y1_norm, ', y2_norm =', y2_norm);
    console.log('Material: fcd =', fcd/1e6, 'MPa, fyd =', fyd/1e6, 'MPa');
    console.log('Strains: εcu =', epsCu*1000, '‰, εc2 =', epsC2*1000, '‰, εud =', epsUd*1000, '‰');

    // Yield strain
    const epsYd = fyd / Es; // [-]
    console.log('εyd =', epsYd*1000, '‰');

    // Get design loads from interaction tab inputs
    const N_design = parseFloat(document.getElementById('interaction_N').value) || 0; // kN
    const M_design = parseFloat(document.getElementById('interaction_M').value) || 0; // kNm

    console.log('Design loads: N =', N_design, 'kN, M =', M_design, 'kNm');

    const points = [];

    // Helper function to calculate point data
    function calculatePoint(name, epsTop, epsBottom) {
        // Convert to ‰ for display
        const epsTop_pm = epsTop * 1000;
        const epsBottom_pm = epsBottom * 1000;

        // Calculate strains at reinforcement layers
        const epsS1 = calculateStrainAtY(epsTop, epsBottom, y1_norm);
        const epsS2 = calculateStrainAtY(epsTop, epsBottom, y2_norm);
        const epsS1_pm = epsS1 * 1000;
        const epsS2_pm = epsS2 * 1000;

        // Calculate k and q for strain distribution
        const h_2 = h / 2;
        const y_top_local = h_2;
        const y_bottom_local = -h_2;

        const k = (epsTop - epsBottom) / (y_top_local - y_bottom_local);
        const q = epsTop - k * y_top_local;

        // Calculate concrete forces
        const concreteForces = fastConcreteNM(b, h, k, q, fcd);
        const Fc = concreteForces.N / 1000; // kN
        const Mc = concreteForces.M / 1000; // kNm

        // Calculate steel stresses (bilinear)
        const sigma1_elastic = epsS1 * Es;
        const sigma1 = Math.max(Math.min(sigma1_elastic, fyd), -fyd);
        const sigma2_elastic = epsS2 * Es;
        const sigma2 = Math.max(Math.min(sigma2_elastic, fyd), -fyd);

        // Local coordinates for reinforcement
        const y1_local = y1_abs - (h - h_2);
        const y2_local = y2_abs - (h - h_2);

        // Calculate As1 and As2 for the design loads N_design and M_design
        // Using Cramer's rule to solve the system of equations
        const N_Pa = N_design * 1000; // kN -> N
        const M_Pa = M_design * 1000; // kNm -> Nm

        // Right hand side (without unknowns)
        const RHS_N = N_Pa - concreteForces.N;        // N
        const RHS_M = -M_Pa - concreteForces.M;       // Nm

        // System determinant: D = σ1·σ2·(y2 - y1)
        const det = sigma1 * sigma2 * (y2_local - y1_local);

        let As1, As2, Fs1, Fs2;

        if (Math.abs(det) < 1e-6) {
            // Singular system - cannot calculate reinforcement
            As1 = NaN;
            As2 = NaN;
            Fs1 = NaN;
            Fs2 = NaN;
        } else {
            // Cramer's rule
            As1 = (RHS_N * y2_local - RHS_M) / (sigma1 * (y2_local - y1_local));
            As2 = (RHS_M - y1_local * RHS_N) / (sigma2 * (y2_local - y1_local));

            // Calculate forces in steel
            Fs1 = As1 * sigma1 / 1000; // kN
            Fs2 = As2 * sigma2 / 1000; // kN
        }

        // Total forces (for verification)
        const N_total = Fc + (isNaN(Fs1) ? 0 : Fs1) + (isNaN(Fs2) ? 0 : Fs2); // kN
        const M_total = -Mc + (isNaN(Fs1) ? 0 : Fs1 * (-y1_local)) + (isNaN(Fs2) ? 0 : Fs2 * (-y2_local)); // kNm

        // Calculate As and Md assuming As1 = 0
        // Force equilibrium: As1·σ1 + As2·σ2 + Fc = N
        // With As1 = 0: As2·σ2 = N - Fc
        // => As = As2 = (N - Fc) / σ2
        let As_simple, Md_simple;

        if (Math.abs(sigma2) > 1e-6) {
            // Calculate As2 from force equilibrium with As1 = 0
            As_simple = (N_Pa - concreteForces.N) / sigma2; // m²

            // Calculate moment with As1 = 0 and As2 = As_simple
            // Moment equilibrium: Fc·(-yc) + As2·σ2·(-y2) = Md
            const Fs2_simple = As_simple * sigma2; // N
            const Ms2_simple = Fs2_simple * (-y2_local); // Nm
            Md_simple = -concreteForces.M / 1000 + Ms2_simple / 1000; // kNm
        } else {
            As_simple = NaN;
            Md_simple = NaN;
        }

        // Calculate Astot and Mdtot assuming As1 = As2 = Astot/2
        // Force equilibrium: As1·σ1 + As2·σ2 + Fc = N
        // With As1 = As2 = Astot/2: (Astot/2)·σ1 + (Astot/2)·σ2 + Fc = N
        // => Astot·(σ1 + σ2)/2 = N - Fc
        // => Astot = 2·(N - Fc) / (σ1 + σ2)
        let Astot, Mdtot;

        const sigma_sum = sigma1 + sigma2;
        if (Math.abs(sigma_sum) > 1e-6) {
            // Calculate Astot from force equilibrium
            Astot = 2 * (N_Pa - concreteForces.N) / sigma_sum; // m²

            // Calculate moment with As1 = As2 = Astot/2
            // Moment equilibrium: Fc·(-yc) + As1·σ1·(-y1) + As2·σ2·(-y2) = Mdtot
            const As1_tot = Astot / 2; // m²
            const As2_tot = Astot / 2; // m²
            const Fs1_tot = As1_tot * sigma1; // N
            const Fs2_tot = As2_tot * sigma2; // N
            const Ms1_tot = Fs1_tot * (-y1_local); // Nm
            const Ms2_tot = Fs2_tot * (-y2_local); // Nm
            Mdtot = -concreteForces.M / 1000 + Ms1_tot / 1000 + Ms2_tot / 1000; // kNm
        } else {
            Astot = NaN;
            Mdtot = NaN;
        }

        return {
            name: name,
            epsTop: epsTop_pm,
            epsBottom: epsBottom_pm,
            epsS1: epsS1_pm,
            epsS2: epsS2_pm,
            Fc: Fc,
            Fs1: Fs1,
            Fs2: Fs2,
            As1: isNaN(As1) ? NaN : As1 * 10000, // convert m² to cm²
            As2: isNaN(As2) ? NaN : As2 * 10000, // convert m² to cm²
            N: N_total,
            M: M_total,
            As: isNaN(As_simple) ? NaN : As_simple * 10000, // convert m² to cm²
            Md: Md_simple, // kNm
            Astot: isNaN(Astot) ? NaN : Astot * 10000, // convert m² to cm²
            Mdtot: Mdtot // kNm
        };
    }

    // Define characteristic points (εtop, εbottom)
    const characteristicPoints = [];

    // BOD 1: Dostředný tlak (εtop = εbottom = εcu)
    characteristicPoints.push({ name: 'Bod 1', epsTop: epsCu, epsBottom: epsCu });

    // BOD 2: TOP = εcu, BOTTOM = εc2
    characteristicPoints.push({ name: 'Bod 2', epsTop: epsCu, epsBottom: epsC2 });

    // BOD 2b: TOP = εcu, BOTTOM = 0
    characteristicPoints.push({ name: 'Bod 2b', epsTop: epsCu, epsBottom: 0 });

    // BOD 3: TOP = εcu, εs2 = εyd
    const epsBottom_bod3 = (epsYd - epsCu * y2_norm) / (1 - y2_norm);
    characteristicPoints.push({ name: 'Bod 3', epsTop: epsCu, epsBottom: epsBottom_bod3 });

    // BOD 4: TOP = εcu, εs2 = εud
    const epsBottom_bod4 = (epsUd - epsCu * y2_norm) / (1 - y2_norm);
    characteristicPoints.push({ name: 'Bod 4', epsTop: epsCu, epsBottom: epsBottom_bod4 });

    // BOD 5: TOP = εc2, εs2 = εud (fixed)
    const epsBottom_bod5 = (epsUd - epsC2 * y2_norm) / (1 - y2_norm);
    characteristicPoints.push({ name: 'Bod 5', epsTop: epsC2, epsBottom: epsBottom_bod5 });

    // BOD 6: TOP = 0, εs2 = εud (fixed)
    const epsBottom_bod6 = (epsUd - 0 * y2_norm) / (1 - y2_norm);
    characteristicPoints.push({ name: 'Bod 6', epsTop: 0, epsBottom: epsBottom_bod6 });

    // BOD 7: εs1 = εyd, εs2 = εud (fixed)
    const epsTop_bod7 = (epsYd * y2_norm - epsUd * y1_norm) / (y2_norm - y1_norm);
    const epsBottom_bod7 = (epsUd - epsTop_bod7 * (1 - y2_norm)) / y2_norm;
    characteristicPoints.push({ name: 'Bod 7', epsTop: epsTop_bod7, epsBottom: epsBottom_bod7 });

    // BOD 8: Čistý tah (εtop = εbottom = εud)
    characteristicPoints.push({ name: 'Bod 8', epsTop: epsUd, epsBottom: epsUd });

    console.log('Characteristic points:', characteristicPoints);

    // Get density parameter
    const density = parseInt(document.getElementById('interaction_density').value) || 10;
    console.log('Density (subdivisions):', density);

    // Generate densified points
    for (let i = 0; i < characteristicPoints.length - 1; i++) {
        const point1 = characteristicPoints[i];
        const point2 = characteristicPoints[i + 1];

        // Add first characteristic point
        points.push(calculatePoint(point1.name, point1.epsTop, point1.epsBottom));

        // Add intermediate points
        for (let j = 1; j < density; j++) {
            const t = j / density; // interpolation parameter [0, 1]
            const epsTop_interp = point1.epsTop + t * (point2.epsTop - point1.epsTop);
            const epsBottom_interp = point1.epsBottom + t * (point2.epsBottom - point1.epsBottom);

            const name = `${point1.name}-${point2.name} (${j}/${density})`;
            points.push(calculatePoint(name, epsTop_interp, epsBottom_interp));
        }
    }

    // Add last characteristic point
    const lastPoint = characteristicPoints[characteristicPoints.length - 1];
    points.push(calculatePoint(lastPoint.name, lastPoint.epsTop, lastPoint.epsBottom));

    console.log('Total densified points:', points.length);

    // Display results in table
    displayInteractionTable(points);

    return points;
}

/**
 * Display interaction diagram points in table
 * @param {Array} points - Array of calculated points
 */
function displayInteractionTable(points) {
    const tableContainer = document.getElementById('interactionTableContainer');
    const tableBody = document.getElementById('interactionTableBody');

    // Clear existing rows
    tableBody.innerHTML = '';

    // Add rows for each point
    points.forEach(point => {
        const row = document.createElement('tr');

        // Helper to format numbers with color coding
        const formatValue = (value, decimals = 2) => {
            if (isNaN(value) || !isFinite(value)) {
                return '<span style="color: #999;">N/A</span>';
            }
            const formatted = value.toFixed(decimals);
            const className = value < 0 ? 'negative' : (value > 0 ? 'positive' : '');
            return `<span class="${className}">${formatted}</span>`;
        };

        row.innerHTML = `
            <td>${point.name}</td>
            <td>${formatValue(point.epsTop, 2)}</td>
            <td>${formatValue(point.epsBottom, 2)}</td>
            <td>${formatValue(point.epsS1, 2)}</td>
            <td>${formatValue(point.epsS2, 2)}</td>
            <td>${formatValue(point.Fc, 2)}</td>
            <td>${formatValue(point.Fs1, 2)}</td>
            <td>${formatValue(point.Fs2, 2)}</td>
            <td>${formatValue(point.As1, 2)}</td>
            <td>${formatValue(point.As2, 2)}</td>
            <td>${formatValue(point.N, 2)}</td>
            <td>${formatValue(point.M, 2)}</td>
            <td>${formatValue(point.As, 2)}</td>
            <td>${formatValue(point.Md, 2)}</td>
            <td>${formatValue(point.Astot, 2)}</td>
            <td>${formatValue(point.Mdtot, 2)}</td>
        `;

        tableBody.appendChild(row);
    });

    // Show table
    tableContainer.style.display = 'block';
}

/**
 * Calculate reinforcement for given N and M in interaction diagram tab
 * Uses strain analysis from "Analýza přetvoření a napětí" tab
 */
function calculateInteractionReinforcement() {
    console.log('=== CALCULATING INTERACTION REINFORCEMENT ===');

    // 1. Get strain data from analysis dialog
    const kqData = calculateKQ();

    if (!kqData) {
        console.log('kqData is null, showing alert');
        alert('Nejprve nastavte přetvoření v záložce "Analýza přetvoření a napětí průřezu"!');
        return;
    }
    console.log('kqData is valid, continuing...');

    // 2. Get loads from interaction tab
    const N = parseFloat(document.getElementById('interaction_N').value) || 0; // kN
    const M = parseFloat(document.getElementById('interaction_M').value) || 0; // kNm

    // 3. Get geometry and materials
    const b = parseFloat(document.getElementById('b').value) || 0.3; // m
    const h = parseFloat(document.getElementById('h').value) || 0.5; // m
    const fcd = parseFloat(document.getElementById('fcd').value) || -20000000; // Pa
    const fyd = parseFloat(document.getElementById('fyd').value) || 435000000; // Pa
    const Es = parseFloat(document.getElementById('Es').value) || 200000000000; // Pa
    const layer1_distance = parseFloat(document.getElementById('layer1_yPos').value) || 0.05; // m
    const y1 = h - layer1_distance; // absolute coordinate
    const y2 = parseFloat(document.getElementById('layer2_yPos').value) || 0.05; // m

    // 4. Calculate stresses in reinforcement from strain
    const h_2 = h / 2;
    const y1_local = y1 - (h - h_2); // convert to local system
    const y2_local = y2 - (h - h_2);

    const eps_s1 = kqData.k * y1_local + kqData.q;
    const eps_s2 = kqData.k * y2_local + kqData.q;

    // Bilinear steel diagram
    const sigma1_elastic = eps_s1 * Es;
    const sigma1 = Math.max(Math.min(sigma1_elastic, fyd), -fyd);

    const sigma2_elastic = eps_s2 * Es;
    const sigma2 = Math.max(Math.min(sigma2_elastic, fyd), -fyd);

    // 5. Calculate internal forces from concrete
    const concreteForces = fastConcreteNM(b, h, kqData.k, kqData.q, fcd);
    const Fc = concreteForces.N; // N
    const Mc = concreteForces.M; // Nm

    // Calculate centroid of compression zone
    const yc = (Math.abs(Fc) > 1e-6) ? Mc / Fc : 0; // m

    // 6. SOLVE SYSTEM OF EQUATIONS FOR As1 and As2
    // Force equilibrium:     As1·σ1 + As2·σ2 + Fc = N
    // Moment equilibrium: As1·σ1·(-y1) + As2·σ2·(-y2) + Fc·(-yc) = M

    const N_Pa = N * 1000; // kN -> N
    const M_Pa = M * 1000; // kNm -> Nm

    // Right hand side (without unknowns)
    const RHS_N = N_Pa - Fc;        // N
    const RHS_M = -M_Pa - Mc;       // Nm

    // System determinant: D = σ1·σ2·(y2 - y1)
    const det = sigma1 * sigma2 * (y2_local - y1_local);

    let As1, As2;

    if (Math.abs(det) < 1e-6) {
        // Singular system - reinforcement has same stress and position
        alert('Nelze vypočítat výztuž - singulární soustava rovnic!\n' +
              'Zkuste změnit přetvoření nebo polohu výztuže.');
        console.error('Singular system: det =', det);
        return;
    }

    // Cramer's rule
    As1 = (RHS_N * y2_local - RHS_M) / (sigma1 * (y2_local - y1_local));
    As2 = (RHS_M - y1_local * RHS_N) / (sigma2 * (y2_local - y1_local));

    // Forces in reinforcement
    const Fs1 = As1 * sigma1; // N
    const Fs2 = As2 * sigma2; // N

    console.log('=== SOLUTION ===');
    console.log('As1 =', As1, 'm² =', (As1*10000).toFixed(2), 'cm²');
    console.log('As2 =', As2, 'm² =', (As2*10000).toFixed(2), 'cm²');
    console.log('Fs1 =', Fs1/1000, 'kN');
    console.log('Fs2 =', Fs2/1000, 'kN');

    // 7. Display results
    const resultsDiv = document.getElementById('interactionDesignResults');
    const as1Result = document.getElementById('interactionAs1Result');
    const as2Result = document.getElementById('interactionAs2Result');

    as1Result.innerHTML = `• <strong style="color: #e74c3c;">As<sub>1</sub></strong> (horní) = <strong>${(As1*10000).toFixed(2)} cm²</strong> = ${(As1*1e6).toFixed(0)} mm² <span style="font-size: 12px; color: #666;">(Fs1 = ${(Fs1/1000).toFixed(2)} kN)</span>`;
    as2Result.innerHTML = `• <strong style="color: #3498db;">As<sub>2</sub></strong> (dolní) = <strong>${(As2*10000).toFixed(2)} cm²</strong> = ${(As2*1e6).toFixed(0)} mm² <span style="font-size: 12px; color: #666;">(Fs2 = ${(Fs2/1000).toFixed(2)} kN)</span>`;

    resultsDiv.style.display = 'block';

    console.log('Results displayed successfully');
}

// ========================================================================
// UI AND DRAWING FUNCTIONS - User Interface and Canvas Visualization
// ========================================================================

// Wait for DOM to be fully loaded
document.addEventListener('DOMContentLoaded', function() {
    // Initialize all UI components
    initializeUI();
});

function initializeUI() {
    // Get DOM elements
    const form = document.getElementById('designForm');
    const results = document.getElementById('results');
    const canvas = document.getElementById('sectionCanvas');

    if (!canvas) {
        console.error('Canvas element not found!');
        return;
    }

    const ctx = canvas.getContext('2d');

    // ============================================
    // MAIN TAB SYSTEM
    // ============================================

    const mainTabButtons = document.querySelectorAll('.main-tab-button');
    const mainTabContents = document.querySelectorAll('.main-tab-content');

    mainTabButtons.forEach(button => {
        button.addEventListener('click', () => {
            // Remove active from all main tabs
            mainTabButtons.forEach(btn => btn.classList.remove('active'));
            mainTabContents.forEach(content => content.classList.remove('active'));

            // Add active to current tab
            button.classList.add('active');
            const mainTabId = button.getAttribute('data-main-tab');
            document.getElementById(mainTabId).classList.add('active');

            // When switching to input tab - redraw section
            if (mainTabId === 'zadani') {
                setTimeout(() => {
                    resizeCanvas();
                }, 50);
            }
        });
    });

    // ============================================
    // SECONDARY TAB SYSTEM (within input tab)
    // ============================================

    const tabButtons = document.querySelectorAll('.tab-button');
    const tabContents = document.querySelectorAll('.tab-content');

    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            // Remove active from all tabs
            tabButtons.forEach(btn => btn.classList.remove('active'));
            tabContents.forEach(content => content.classList.remove('active'));

            // Add active to current tab
            button.classList.add('active');
            const tabId = button.getAttribute('data-tab');
            document.getElementById(tabId).classList.add('active');
        });
    });

    // ============================================
    // CANVAS FUNCTIONS
    // ============================================

    /**
     * Resize canvas to fit container
     */
    function resizeCanvas() {
        const container = canvas.parentElement;
        const size = Math.min(container.clientWidth - 40, container.clientHeight - 40, 1400);
        canvas.width = size;
        canvas.height = size;
        drawSection();
    }

    /**
     * Draw reinforced concrete cross-section
     * Shows geometry with reinforcement layers
     */
    function drawSection() {
        // Clear canvas
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Get values
        const b = parseFloat(document.getElementById('b').value) || 0.3;
        const h = parseFloat(document.getElementById('h').value) || 0.5;
        const layer1_distance = parseFloat(document.getElementById('layer1_yPos').value) || 0.05;
        const y1 = h - layer1_distance;
        const y2 = parseFloat(document.getElementById('layer2_yPos').value) || 0.05;

        // Scale - use max 80% of canvas
        const maxDim = Math.max(b, h);
        const scale = (canvas.width * 0.8) / maxDim;
        const offsetX = (canvas.width - b * scale) / 2;
        const offsetY = (canvas.height - h * scale) / 2;

        // Draw concrete - main section
        ctx.fillStyle = '#d0d0d0';
        ctx.fillRect(offsetX, offsetY, b * scale, h * scale);

        // Concrete border
        ctx.strokeStyle = '#333333';
        ctx.lineWidth = 4;
        ctx.strokeRect(offsetX, offsetY, b * scale, h * scale);

        // Draw helper lines for coordinate system
        ctx.strokeStyle = '#999999';
        ctx.lineWidth = 1;
        ctx.setLineDash([10, 5]);

        // Horizontal axis Y=0 (if in cross-section)
        if (y2 < h && y1 > 0) {
            const y0Canvas = offsetY + h * scale;
            ctx.beginPath();
            ctx.moveTo(offsetX - 30, y0Canvas);
            ctx.lineTo(offsetX + b * scale + 30, y0Canvas);
            ctx.stroke();
        }

        ctx.setLineDash([]);

        // Draw reinforcement layers as horizontal lines
        const reinforcementLineWidth = 8;
        const reinforcementOffset = reinforcementLineWidth * 3;

        // Calculate positions on canvas (Y grows upward)
        const y1Canvas = offsetY + h * scale - y1 * scale;
        const y2Canvas = offsetY + h * scale - y2 * scale;

        // Layer 1 (top) - red line with dark outline
        ctx.strokeStyle = '#721c24';
        ctx.lineWidth = reinforcementLineWidth + 2;
        ctx.beginPath();
        ctx.moveTo(offsetX + reinforcementOffset, y1Canvas);
        ctx.lineTo(offsetX + b * scale - reinforcementOffset, y1Canvas);
        ctx.stroke();

        ctx.strokeStyle = '#dc3545';
        ctx.lineWidth = reinforcementLineWidth;
        ctx.beginPath();
        ctx.moveTo(offsetX + reinforcementOffset, y1Canvas);
        ctx.lineTo(offsetX + b * scale - reinforcementOffset, y1Canvas);
        ctx.stroke();

        // Layer 2 (bottom) - blue line with dark outline
        ctx.strokeStyle = '#004085';
        ctx.lineWidth = reinforcementLineWidth + 2;
        ctx.beginPath();
        ctx.moveTo(offsetX + reinforcementOffset, y2Canvas);
        ctx.lineTo(offsetX + b * scale - reinforcementOffset, y2Canvas);
        ctx.stroke();

        ctx.strokeStyle = '#007bff';
        ctx.lineWidth = reinforcementLineWidth;
        ctx.beginPath();
        ctx.moveTo(offsetX + reinforcementOffset, y2Canvas);
        ctx.lineTo(offsetX + b * scale - reinforcementOffset, y2Canvas);
        ctx.stroke();

        // Dimension labels
        ctx.font = 'bold 18px Arial';
        ctx.fillStyle = '#333333';

        // Width b
        ctx.fillText(`b = ${(b * 1000).toFixed(0)} mm`, offsetX, offsetY - 20);

        // Height h
        ctx.save();
        ctx.translate(offsetX + b * scale + 30, offsetY + h * scale / 2);
        ctx.rotate(-Math.PI / 2);
        ctx.fillText(`h = ${(h * 1000).toFixed(0)} mm`, 0, 0);
        ctx.restore();

        // Layer labels with Y coordinates
        ctx.font = 'bold 16px Arial';

        // Layer 1
        ctx.fillStyle = '#dc3545';
        const label1X = offsetX + b * scale + 15;
        ctx.fillText(`Vrstva 1 (horní)`, label1X, y1Canvas - 5);
        ctx.font = '14px Arial';
        ctx.fillText(`y = ${y1.toFixed(3)} m`, label1X, y1Canvas + 15);

        // Layer 2
        ctx.font = 'bold 16px Arial';
        ctx.fillStyle = '#007bff';
        const label2X = offsetX + b * scale + 15;
        ctx.fillText(`Vrstva 2 (dolní)`, label2X, y2Canvas - 5);
        ctx.font = '14px Arial';
        ctx.fillText(`y = ${y2.toFixed(3)} m`, label2X, y2Canvas + 15);

        // Coordinate system - Y axis arrow
        ctx.strokeStyle = '#666666';
        ctx.lineWidth = 2;
        ctx.fillStyle = '#666666';

        const arrowX = offsetX - 60;
        const arrowYTop = offsetY;
        const arrowYBottom = offsetY + h * scale;

        // Vertical line
        ctx.beginPath();
        ctx.moveTo(arrowX, arrowYBottom);
        ctx.lineTo(arrowX, arrowYTop);
        ctx.stroke();

        // Arrow pointing up
        ctx.beginPath();
        ctx.moveTo(arrowX, arrowYTop);
        ctx.lineTo(arrowX - 8, arrowYTop + 15);
        ctx.lineTo(arrowX + 8, arrowYTop + 15);
        ctx.closePath();
        ctx.fill();

        // Y label
        ctx.font = 'bold 20px Arial';
        ctx.fillText('Y', arrowX - 10, arrowYTop - 10);

        // Scale
        ctx.font = '12px Arial';
        ctx.fillStyle = '#666666';
        ctx.fillText(`Měřítko 1:${(1/scale).toFixed(0)}`, offsetX, offsetY + h * scale + 40);
    }

    // ============================================
    // EVENT LISTENERS
    // ============================================

    form.addEventListener('submit', (e) => {
        e.preventDefault();
    });

    // Update visualization when geometry changes
    ['b', 'h', 'layer1_yPos', 'layer2_yPos'].forEach(id => {
        const element = document.getElementById(id);
        if (element) {
            element.addEventListener('input', drawSection);
        }
    });

    form.addEventListener('reset', () => {
        setTimeout(() => {
            drawSection();
            results.classList.remove('show');
        }, 10);
    });

    // Resize handler
    window.addEventListener('resize', resizeCanvas);

    // ============================================
    // STRAIN ANALYSIS POINT SELECTION
    // ============================================

    const radioButtons = document.querySelectorAll('.point-radio');
    const pointGroups = document.querySelectorAll('.point-group');

    radioButtons.forEach(radio => {
        radio.addEventListener('change', function() {
            // Update visual styles
            updatePointGroupStyles();

            // Enable/disable input fields
            updateInputFields();
        });
    });

    /**
     * Update point group styling based on selection
     */
    function updatePointGroupStyles() {
        pointGroups.forEach(group => {
            const radio = group.querySelector('.point-radio');
            if (radio && radio.checked) {
                group.classList.add('selected');
            } else {
                group.classList.remove('selected');
            }
        });
    }

    /**
     * Enable/disable strain input fields based on radio selection
     */
    function updateInputFields() {
        radioButtons.forEach(radio => {
            const point = radio.getAttribute('data-point');
            const input = document.getElementById(`strain_${point}`);
            if (input) {
                input.disabled = !radio.checked;
            }
        });
    }

    // Initialize
    updatePointGroupStyles();
    updateInputFields();

    // ============================================
    // INITIALIZATION ON PAGE LOAD
    // ============================================

    // Initial setup
    resizeCanvas();

    // Initialize equilibrium equations
    if (typeof updateEquilibrium === 'function') {
        updateEquilibrium();
    }
}

// ============================================
// STRAIN AND STRESS DIAGRAM DRAWING
// ============================================

/**
 * Draw strain distribution diagram
 * Shows linear strain distribution ε(y) = k·y + q
 */
function drawStrainDiagram(canvas, kqData, h, y1, y2, fcd, fyd, Es) {
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const width = canvas.width;
    const height = canvas.height;
    const margin = 60;
    const plotHeight = height - 2 * margin;

    const h_2 = h / 2;

    // Scale
    const yScale = plotHeight / h;
    const centerY = margin + plotHeight / 2;

    const toCanvasY = (y) => centerY - y * yScale;

    // Calculate strains at edges
    const eps_top = kqData.k * h_2 + kqData.q;
    const eps_bottom = kqData.k * (-h_2) + kqData.q;

    // Find strain range for X scale
    const eps_s1 = kqData.k * (y1 - h + h_2) + kqData.q;
    const eps_s2 = kqData.k * (y2 - h + h_2) + kqData.q;
    const allStrains = [eps_top, eps_bottom, eps_s1, eps_s2];
    const maxStrain = Math.max(...allStrains.map(Math.abs)) * 1.2;

    const xScale = (width - 2 * margin) / (2 * maxStrain);
    const centerX = margin + (width - 2 * margin) / 2;

    const toCanvasX = (eps) => centerX + eps * xScale;

    // Get concrete stress function
    const EC2 = -0.002;
    const getConcreteStress = (eps) => {
        if (eps >= 0) {
            return 0;
        }
        if (eps > EC2) {
            const epsilon_norm = eps / EC2;
            return fcd * (1 - (1 - epsilon_norm) * (1 - epsilon_norm));
        }
        return fcd;
    };

    // Get steel stress function
    const getSteelStress = (eps) => {
        const stress = eps * Es;
        return Math.max(Math.min(stress, fyd), -fyd);
    };

    // Draw cross-section as BLACK LINE
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(centerX, toCanvasY(h_2));
    ctx.lineTo(centerX, toCanvasY(-h_2));
    ctx.stroke();

    // Zero strain axis
    ctx.strokeStyle = '#999';
    ctx.lineWidth = 1;
    ctx.setLineDash([5, 5]);
    ctx.beginPath();
    ctx.moveTo(centerX, margin);
    ctx.lineTo(centerX, height - margin);
    ctx.stroke();
    ctx.setLineDash([]);

    // Strain plane
    ctx.strokeStyle = '#e74c3c';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(toCanvasX(eps_top), toCanvasY(h_2));
    ctx.lineTo(toCanvasX(eps_bottom), toCanvasY(-h_2));
    ctx.stroke();

    // Reinforcement points
    const eps_s1_local = kqData.k * (y1 - h + h_2) + kqData.q;
    const eps_s2_local = kqData.k * (y2 - h + h_2) + kqData.q;

    const sigma_s1 = getSteelStress(eps_s1_local);
    const sigma_s2 = getSteelStress(eps_s2_local);

    // Top reinforcement
    ctx.fillStyle = '#dc3545';
    ctx.beginPath();
    ctx.arc(toCanvasX(eps_s1_local), toCanvasY(y1 - h + h_2), 8, 0, 2 * Math.PI);
    ctx.fill();
    ctx.strokeStyle = '#000';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Bottom reinforcement
    ctx.fillStyle = '#007bff';
    ctx.beginPath();
    ctx.arc(toCanvasX(eps_s2_local), toCanvasY(y2 - h + h_2), 8, 0, 2 * Math.PI);
    ctx.fill();
    ctx.strokeStyle = '#000';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Concrete stresses at edges
    const sigma_top = getConcreteStress(eps_top);
    const sigma_bottom = getConcreteStress(eps_bottom);

    // Strain and stress labels
    ctx.fillStyle = '#333';
    ctx.font = 'bold 13px Arial';
    ctx.fillText('ε = 0', centerX + 5, margin - 10);

    // Top edge
    ctx.fillStyle = '#e74c3c';
    ctx.font = 'bold 12px Arial';
    const topX = toCanvasX(eps_top) + 10;
    ctx.fillText(`εtop = ${(eps_top * 1000).toFixed(2)}‰`, topX, toCanvasY(h_2) - 15);
    ctx.font = '11px Arial';
    ctx.fillText(`σc = ${(sigma_top / 1e6).toFixed(2)} MPa`, topX, toCanvasY(h_2));

    // Bottom edge
    ctx.fillStyle = '#e74c3c';
    ctx.font = 'bold 12px Arial';
    const botX = toCanvasX(eps_bottom) + 10;
    ctx.fillText(`εbot = ${(eps_bottom * 1000).toFixed(2)}‰`, botX, toCanvasY(-h_2) + 5);
    ctx.font = '11px Arial';
    ctx.fillText(`σc = ${(sigma_bottom / 1e6).toFixed(2)} MPa`, botX, toCanvasY(-h_2) + 20);

    // Reinforcement labels
    ctx.font = 'bold 12px Arial';
    ctx.fillStyle = '#dc3545';
    const s1X = toCanvasX(eps_s1_local) + 10;
    const s1Y = toCanvasY(y1 - h + h_2);
    ctx.fillText(`A1: εs1 = ${(eps_s1_local * 1000).toFixed(2)}‰`, s1X, s1Y - 10);
    ctx.font = '11px Arial';
    ctx.fillText(`σs1 = ${(sigma_s1 / 1e6).toFixed(1)} MPa`, s1X, s1Y + 5);

    ctx.font = 'bold 12px Arial';
    ctx.fillStyle = '#007bff';
    const s2X = toCanvasX(eps_s2_local) + 10;
    const s2Y = toCanvasY(y2 - h + h_2);
    ctx.fillText(`A2: εs2 = ${(eps_s2_local * 1000).toFixed(2)}‰`, s2X, s2Y - 10);
    ctx.font = '11px Arial';
    ctx.fillText(`σs2 = ${(sigma_s2 / 1e6).toFixed(1)} MPa`, s2X, s2Y + 5);

    // Parameters k, q
    ctx.fillStyle = '#333';
    ctx.font = 'bold 14px Arial';
    ctx.fillText(`k = ${kqData.k.toFixed(6)} [-/m]`, margin, height - margin + 30);
    ctx.fillText(`q = ${(kqData.q * 1000).toFixed(3)} ‰`, margin, height - margin + 50);
}

/**
 * Draw stress distribution diagram
 * Parabolic-rectangular diagram according to EC2
 */
function drawStressDiagram(canvas, kqData, h, b, fcd) {
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const width = canvas.width;
    const height = canvas.height;
    const margin = 80;
    const plotHeight = height - 2 * margin;

    const h_2 = h / 2;
    const k = kqData.k;
    const q = kqData.q;
    const EC2 = -0.002;

    // Y scale (section height)
    const yScale = plotHeight / h;
    const centerY = margin + plotHeight / 2;

    const toCanvasY = (y) => centerY - y * yScale;

    // Find stress range for correct scale
    const steps = 200;
    let minStress = 0;
    let maxStress = 0;

    for (let i = 0; i <= steps; i++) {
        const t = i / steps;
        const y = -h_2 + t * h;
        const eps = k * y + q;

        let sigma = 0;
        if (eps >= 0) {
            sigma = 0;
        } else if (eps > EC2) {
            const epsilon_norm = eps / EC2;
            sigma = fcd * (1 - (1 - epsilon_norm) * (1 - epsilon_norm));
        } else {
            sigma = fcd;
        }

        minStress = Math.min(minStress, sigma);
        maxStress = Math.max(maxStress, sigma);
    }

    // Stress scale - dynamic based on actual range
    const stressRange = Math.max(Math.abs(minStress), Math.abs(maxStress)) * 1.2;
    const plotWidth = width - 2 * margin;
    const xScale = plotWidth / (stressRange * 2);
    const centerX = margin + plotWidth / 2;

    const toCanvasX = (stress) => centerX + stress * xScale;

    // Draw cross-section as BLACK LINE
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(centerX, toCanvasY(h_2));
    ctx.lineTo(centerX, toCanvasY(-h_2));
    ctx.stroke();

    // Zero stress axis
    ctx.strokeStyle = '#999';
    ctx.lineWidth = 1;
    ctx.setLineDash([5, 5]);
    ctx.beginPath();
    ctx.moveTo(centerX, margin);
    ctx.lineTo(centerX, height - margin);
    ctx.stroke();
    ctx.setLineDash([]);

    // Draw stress diagram
    const points = [];
    let hasCompression = false;

    for (let i = 0; i <= steps; i++) {
        const t = i / steps;
        const y = -h_2 + t * h;
        const eps = k * y + q;

        let sigma = 0;

        // Calculate stress according to parabolic-rectangular diagram
        if (eps >= 0) {
            sigma = 0;
        } else if (eps > EC2) {
            const epsilon_norm = eps / EC2;
            sigma = fcd * (1 - (1 - epsilon_norm) * (1 - epsilon_norm));
            hasCompression = true;
        } else {
            sigma = fcd;
            hasCompression = true;
        }

        points.push({y: y, sigma: sigma});
    }

    // Draw diagram - from bottom up along curve, back along axis
    ctx.beginPath();

    // Start at bottom edge on axis
    ctx.moveTo(centerX, toCanvasY(-h_2));

    // Draw up along stress curve
    for (let i = 0; i < points.length; i++) {
        const x = toCanvasX(points[i].sigma);
        const yCanvas = toCanvasY(points[i].y);
        ctx.lineTo(x, yCanvas);
    }

    // From top edge back to axis
    ctx.lineTo(centerX, toCanvasY(h_2));

    // Close path back to start
    ctx.closePath();

    // Fill and stroke
    if (hasCompression) {
        ctx.fillStyle = 'rgba(52, 152, 219, 0.3)';
        ctx.fill();
    }
    ctx.strokeStyle = '#2c3e50';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Find neutral axis (where eps = 0)
    let y_neutral = (Math.abs(k) > 1e-10) ? -q / k : ((q >= 0) ? h_2 : -h_2);

    // Mark neutral axis if in cross-section
    if (y_neutral >= -h_2 && y_neutral <= h_2) {
        ctx.strokeStyle = '#ff6b6b';
        ctx.lineWidth = 2;
        ctx.setLineDash([8, 4]);
        ctx.beginPath();
        ctx.moveTo(margin, toCanvasY(y_neutral));
        ctx.lineTo(width - margin, toCanvasY(y_neutral));
        ctx.stroke();
        ctx.setLineDash([]);

        ctx.fillStyle = '#ff6b6b';
        ctx.font = 'bold 11px Arial';
        ctx.fillText('N.O. (ε=0)', width - margin - 60, toCanvasY(y_neutral) - 5);

        // Height of compressed zone
        const x_compressed = y_neutral - (-h_2);
        ctx.fillStyle = '#ff6b6b';
        ctx.font = '11px Arial';
        ctx.fillText(`x = ${(x_compressed * 1000).toFixed(1)} mm`, margin + 5, toCanvasY(y_neutral) + 15);
    }

    // Find position of breakpoint εc2 = -2‰
    let y_ec2 = (Math.abs(k) > 1e-10) ? (EC2 - q) / k : null;

    // Mark breakpoint if in cross-section and in compression zone
    let showEc2Line = false;
    if (y_ec2 !== null && y_ec2 >= -h_2 && y_ec2 <= h_2) {
        const eps_top = k * h_2 + q;
        const eps_bottom = k * (-h_2) + q;

        const ec2_between_edges = (eps_top <= EC2 && eps_bottom >= EC2) || (eps_top >= EC2 && eps_bottom <= EC2);
        const has_compression = (eps_top < 0 || eps_bottom < 0);

        if (ec2_between_edges && has_compression) {
            showEc2Line = true;
        }
    }

    if (showEc2Line) {
        ctx.strokeStyle = '#9b59b6';
        ctx.lineWidth = 2;
        ctx.setLineDash([8, 4]);
        ctx.beginPath();
        ctx.moveTo(margin, toCanvasY(y_ec2));
        ctx.lineTo(width - margin, toCanvasY(y_ec2));
        ctx.stroke();
        ctx.setLineDash([]);

        ctx.fillStyle = '#9b59b6';
        ctx.font = 'bold 11px Arial';
        ctx.fillText('εc2 = -2‰', width - margin - 70, toCanvasY(y_ec2) - 5);

        // Distance from bottom edge
        const x_ec2 = y_ec2 - (-h_2);
        ctx.fillStyle = '#9b59b6';
        ctx.font = '11px Arial';
        ctx.fillText(`x = ${(x_ec2 * 1000).toFixed(1)} mm`, margin + 5, toCanvasY(y_ec2) + 15);
    }

    // Labels
    ctx.fillStyle = '#333';
    ctx.font = '12px Arial';
    ctx.fillText('σ = 0', centerX + 5, margin - 10);

    // Stress scale
    if (hasCompression) {
        ctx.fillText(`σmax = ${(minStress / 1e6).toFixed(1)} MPa`, margin, height - margin + 30);
    }
    ctx.fillText(`fcd = ${(fcd / 1e6).toFixed(1)} MPa`, margin, height - margin + 50);

    // Legend
    ctx.font = 'bold 12px Arial';
    ctx.fillStyle = '#2c3e50';
    ctx.fillText('Parabolicko-obdélníkový', margin, margin - 40);
    ctx.fillText('diagram betonu', margin, margin - 25);
    ctx.font = '10px Arial';
    ctx.fillStyle = '#666';
    ctx.fillText('(konstanta pokračuje do ∞)', margin, margin - 10);
}

/**
 * Draw combined design diagram with stress resultants
 * Shows stress distribution with force arrows
 */
function drawDesignCombinedDiagram(kqData, h, b, fcd, sigma1, sigma2, y1_local, y2_local, Fc, Fs1, Fs2, yc, Mc, Ms1, Ms2) {
    const canvas = document.getElementById('designCombinedCanvas');

    if (!canvas) {
        console.error('Canvas element not found!');
        return;
    }

    // Set canvas size based on container
    const container = canvas.parentElement;
    const size = Math.min(container.clientWidth - 40, container.clientHeight - 40, 1400);
    canvas.width = size;
    canvas.height = size;

    const ctx = canvas.getContext('2d');

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const width = canvas.width;
    const height = canvas.height;
    const margin = 80;
    const plotHeight = height - 2 * margin;

    const h_2 = h / 2;
    const yScale = plotHeight / h;
    const centerY = margin + plotHeight / 2;

    const toCanvasY = (y) => centerY - y * yScale;

    // Find max stress for scale
    const EC2 = -0.002;
    const steps = 100;
    let minStress = 0;
    let maxStress = 0;

    for (let i = 0; i <= steps; i++) {
        const t = i / steps;
        const y = -h_2 + t * h;
        const eps = kqData.k * y + kqData.q;

        let sigma = 0;
        if (eps >= 0) {
            sigma = 0;
        } else if (eps > EC2) {
            const epsilon_norm = eps / EC2;
            sigma = fcd * (1 - (1 - epsilon_norm) * (1 - epsilon_norm));
        } else {
            sigma = fcd;
        }

        minStress = Math.min(minStress, sigma);
        maxStress = Math.max(maxStress, sigma);
    }

    // Stress scale
    const stressRange = Math.max(Math.abs(minStress), Math.abs(maxStress)) * 1.3;
    const plotWidth = width - 2 * margin - 150;
    const xScale = plotWidth / (stressRange * 2);
    const centerX = margin + plotWidth / 2;

    const toCanvasX = (stress) => centerX + stress * xScale;

    // ============================================
    // DRAW CROSS-SECTION
    // ============================================
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(centerX, toCanvasY(h_2));
    ctx.lineTo(centerX, toCanvasY(-h_2));
    ctx.stroke();

    // Centroid line
    ctx.strokeStyle = '#666';
    ctx.lineWidth = 1;
    ctx.setLineDash([5, 5]);
    ctx.beginPath();
    ctx.moveTo(margin, centerY);
    ctx.lineTo(width - margin, centerY);
    ctx.stroke();
    ctx.setLineDash([]);

    ctx.fillStyle = '#666';
    ctx.font = 'bold 12px Arial';
    ctx.fillText('T.P.', margin - 30, centerY - 5);

    // Zero stress axis
    ctx.strokeStyle = '#999';
    ctx.lineWidth = 1;
    ctx.setLineDash([3, 3]);
    ctx.beginPath();
    ctx.moveTo(centerX, margin);
    ctx.lineTo(centerX, height - margin);
    ctx.stroke();
    ctx.setLineDash([]);

    // ============================================
    // DRAW CONCRETE STRESS DIAGRAM
    // ============================================
    const points = [];
    let hasCompression = false;

    for (let i = 0; i <= steps; i++) {
        const t = i / steps;
        const y = -h_2 + t * h;
        const eps = kqData.k * y + kqData.q;

        let sigma = 0;
        if (eps >= 0) {
            sigma = 0;
        } else if (eps > EC2) {
            const epsilon_norm = eps / EC2;
            sigma = fcd * (1 - (1 - epsilon_norm) * (1 - epsilon_norm));
            hasCompression = true;
        } else {
            sigma = fcd;
            hasCompression = true;
        }

        points.push({y: y, sigma: sigma});
    }

    // Draw diagram
    ctx.beginPath();
    ctx.moveTo(centerX, toCanvasY(-h_2));

    for (let i = 0; i < points.length; i++) {
        const x = toCanvasX(points[i].sigma);
        const yCanvas = toCanvasY(points[i].y);
        ctx.lineTo(x, yCanvas);
    }

    ctx.lineTo(centerX, toCanvasY(h_2));
    ctx.closePath();

    if (hasCompression) {
        ctx.fillStyle = 'rgba(52, 152, 219, 0.3)';
        ctx.fill();
    }
    ctx.strokeStyle = '#2c3e50';
    ctx.lineWidth = 2;
    ctx.stroke();

    // ============================================
    // DRAW REINFORCEMENT STRESSES
    // ============================================

    // Top reinforcement
    ctx.strokeStyle = '#e74c3c';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(centerX, toCanvasY(y1_local));
    ctx.lineTo(toCanvasX(sigma1), toCanvasY(y1_local));
    ctx.stroke();

    ctx.fillStyle = '#e74c3c';
    ctx.beginPath();
    ctx.arc(toCanvasX(sigma1), toCanvasY(y1_local), 6, 0, 2 * Math.PI);
    ctx.fill();

    // Bottom reinforcement
    ctx.strokeStyle = '#3498db';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(centerX, toCanvasY(y2_local));
    ctx.lineTo(toCanvasX(sigma2), toCanvasY(y2_local));
    ctx.stroke();

    ctx.fillStyle = '#3498db';
    ctx.beginPath();
    ctx.arc(toCanvasX(sigma2), toCanvasY(y2_local), 6, 0, 2 * Math.PI);
    ctx.fill();

    // ============================================
    // DRAW FORCE RESULTANTS - ARROWS
    // ============================================

    const arrowStartX = width - margin - 100;

    // Force Fc (concrete) - gray arrow
    const arrowY_Fc = toCanvasY(yc);

    ctx.strokeStyle = '#7f8c8d';
    ctx.fillStyle = '#7f8c8d';
    ctx.lineWidth = 4;

    const arrowDir_Fc = Fc < 0 ? 1 : -1;

    // Arrow line
    ctx.beginPath();
    ctx.moveTo(centerX + 20, arrowY_Fc);
    ctx.lineTo(arrowStartX, arrowY_Fc);
    ctx.stroke();

    // Arrow head
    ctx.beginPath();
    ctx.moveTo(arrowStartX, arrowY_Fc);
    ctx.lineTo(arrowStartX - 8 * arrowDir_Fc, arrowY_Fc - 6);
    ctx.lineTo(arrowStartX - 8 * arrowDir_Fc, arrowY_Fc + 6);
    ctx.closePath();
    ctx.fill();

    // Force label
    ctx.font = 'bold 13px Arial';
    ctx.fillText(`Fc = ${(Fc/1000).toFixed(1)} kN`, arrowStartX + 15, arrowY_Fc - 8);

    // Distance from centroid - dimension
    if (Math.abs(yc) > 0.001) {
        ctx.strokeStyle = '#666';
        ctx.lineWidth = 1;
        ctx.setLineDash([2, 2]);
        ctx.beginPath();
        ctx.moveTo(arrowStartX + 10, centerY);
        ctx.lineTo(arrowStartX + 10, arrowY_Fc);
        ctx.stroke();
        ctx.setLineDash([]);

        // Dimension arrows
        ctx.beginPath();
        ctx.moveTo(arrowStartX + 10, centerY);
        ctx.lineTo(arrowStartX + 13, centerY + 4);
        ctx.moveTo(arrowStartX + 10, centerY);
        ctx.lineTo(arrowStartX + 7, centerY + 4);
        ctx.stroke();

        ctx.beginPath();
        ctx.moveTo(arrowStartX + 10, arrowY_Fc);
        ctx.lineTo(arrowStartX + 13, arrowY_Fc - 4);
        ctx.moveTo(arrowStartX + 10, arrowY_Fc);
        ctx.lineTo(arrowStartX + 7, arrowY_Fc - 4);
        ctx.stroke();

        ctx.fillStyle = '#666';
        ctx.font = '11px Arial';
        const yc_mm = -yc * 1000;
        ctx.fillText(`yc=${yc_mm.toFixed(0)}mm`, arrowStartX + 15, (centerY + arrowY_Fc) / 2);
    }

    // Force Fs1 (top reinforcement) - red arrow
    const arrowY_Fs1 = toCanvasY(y1_local);

    ctx.strokeStyle = '#e74c3c';
    ctx.fillStyle = '#e74c3c';
    ctx.lineWidth = 3;

    const arrowDir_Fs1 = Fs1 > 0 ? -1 : 1;

    // Arrow line
    ctx.beginPath();
    ctx.moveTo(centerX + 20, arrowY_Fs1);
    ctx.lineTo(arrowStartX - 20, arrowY_Fs1);
    ctx.stroke();

    // Arrow head
    ctx.beginPath();
    ctx.moveTo(arrowStartX - 20, arrowY_Fs1);
    ctx.lineTo(arrowStartX - 20 - 7 * arrowDir_Fs1, arrowY_Fs1 - 5);
    ctx.lineTo(arrowStartX - 20 - 7 * arrowDir_Fs1, arrowY_Fs1 + 5);
    ctx.closePath();
    ctx.fill();

    // Label
    ctx.font = 'bold 12px Arial';
    ctx.fillText(`Fs1 = ${(Fs1/1000).toFixed(2)} kN`, arrowStartX + 15, arrowY_Fs1 + 5);

    // Reinforcement on cross-section
    ctx.fillStyle = '#e74c3c';
    ctx.beginPath();
    ctx.arc(centerX, toCanvasY(y1_local), 6, 0, 2 * Math.PI);
    ctx.fill();
    ctx.strokeStyle = '#c0392b';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Distance
    ctx.fillStyle = '#e74c3c';
    ctx.font = '10px Arial';
    ctx.fillText(`y1=${(y1_local*1000).toFixed(0)}mm`, centerX + 10, toCanvasY(y1_local) - 10);

    // Force Fs2 (bottom reinforcement) - blue arrow
    const arrowY_Fs2 = toCanvasY(y2_local);

    ctx.strokeStyle = '#3498db';
    ctx.fillStyle = '#3498db';
    ctx.lineWidth = 3;

    const arrowDir_Fs2 = Fs2 > 0 ? -1 : 1;

    // Arrow line
    ctx.beginPath();
    ctx.moveTo(centerX + 20, arrowY_Fs2);
    ctx.lineTo(arrowStartX - 20, arrowY_Fs2);
    ctx.stroke();

    // Arrow head
    ctx.beginPath();
    ctx.moveTo(arrowStartX - 20, arrowY_Fs2);
    ctx.lineTo(arrowStartX - 20 - 7 * arrowDir_Fs2, arrowY_Fs2 - 5);
    ctx.lineTo(arrowStartX - 20 - 7 * arrowDir_Fs2, arrowY_Fs2 + 5);
    ctx.closePath();
    ctx.fill();

    // Label
    ctx.font = 'bold 12px Arial';
    ctx.fillText(`Fs2 = ${(Fs2/1000).toFixed(2)} kN`, arrowStartX + 15, arrowY_Fs2 + 5);

    // Reinforcement on cross-section
    ctx.fillStyle = '#3498db';
    ctx.beginPath();
    ctx.arc(centerX, toCanvasY(y2_local), 6, 0, 2 * Math.PI);
    ctx.fill();
    ctx.strokeStyle = '#2980b9';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Distance
    ctx.fillStyle = '#3498db';
    ctx.font = '10px Arial';
    ctx.fillText(`y2=${(y2_local*1000).toFixed(0)}mm`, centerX + 10, toCanvasY(y2_local) + 20);

    // ============================================
    // LABELS AND LEGEND
    // ============================================

    // Title
    ctx.fillStyle = '#2c3e50';
    ctx.font = 'bold 16px Arial';
    ctx.fillText('Diagram napětí s výslednicemi sil', margin, margin - 50);

    ctx.font = '12px Arial';
    ctx.fillText('(Parabolicko-obdélníkový diagram betonu)', margin, margin - 30);

    // Axis labels
    ctx.fillStyle = '#333';
    ctx.font = '12px Arial';
    ctx.fillText('σ = 0', centerX + 5, margin - 10);

    // Stress values in reinforcement
    ctx.fillStyle = '#e74c3c';
    ctx.font = 'bold 11px Arial';
    ctx.fillText(`σ1 = ${(sigma1/1e6).toFixed(1)} MPa`, toCanvasX(sigma1) - 70, toCanvasY(y1_local) - 10);

    ctx.fillStyle = '#3498db';
    ctx.fillText(`σ2 = ${(sigma2/1e6).toFixed(1)} MPa`, toCanvasX(sigma2) - 70, toCanvasY(y2_local) + 20);

    // Moments in lower left corner
    ctx.fillStyle = '#333';
    ctx.font = '11px Arial';
    ctx.fillText(`Mc = ${(-Mc/1000).toFixed(2)} kNm`, margin, height - margin + 25);
    ctx.fillText(`Ms1 = ${(Ms1/1000).toFixed(2)} kNm`, margin, height - margin + 40);
    ctx.fillText(`Ms2 = ${(Ms2/1000).toFixed(2)} kNm`, margin, height - margin + 55);

    // Strain parameters
    ctx.fillStyle = '#666';
    ctx.font = '11px Arial';
    ctx.fillText(`k = ${kqData.k.toFixed(6)} [1/m]`, width - margin - 150, margin - 30);
    ctx.fillText(`q = ${(kqData.q*1000).toFixed(3)} ‰`, width - margin - 150, margin - 15);
}

/**
 * Main strain analysis calculation
 * Calculates and draws strain/stress diagrams
 */
function calculateStrainAnalysis() {
    // Get cross-section parameters
    const b = parseFloat(document.getElementById('b').value);
    const h = parseFloat(document.getElementById('h').value);
    const fcd = parseFloat(document.getElementById('fcd').value);
    const fyd = parseFloat(document.getElementById('fyd').value);
    const Es = parseFloat(document.getElementById('Es').value);
    const layer1_distance = parseFloat(document.getElementById('layer1_yPos').value);
    const y1 = h - layer1_distance;
    const y2 = parseFloat(document.getElementById('layer2_yPos').value);

    // Calculate k and q
    const kqData = calculateKQ();
    if (!kqData) return;

    // Calculate N and M
    const result = fastConcreteNM(b, h, kqData.k, kqData.q, fcd);

    // Draw diagrams
    const strainCanvas = document.getElementById('strainCanvas');
    const stressCanvas = document.getElementById('stressCanvas');

    drawStrainDiagram(strainCanvas, kqData, h, y1, y2, fcd, fyd, Es);
    drawStressDiagram(stressCanvas, kqData, h, b, fcd);

    // Display results
    const resultsDiv = document.getElementById('analysisResults');
    const resultsContent = document.getElementById('analysisResultsContent');

    resultsContent.innerHTML = `
        <div class="result-row">
            <strong>Sklon přetvoření:</strong>
            <span>k = ${kqData.k.toFixed(6)} [-/m]</span>
        </div>
        <div class="result-row">
            <strong>Přetvoření v těžišti:</strong>
            <span>q = ${(kqData.q * 1000).toFixed(3)} ‰</span>
        </div>
        <div class="result-row">
            <strong>Normálová síla od betonu:</strong>
            <span style="color: #2196F3; font-weight: bold;">N = ${(result.N / 1000).toFixed(2)} kN</span>
        </div>
        <div class="result-row">
            <strong>Moment od betonu:</strong>
            <span style="color: #2196F3; font-weight: bold;">M = ${(result.M / 1000).toFixed(2)} kNm</span>
        </div>
        <div class="result-row">
            <strong>Vybrané body:</strong>
            <span>${getPointName(kqData.point1)} + ${getPointName(kqData.point2)}</span>
        </div>
    `;

    resultsDiv.style.display = 'block';
}

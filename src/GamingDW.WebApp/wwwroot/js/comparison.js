import { fmt, money } from './utils.js';
import { apiGet } from './api.js';
import { showToast } from './toast.js';

export async function loadComparison() {
    const from1 = document.getElementById('comp-from1').value;
    const to1 = document.getElementById('comp-to1').value;
    const from2 = document.getElementById('comp-from2').value;
    const to2 = document.getElementById('comp-to2').value;

    if (!from1 || !to1 || !from2 || !to2) {
        showToast('Please select all four dates to compare', 'warning');
        return;
    }

    try {
        const url = `/api/reports/compare?from1=${from1}&to1=${to1}&from2=${from2}&to2=${to2}`;
        const data = await apiGet(url);

        document.getElementById('comp-vs-text').textContent = 
            `${from1} to ${to1}   vs   ${from2} to ${to2}`;

        const grid = document.getElementById('compare-grid');
        grid.innerHTML = '';

        const keys = [
            { k: 'registrations', label: 'Registrations', f: fmt },
            { k: 'ftDs', label: 'FTDs', f: fmt },
            { k: 'deposits', label: 'Deposits', f: money },
            { k: 'withdrawals', label: 'Withdrawals', f: money },
            { k: 'ggr', label: 'GGR', f: money },
            { k: 'activePlayers', label: 'Active Players', f: fmt },
            { k: 'sessions', label: 'Sessions', f: fmt },
            { k: 'bonusCost', label: 'Bonus Cost', f: money },
            { k: 'netRevenue', label: 'Net Revenue', f: money }
        ];

        keys.forEach(obj => {
            const v1 = data.period1[obj.k] || 0;
            const v2 = data.period2[obj.k] || 0;
            
            // To compare Period1 vs Period2, usually we check if Period1 > Period2 means positive growth (Period 1 is newer usually)
            // But let's assume standard change calculation: (v1 - v2) / v2
            let diff = v1 - v2;
            let pct = v2 !== 0 ? (diff / Math.abs(v2)) * 100 : 0;
            
            // Inverted logic for costs (bonus, withdrawals)
            const invert = ['withdrawals', 'bonusCost'].includes(obj.k);
            let state = 'neutral';
            if (pct > 0) state = invert ? 'down' : 'up';
            else if (pct < 0) state = invert ? 'up' : 'down';

            const sign = pct > 0 ? '+' : '';
            const pctStr = v2 === 0 ? (v1 > 0 ? '+100%' : '0%') : `${sign}${pct.toFixed(1)}%`;

            grid.innerHTML += `
                <div class="compare-card">
                    <div class="compare-metric">${obj.label}</div>
                    <div class="compare-values">
                        <div class="compare-val">
                            <div class="number">${obj.f(v1)}</div>
                            <div class="label">P1</div>
                        </div>
                        <div class="compare-change ${state}">${pctStr}</div>
                        <div class="compare-val">
                            <div class="number">${obj.f(v2)}</div>
                            <div class="label">P2</div>
                        </div>
                    </div>
                </div>
            `;
        });
    } catch { }
}

export function setupComparisonEvents() {
    document.getElementById('btn-compare').addEventListener('click', loadComparison);
}

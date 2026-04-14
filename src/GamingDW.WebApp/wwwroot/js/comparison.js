import { fmt, money } from './utils.js';

export function setupComparisonEvents() {
    document.getElementById('btn-compare').addEventListener('click', async () => {
        const from1 = document.getElementById('cmp-from1').value;
        const to1 = document.getElementById('cmp-to1').value;
        const from2 = document.getElementById('cmp-from2').value;
        const to2 = document.getElementById('cmp-to2').value;
        if (!from1 || !to1 || !from2 || !to2) { alert('Please select both date ranges'); return; }

        try {
            const res = await fetch(`/api/reports/compare?from1=${from1}&to1=${to1}&from2=${from2}&to2=${to2}`);
            if (!res.ok) {
                const err = await res.json();
                alert(err.error || 'Comparison failed');
                return;
            }
            const data = await res.json();
            const grid = document.getElementById('compare-grid');
            document.getElementById('compare-results').style.display = 'block';

            const metrics = [
                { key: 'registrations', label: 'Registrations', fmt: fmt },
                { key: 'ftds', label: 'FTDs', fmt: fmt },
                { key: 'deposits', label: 'Deposits', fmt: money },
                { key: 'withdrawals', label: 'Withdrawals', fmt: money },
                { key: 'ggr', label: 'GGR', fmt: money },
                { key: 'activePlayers', label: 'Avg Active Players', fmt: fmt },
                { key: 'sessions', label: 'Sessions', fmt: fmt },
                { key: 'bonusCost', label: 'Bonus Cost', fmt: money },
                { key: 'netRevenue', label: 'Net Revenue', fmt: money },
            ];

            grid.innerHTML = metrics.map(m => {
                const a = data.period1[m.key] || 0;
                const b = data.period2[m.key] || 0;
                const change = a !== 0 ? ((b - a) / Math.abs(a) * 100).toFixed(1) : (b !== 0 ? '∞' : '0.0');
                const cls = parseFloat(change) > 0 ? 'up' : parseFloat(change) < 0 ? 'down' : 'neutral';
                const arrow = cls === 'up' ? '↑' : cls === 'down' ? '↓' : '→';
                return `
                    <div class="compare-card">
                        <div class="compare-metric">${m.label}</div>
                        <div class="compare-values">
                            <div class="compare-val"><div class="label">Period A</div><div class="number">${m.fmt(a)}</div></div>
                            <span class="compare-change ${cls}">${arrow} ${change}%</span>
                            <div class="compare-val"><div class="label">Period B</div><div class="number">${m.fmt(b)}</div></div>
                        </div>
                    </div>
                `;
            }).join('');
        } catch (e) {
            alert('Failed to fetch comparison stats');
        }
    });
}

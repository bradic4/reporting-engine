import { fmt, money, charts, destroyChart } from './utils.js';
import { setLoaded } from './main.js';
import { apiGet } from './api.js';

export async function loadTrends() {
    const from = document.getElementById('trends-from').value;
    const to = document.getElementById('trends-to').value;
    let url = '/api/reports/daily';
    const params = [];
    if (from) params.push(`from=${from}`);
    if (to) params.push(`to=${to}`);
    // Always request up to a large number of pages so we get everything for plotting (or rely on not passing page so API can return defaults)
    if (params.length) url += '?' + params.join('&');

    try {
        const res = await apiGet(url);
        const data = res?.data || [];
        if (data.length === 0) {
            document.getElementById('trends-kpis').innerHTML = '<p style="color:var(--text-muted)">No reports found for this range. Add daily reports first!</p>';
            return;
        }
        // Reports API returns ordered desc by date, so we want to reverse for chronological charts
        const reversed = [...data].reverse();

        const totalReg = data.reduce((s, r) => s + r.registrations, 0);
        const totalFTD = data.reduce((s, r) => s + r.ftDs, 0);
        const totalDeposits = data.reduce((s, r) => s + r.deposits, 0);
        const totalGGR = data.reduce((s, r) => s + r.ggr, 0);
        const totalNet = data.reduce((s, r) => s + r.netRevenue, 0);

        document.getElementById('trends-kpis').innerHTML = `
            <div class="kpi-card blue"><div class="kpi-label">Total Registrations</div><div class="kpi-value">${fmt(totalReg)}</div></div>
            <div class="kpi-card green"><div class="kpi-label">Total FTDs</div><div class="kpi-value">${fmt(totalFTD)}</div></div>
            <div class="kpi-card orange"><div class="kpi-label">Total Deposits</div><div class="kpi-value">${money(totalDeposits)}</div></div>
            <div class="kpi-card purple"><div class="kpi-label">Total GGR</div><div class="kpi-value">${money(totalGGR)}</div></div>
            <div class="kpi-card red"><div class="kpi-label">Net Revenue</div><div class="kpi-value">${money(totalNet)}</div></div>
        `;

        const labels = reversed.map(r => r.date);

        destroyChart('trends-revenue');
        charts['trends-revenue'] = new Chart(document.getElementById('trends-revenue-chart'), {
            type: 'line',
            data: {
                labels,
                datasets: [
                    { label: 'Deposits', data: reversed.map(r => r.deposits), borderColor: '#34d399', backgroundColor: 'rgba(52,211,153,0.1)', fill: true, tension: 0.3 },
                    { label: 'GGR', data: reversed.map(r => r.ggr), borderColor: '#a78bfa', tension: 0.3 },
                    { label: 'Net Revenue', data: reversed.map(r => r.netRevenue), borderColor: '#4f8cff', borderDash: [5, 5], tension: 0.3 }
                ]
            },
            options: { responsive: true, plugins: { legend: { position: 'top' } }, scales: { y: { beginAtZero: false } } }
        });

        destroyChart('trends-users');
        charts['trends-users'] = new Chart(document.getElementById('trends-users-chart'), {
            type: 'bar',
            data: {
                labels,
                datasets: [
                    { label: 'Registrations', data: reversed.map(r => r.registrations), backgroundColor: 'rgba(79,140,255,0.6)', borderRadius: 4 },
                    { label: 'FTDs', data: reversed.map(r => r.ftDs), backgroundColor: 'rgba(52,211,153,0.6)', borderRadius: 4 }
                ]
            },
            options: { responsive: true, plugins: { legend: { position: 'top' } }, scales: { y: { beginAtZero: true } } }
        });

        destroyChart('trends-activity');
        charts['trends-activity'] = new Chart(document.getElementById('trends-activity-chart'), {
            type: 'line',
            data: {
                labels,
                datasets: [
                    { label: 'Sessions', data: reversed.map(r => r.sessions), borderColor: '#fb923c', tension: 0.3 },
                    { label: 'Active Players', data: reversed.map(r => r.activePlayers), borderColor: '#4f8cff', tension: 0.3 }
                ]
            },
            options: { responsive: true, plugins: { legend: { position: 'top' } }, scales: { y: { beginAtZero: true } } }
        });
    } catch { }
}

export function setupTrendsEvents() {
    document.getElementById('btn-filter-trends').addEventListener('click', () => {
        setLoaded('trends', false);
        loadTrends();
    });
}

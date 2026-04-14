import { fmt, money } from './utils.js';
import { apiGet } from './api.js';

export async function loadLive() {
    try {
        const data = await apiGet('/api/live/today');
        
        document.getElementById('live-metrics').innerHTML = `
            <div class="kpi-card purple"><div class="kpi-label">Active Sessions</div><div class="kpi-value">${fmt(data.sessions)}</div></div>
            <div class="kpi-card blue"><div class="kpi-label">Active Players</div><div class="kpi-value">${fmt(data.activePlayers)}</div></div>
            <div class="kpi-card green"><div class="kpi-label">Today's Deposits</div><div class="kpi-value">${money(data.deposits)}</div></div>
            <div class="kpi-card orange"><div class="kpi-label">Today's Withdrawals</div><div class="kpi-value">${money(data.withdrawals)}</div></div>
            <div class="kpi-card red"><div class="kpi-label">Live GGR</div><div class="kpi-value">${money(data.ggr)}</div></div>
        `;
        document.getElementById('live-updated').textContent = `Last auto-refresh: ${new Date().toLocaleTimeString()}`;
    } catch { }
}

export function setupLiveEvents() {
    document.getElementById('btn-refresh-live').addEventListener('click', loadLive);
    // Auto refresh every 30 seconds
    setInterval(() => {
        if (document.getElementById('tab-live').classList.contains('active')) {
            loadLive();
        }
    }, 30000);
}

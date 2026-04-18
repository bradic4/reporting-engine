import { fmt, money } from './utils.js';
import { apiGet } from './api.js';

let liveRefreshInterval = null;

export async function loadLive() {
    try {
        const data = await apiGet('/api/live/today');

        const metricsEl = document.getElementById('live-metrics');
        const updatedEl = document.getElementById('live-updated');

        if (metricsEl) {
            metricsEl.innerHTML = `
                <div class="kpi-card purple"><div class="kpi-label">Active Sessions</div><div class="kpi-value">${fmt(data.sessions)}</div></div>
                <div class="kpi-card blue"><div class="kpi-label">Active Players</div><div class="kpi-value">${fmt(data.activePlayers)}</div></div>
                <div class="kpi-card green"><div class="kpi-label">Today's Deposits</div><div class="kpi-value">${money(data.deposits)}</div></div>
                <div class="kpi-card orange"><div class="kpi-label">Today's Withdrawals</div><div class="kpi-value">${money(data.withdrawals)}</div></div>
                <div class="kpi-card red"><div class="kpi-label">Live GGR</div><div class="kpi-value">${money(data.ggr)}</div></div>
            `;
        }

        if (updatedEl) {
            updatedEl.textContent = `Last auto-refresh: ${new Date().toLocaleTimeString()}`;
        }
    } catch (err) {
        console.error('[Live] Failed to load live data:', err);
    }
}

export function setupLiveEvents() {
    const refreshBtn = document.getElementById('btn-refresh-live');

    if (refreshBtn) {
        refreshBtn.addEventListener('click', loadLive);
    }

    if (!liveRefreshInterval) {
        liveRefreshInterval = setInterval(() => {
            const liveTab = document.getElementById('tab-live');
            if (liveTab && liveTab.classList.contains('active')) {
                loadLive();
            }
        }, 30000);
    }
}
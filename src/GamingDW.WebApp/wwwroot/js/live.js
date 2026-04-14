import { fmt, money } from './utils.js';

let liveInterval = null;

export async function loadLive() {
    try {
        const res = await fetch('/api/live/today');
        if (!res.ok) throw new Error('Failed to fetch');
        const data = await res.json();
        const grid = document.getElementById('live-kpis');
        if (Object.keys(data).length === 0 || !data.timestamp) {
             grid.innerHTML = '<p style="color:var(--text-muted)">No live data for today yet</p>';
             return;
        }
        grid.innerHTML = `
            <div class="kpi-card blue"><div class="kpi-label">Sessions</div><div class="kpi-value">${fmt(data.sessions)}</div></div>
            <div class="kpi-card green"><div class="kpi-label">Active Players</div><div class="kpi-value">${fmt(data.activePlayers)}</div></div>
            <div class="kpi-card orange"><div class="kpi-label">Deposits</div><div class="kpi-value">${money(data.deposits)}</div></div>
            <div class="kpi-card red"><div class="kpi-label">Withdrawals</div><div class="kpi-value">${money(data.withdrawals)}</div></div>
            <div class="kpi-card purple"><div class="kpi-label">GGR</div><div class="kpi-value">${money(data.ggr)}</div></div>
            <div class="kpi-card blue"><div class="kpi-label">Bets</div><div class="kpi-value">${money(data.bets)}</div></div>
            <div class="kpi-card green"><div class="kpi-label">Wins</div><div class="kpi-value">${money(data.wins)}</div></div>
            <div class="kpi-card orange"><div class="kpi-label">Plays</div><div class="kpi-value">${fmt(data.plays)}</div></div>
        `;
        document.getElementById('live-timestamp').textContent = new Date(data.timestamp).toLocaleTimeString();
    } catch {
        document.getElementById('live-kpis').innerHTML = '<p style="color:var(--text-muted)">Unable to fetch live data</p>';
    }
}

export function startLiveRefresh() {
    if (liveInterval) clearInterval(liveInterval);
    liveInterval = setInterval(loadLive, 30000);
}

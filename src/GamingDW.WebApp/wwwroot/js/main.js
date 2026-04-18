import { loadStats } from './utils.js';
import { checkAuth, setupAuthEvents } from './auth.js';
import { loadReports, setupReportsEvents } from './reports.js';
import { loadComparison, setupComparisonEvents } from './comparison.js';
import { loadTargets, setupTargetsEvents } from './targets.js';
import { loadTrends, setupTrendsEvents } from './trends.js';
import { loadLive, setupLiveEvents } from './live.js';
import { loadStaff, setupStaffEvents } from './staff.js';

// ═══════════════════════════════════════
// TAB NAVIGATION & STATE
// ═══════════════════════════════════════
const tabs = document.querySelectorAll('.tab');
const panels = document.querySelectorAll('.panel');
const loaded = {};

export function setLoaded(tab, state) {
    loaded[tab] = state;
}

function activateTab(tabEl) {
    tabs.forEach(t => t.classList.remove('active'));
    panels.forEach(p => p.classList.remove('active'));
    tabEl.classList.add('active');

    const panel = document.getElementById(`panel-${tabEl.dataset.tab}`);
    if (panel) panel.classList.add('active');

    loadTab(tabEl.dataset.tab);
}

tabs.forEach(tab => {
    tab.addEventListener('click', () => activateTab(tab));
});

async function loadTab(name) {
    if (loaded[name]) return;
    loaded[name] = true;

    try {
        switch (name) {
            case 'reports':
                await loadReports();
                break;
            case 'compare':
                await loadComparison();
                break;
            case 'targets':
                await loadTargets();
                break;
            case 'trends':
                await loadTrends();
                break;
            case 'live':
                await loadLive();
                break;
            case 'admin':
                await loadStaff();
                break;
        }
    } catch (err) {
        console.error(`[Tab] Failed to load tab "${name}":`, err);
        loaded[name] = false;
    }
}

// ═══════════════════════════════════════
// INITIALIZATION
// ═══════════════════════════════════════
(async () => {
    try {
        const authed = await checkAuth();

        if (authed) {
            setupAuthEvents();
            setupReportsEvents();
            setupComparisonEvents();
            setupTargetsEvents();
            setupTrendsEvents();
            setupLiveEvents();
            setupStaffEvents();

            loadStats();
        }
    } catch (err) {
        console.error('[Main] Initialization error:', err);
        window.location.href = '/login.html';
    }
})();
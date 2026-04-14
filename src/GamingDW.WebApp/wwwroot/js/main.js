import { loadStats } from './utils.js';
import { checkAuth, setupAuthEvents } from './auth.js';
import { loadReports, setupReportsEvents } from './reports.js';
import { setupComparisonEvents } from './comparison.js';
import { loadTargets, setupTargetsEvents } from './targets.js';
import { loadTrends, setupTrendsEvents } from './trends.js';
import { loadLive, startLiveRefresh } from './live.js';
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

tabs.forEach(tab => {
    tab.addEventListener('click', () => {
        tabs.forEach(t => t.classList.remove('active'));
        panels.forEach(p => p.classList.remove('active'));
        tab.classList.add('active');
        const panel = document.getElementById(`panel-${tab.dataset.tab}`);
        if (panel) panel.classList.add('active');
        loadTab(tab.dataset.tab);
    });
});

async function loadTab(name) {
    if (loaded[name]) return;
    loaded[name] = true;
    switch (name) {
        case 'reports': await loadReports(); break;
        case 'targets': await loadTargets(); break;
        case 'trends': await loadTrends(); break;
        case 'live': await loadLive(); startLiveRefresh(); break;
        case 'admin': await loadStaff(); break;
    }
}

// ═══════════════════════════════════════
// INITIALIZATION
// ═══════════════════════════════════════
(async () => {
    if (await checkAuth()) {
        setupAuthEvents();
        setupReportsEvents();
        setupComparisonEvents();
        setupTargetsEvents();
        setupTrendsEvents();
        setupStaffEvents();
        loadStats();
    }
})();

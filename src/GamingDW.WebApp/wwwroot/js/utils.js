import { apiGet } from './api.js';

export function fmt(n) { return new Intl.NumberFormat().format(n); }
export function money(n) { return '$' + new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n); }
export function setText(sel, val) { const el = document.querySelector(sel); if (el) el.textContent = val; }

Chart.defaults.color = '#8b8fa3';
Chart.defaults.borderColor = 'rgba(42, 45, 62, 0.5)';
Chart.defaults.font.family = "'Inter', sans-serif";
export const charts = {};
export function destroyChart(id) { if (charts[id]) { charts[id].destroy(); delete charts[id]; } }

export async function loadStats() {
    try {
        const d = await apiGet('/api/stats');
        setText('#stat-reports .stat-value', fmt(d.reports));
        setText('#stat-targets .stat-value', fmt(d.targets));
        setText('#stat-staff .stat-value', fmt(d.staff));
    } catch { }
}

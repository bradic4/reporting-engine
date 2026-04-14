import { fmt, money, loadStats } from './utils.js';
import { setLoaded } from './main.js';
import { apiGet, apiPost, apiPut, apiDelete } from './api.js';
import { showToast, showConfirm } from './toast.js';

let allTargets = [];

export async function loadTargets() {
    try {
        allTargets = await apiGet('/api/targets') || [];
        renderTargetsTable();
    } catch { }
}

function renderTargetsTable() {
    const tbody = document.querySelector('#targets-table tbody');
    if (allTargets.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;color:var(--text-muted);padding:40px">No targets set yet</td></tr>';
        return;
    }
    tbody.innerHTML = allTargets.map(t => `
        <tr>
            <td>${t.period}</td>
            <td>${t.periodStart}</td>
            <td><strong>${t.metricName}</strong></td>
            <td>${fmt(t.targetValue)}</td>
            <td>${t.createdBy}</td>
            <td><button class="btn-delete" data-id="${t.id}">Delete</button></td>
        </tr>
    `).join('');
}

export function setupTargetsEvents() {
    document.getElementById('btn-check-progress').addEventListener('click', async () => {
        const date = document.getElementById('target-progress-date').value;
        if (!date) { showToast('Please select a date', 'warning'); return; }
        
        try {
            const data = await apiGet(`/api/targets/progress?date=${date}`);
            const grid = document.getElementById('target-progress');
            if (!data.progress || data.progress.length === 0) {
                grid.innerHTML = '<p style="color:var(--text-muted)">No targets set for this date</p>';
                return;
            }
            grid.innerHTML = data.progress.map(p => {
                const pct = Math.min(p.progressPct, 100);
                const cls = pct >= 80 ? 'good' : pct >= 50 ? 'mid' : 'low';
                const isMonetary = ['Deposits', 'Withdrawals', 'GGR', 'BonusCost', 'NetRevenue'].includes(p.metricName);
                const fmtFn = isMonetary ? money : fmt;
                return `
                    <div class="target-card">
                        <div class="metric-name">${p.metricName}</div>
                        <div class="values">
                            <span class="actual">${fmtFn(p.actual)}</span>
                            <span class="target-val">/ ${fmtFn(p.target)}</span>
                        </div>
                        <div class="progress-bar"><div class="progress-fill ${cls}" style="width:${pct}%"></div></div>
                        <div class="pct" style="color:var(--accent-${cls === 'good' ? 'green' : cls === 'mid' ? 'orange' : 'red'})">${p.progressPct}%</div>
                    </div>
                `;
            }).join('');
        } catch { }
    });

    const targetModal = document.getElementById('target-modal');
    const targetForm = document.getElementById('target-form');

    document.getElementById('btn-add-target').addEventListener('click', () => {
        document.getElementById('target-modal-title').textContent = 'Set KPI Target';
        document.getElementById('target-id').value = '';
        targetForm.reset();
        document.getElementById('target-start').value = new Date().toISOString().slice(0, 10);
        targetModal.style.display = 'flex';
    });
    document.getElementById('btn-cancel-target').addEventListener('click', () => targetModal.style.display = 'none');
    targetModal.addEventListener('click', e => { if (e.target === targetModal) targetModal.style.display = 'none'; });

    targetForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = document.getElementById('target-id').value;
        const body = {
            period: document.getElementById('target-period').value,
            periodStart: document.getElementById('target-start').value,
            metricName: document.getElementById('target-metric').value,
            targetValue: parseFloat(document.getElementById('target-value').value) || 0,
        };
        const url = id ? `/api/targets/${id}` : '/api/targets';
        
        try {
            if (id) await apiPut(url, body);
            else await apiPost(url, body);
            
            targetModal.style.display = 'none';
            showToast('Target saved successfully', 'success');
            setLoaded('targets', false);
            loadTargets();
            loadStats();
        } catch { }
    });

    document.querySelector('#targets-table').addEventListener('click', async e => {
        if (e.target.classList.contains('btn-delete')) {
            const id = parseInt(e.target.dataset.id);
            if (!await showConfirm('Delete this target?')) return;
            try {
                await apiDelete(`/api/targets/${id}`);
                showToast('Target deleted', 'success');
                setLoaded('targets', false);
                loadTargets();
                loadStats();
            } catch { }
        }
    });
}

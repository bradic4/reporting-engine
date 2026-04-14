import { fmt, money, loadStats } from './utils.js';
import { currentUser } from './auth.js';
import { setLoaded } from './main.js';

export let allReports = [];

export async function loadReports() {
    const from = document.getElementById('reports-from').value;
    const to = document.getElementById('reports-to').value;
    let url = '/api/reports/daily';
    const params = [];
    if (from) params.push(`from=${from}`);
    if (to) params.push(`to=${to}`);
    if (params.length) url += '?' + params.join('&');

    allReports = await (await fetch(url)).json();
    renderReportsTable();
}

function renderReportsTable() {
    const canEdit = currentUser?.permissions?.editReports;
    const tbody = document.querySelector('#reports-table tbody');
    if (allReports.length === 0) {
        tbody.innerHTML = '<tr><td colspan="12" style="text-align:center;color:var(--text-muted);padding:40px">No reports found. Add your first daily report!</td></tr>';
        return;
    }
    tbody.innerHTML = allReports.map(r => `
        <tr>
            <td><strong>${r.date}</strong></td>
            <td>${fmt(r.registrations)}</td>
            <td>${fmt(r.ftDs)}</td>
            <td class="positive">${money(r.deposits)}</td>
            <td class="negative">${money(r.withdrawals)}</td>
            <td>${money(r.ggr)}</td>
            <td>${fmt(r.activePlayers)}</td>
            <td>${fmt(r.sessions)}</td>
            <td>${money(r.bonusCost)}</td>
            <td>${money(r.netRevenue)}</td>
            <td class="notes-cell" title="${r.notes || ''}">${r.notes || '—'}</td>
            <td>
                ${canEdit ? `<button class="btn-edit" data-id="${r.id}">Edit</button>
                <button class="btn-delete" data-id="${r.id}">Del</button>` : ''}
            </td>
        </tr>
    `).join('');
}

export function setupReportsEvents() {
    document.getElementById('btn-filter-reports').addEventListener('click', () => {
        setLoaded('reports', false);
        loadReports();
    });

    const reportModal = document.getElementById('report-modal');
    const reportForm = document.getElementById('report-form');

    document.getElementById('btn-add-report').addEventListener('click', () => {
        document.getElementById('report-modal-title').textContent = 'Add Daily Report';
        document.getElementById('report-id').value = '';
        reportForm.reset();
        document.getElementById('report-date').value = new Date().toISOString().slice(0, 10);
        reportModal.style.display = 'flex';
    });

    document.getElementById('btn-cancel-report').addEventListener('click', () => reportModal.style.display = 'none');
    reportModal.addEventListener('click', e => { if (e.target === reportModal) reportModal.style.display = 'none'; });

    document.querySelector('#reports-table').addEventListener('click', e => {
        if (e.target.classList.contains('btn-edit')) {
            editReport(parseInt(e.target.dataset.id));
        } else if (e.target.classList.contains('btn-delete')) {
            deleteReport(parseInt(e.target.dataset.id));
        }
    });

    async function deleteReport(id) {
        if (!confirm('Delete this report?')) return;
        await fetch(`/api/reports/daily/${id}`, { method: 'DELETE' });
        setLoaded('reports', false);
        loadReports();
        loadStats();
    }

    function editReport(id) {
        const r = allReports.find(x => x.id === id);
        if (!r) return;
        document.getElementById('report-modal-title').textContent = 'Edit Report';
        document.getElementById('report-id').value = r.id;
        document.getElementById('report-date').value = r.date;
        document.getElementById('report-reg').value = r.registrations;
        document.getElementById('report-ftd').value = r.ftDs;
        document.getElementById('report-deposits').value = r.deposits;
        document.getElementById('report-withdrawals').value = r.withdrawals;
        document.getElementById('report-ggr').value = r.ggr;
        document.getElementById('report-active').value = r.activePlayers;
        document.getElementById('report-sessions').value = r.sessions;
        document.getElementById('report-bonus').value = r.bonusCost;
        document.getElementById('report-notes').value = r.notes || '';
        reportModal.style.display = 'flex';
    }

    reportForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = document.getElementById('report-id').value;
        const body = {
            date: document.getElementById('report-date').value,
            registrations: parseInt(document.getElementById('report-reg').value) || 0,
            ftDs: parseInt(document.getElementById('report-ftd').value) || 0,
            deposits: parseFloat(document.getElementById('report-deposits').value) || 0,
            withdrawals: parseFloat(document.getElementById('report-withdrawals').value) || 0,
            ggr: parseFloat(document.getElementById('report-ggr').value) || 0,
            activePlayers: parseInt(document.getElementById('report-active').value) || 0,
            sessions: parseInt(document.getElementById('report-sessions').value) || 0,
            bonusCost: parseFloat(document.getElementById('report-bonus').value) || 0,
            notes: document.getElementById('report-notes').value || null,
        };
        const url = id ? `/api/reports/daily/${id}` : '/api/reports/daily';
        const method = id ? 'PUT' : 'POST';
        const res = await fetch(url, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
        if (res.ok) {
            reportModal.style.display = 'none';
            setLoaded('reports', false);
            loadReports();
            loadStats();
        } else {
            const err = await res.json();
            alert(err.error || 'Error saving report');
        }
    });

    document.getElementById('btn-upload-report').addEventListener('click', () => {
        document.getElementById('upload-file').click();
    });

    document.getElementById('upload-file').addEventListener('change', async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        const uploadBtn = document.getElementById('btn-upload-report');
        const origText = uploadBtn.textContent;
        uploadBtn.textContent = '⏳ Uploading...';
        uploadBtn.disabled = true;

        const formData = new FormData();
        formData.append('file', file);

        try {
            const res = await fetch('/api/reports/upload', { method: 'POST', body: formData });
            const data = await res.json();

            if (res.ok) {
                let msg = `✅ Imported ${data.imported} report(s)`;
                if (data.skipped > 0) msg += `, skipped ${data.skipped} duplicate(s)`;
                if (data.errors && data.errors.length > 0) msg += `\n⚠️ Errors:\n` + data.errors.join('\n');
                alert(msg);
                setLoaded('reports', false);
                loadReports();
                loadStats();
            } else {
                alert(data.error || 'Upload failed');
            }
        } catch (err) {
            alert('Upload error: ' + err.message);
        } finally {
            uploadBtn.textContent = origText;
            uploadBtn.disabled = false;
            e.target.value = '';
        }
    });
}

import { loadStats } from './utils.js';
import { setLoaded } from './main.js';
import { apiGet, apiPost, apiPut } from './api.js';
import { showToast } from './toast.js';

let allStaff = [];

export async function loadStaff() {
    try {
        allStaff = await apiGet('/api/staff') || [];
        const tbody = document.querySelector('#staff-table tbody');
        tbody.innerHTML = allStaff.map(s => `
            <tr>
                <td><strong>${s.username}</strong></td>
                <td>${s.title}</td>
                <td>${badge(s.canViewReports)}</td>
                <td>${badge(s.canEditReports)}</td>
                <td>${badge(s.canSetTargets)}</td>
                <td>${badge(s.canViewLive)}</td>
                <td>${badge(s.canManageStaff)}</td>
                <td>${badge(s.isActive)}</td>
                <td><button class="btn-edit" data-id="${s.id}">Edit</button></td>
            </tr>
        `).join('');
    } catch { }
}

function badge(val) {
    return val ? '<span class="perm-badge yes">✓</span>' : '<span class="perm-badge no">✗</span>';
}

export function setupStaffEvents() {
    const staffModal = document.getElementById('staff-modal');
    const staffForm = document.getElementById('staff-form');

    document.getElementById('btn-add-staff').addEventListener('click', () => {
        document.getElementById('modal-title').textContent = 'Add Staff User';
        document.getElementById('staff-id').value = '';
        staffForm.reset();
        document.getElementById('perm-active').checked = true;
        document.getElementById('perm-view').checked = true;
        document.getElementById('staff-password').required = true;
        document.getElementById('staff-password').placeholder = '';
        staffModal.style.display = 'flex';
    });
    
    document.getElementById('btn-cancel-modal').addEventListener('click', () => staffModal.style.display = 'none');
    staffModal.addEventListener('click', e => { if (e.target === staffModal) staffModal.style.display = 'none'; });

    document.querySelector('#staff-table').addEventListener('click', e => {
        if (e.target.classList.contains('btn-edit')) {
            editStaff(parseInt(e.target.dataset.id));
        }
    });

    function editStaff(id) {
        const s = allStaff.find(u => u.id === id);
        if (!s) return;
        document.getElementById('modal-title').textContent = 'Edit Staff User';
        document.getElementById('staff-id').value = s.id;
        document.getElementById('staff-username').value = s.username;
        document.getElementById('staff-password').value = '';
        document.getElementById('staff-password').required = false;
        document.getElementById('staff-password').placeholder = 'Leave blank to keep current';
        document.getElementById('staff-title').value = s.title;
        document.getElementById('perm-view').checked = s.canViewReports;
        document.getElementById('perm-edit').checked = s.canEditReports;
        document.getElementById('perm-targets').checked = s.canSetTargets;
        document.getElementById('perm-live').checked = s.canViewLive;
        document.getElementById('perm-manage').checked = s.canManageStaff;
        document.getElementById('perm-active').checked = s.isActive;
        staffModal.style.display = 'flex';
    }

    staffForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = document.getElementById('staff-id').value;
        const body = {
            username: document.getElementById('staff-username').value,
            password: document.getElementById('staff-password').value || null,
            title: document.getElementById('staff-title').value,
            canViewReports: document.getElementById('perm-view').checked,
            canEditReports: document.getElementById('perm-edit').checked,
            canSetTargets: document.getElementById('perm-targets').checked,
            canViewLive: document.getElementById('perm-live').checked,
            canManageStaff: document.getElementById('perm-manage').checked,
            isActive: document.getElementById('perm-active').checked,
        };
        const url = id ? `/api/staff/${id}` : '/api/staff';
        
        try {
            if (id) await apiPut(url, body);
            else await apiPost(url, body);
            
            staffModal.style.display = 'none';
            showToast('Staff saved successfully', 'success');
            setLoaded('admin', false);
            loadStaff();
            loadStats();
        } catch { }
    });
}

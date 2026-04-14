import { apiGet, apiPost } from './api.js';

export let currentUser = null;

export async function checkAuth() {
    try {
        currentUser = await apiGet('/api/auth/me');
        if (!currentUser) { window.location.href = '/login.html'; return false; }
        document.getElementById('user-name').textContent = currentUser.username;
        document.getElementById('user-title').textContent = currentUser.title;
        applyPermissions();
        return true;
    } catch { window.location.href = '/login.html'; return false; }
}

function applyPermissions() {
    const p = currentUser.permissions;
    const permMap = {
        viewReports: p.viewReports, editReports: p.editReports,
        setTargets: p.setTargets, viewLive: p.viewLive, manageStaff: p.manageStaff
    };
    let firstVisible = null;
    document.querySelectorAll('.tab[data-perm]').forEach(tab => {
        if (permMap[tab.dataset.perm]) {
            tab.style.display = '';
            if (!firstVisible) firstVisible = tab;
        } else {
            tab.style.display = 'none';
        }
    });

    const addBtn = document.getElementById('btn-add-report');
    const uploadBtn = document.getElementById('btn-upload-report');
    if (addBtn) addBtn.style.display = p.editReports ? '' : 'none';
    if (uploadBtn) uploadBtn.style.display = p.editReports ? '' : 'none';

    if (firstVisible) firstVisible.click();
}

export function setupAuthEvents() {
    document.getElementById('btn-logout').addEventListener('click', async () => {
        await apiPost('/api/auth/logout');
        window.location.href = '/login.html';
    });
}

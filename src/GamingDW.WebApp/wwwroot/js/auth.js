import { apiGet, apiPost } from './api.js';

export let currentUser = null;

export async function checkAuth() {
    try {
        const data = await apiGet('/api/auth/me');
        if (!data) {
            window.location.href = '/login.html';
            return false;
        }
        currentUser = data;

        // Update UI — safely handle missing elements
        const nameEl = document.getElementById('user-name');
        const titleEl = document.getElementById('user-title');
        const avatarEl = document.getElementById('user-avatar');

        if (nameEl) nameEl.textContent = currentUser.username;
        if (titleEl) titleEl.textContent = currentUser.title || '';
        if (avatarEl) {
            // Show first letter of username as avatar
            avatarEl.textContent = (currentUser.username || '?')[0].toUpperCase();
        }

        applyPermissions();
        return true;
    } catch (err) {
        // Only redirect on auth failure — don't redirect on network errors
        // (api.js already handles 401 redirect)
        if (err?.message !== 'Unauthorized') {
            console.error('[Auth] checkAuth failed:', err);
        }
        return false;
    }
}

function applyPermissions() {
    const p = currentUser?.permissions || {};
    const permMap = {
        viewReports: p.viewReports,
        editReports: p.editReports,
        setTargets: p.setTargets,
        viewLive: p.viewLive,
        manageStaff: p.manageStaff
    };

    let firstVisible = null;
    document.querySelectorAll('.tab[data-perm]').forEach(tab => {
        const perm = tab.dataset.perm;
        const hasAccess = permMap[perm];
        tab.style.display = hasAccess ? '' : 'none';
        if (hasAccess && !firstVisible) {
            firstVisible = tab;
        }
    });

    // Show/hide edit buttons based on permissions
    const addBtn = document.getElementById('btn-add-report');
    const uploadBtn = document.getElementById('btn-upload-report');
    if (addBtn) addBtn.style.display = p.editReports ? '' : 'none';
    if (uploadBtn) uploadBtn.style.display = p.editReports ? '' : 'none';

    // Activate first accessible tab
    if (firstVisible) {
        firstVisible.click();
    }
}

export function setupAuthEvents() {
    const logoutBtn = document.getElementById('btn-logout');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', async () => {
            try {
                await apiPost('/api/auth/logout');
            } catch { /* ignore */ }
            window.location.href = '/login.html';
        });
    }
}

/**
 * Toast notification system.
 * Replaces alert() with non-blocking, auto-dismissing notifications.
 */

let container = null;

function ensureContainer() {
    if (container) return container;
    container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }
    return container;
}

/**
 * Show a toast notification.
 * @param {string} message - The message to display
 * @param {'success'|'error'|'warning'|'info'} type - Toast type
 * @param {number} duration - Auto-dismiss duration in ms (0 = no auto-dismiss)
 */
export function showToast(message, type = 'info', duration = 4000) {
    const c = ensureContainer();

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;

    const icons = { success: '✓', error: '✕', warning: '⚠', info: 'ℹ' };
    toast.innerHTML = `
        <span class="toast-icon">${icons[type] || icons.info}</span>
        <span class="toast-message">${escapeHtml(message)}</span>
        <button class="toast-close" aria-label="Close">×</button>
    `;

    toast.querySelector('.toast-close').addEventListener('click', () => dismissToast(toast));
    c.appendChild(toast);

    // Trigger enter animation
    requestAnimationFrame(() => toast.classList.add('toast-visible'));

    if (duration > 0) {
        setTimeout(() => dismissToast(toast), duration);
    }

    return toast;
}

function dismissToast(toast) {
    toast.classList.remove('toast-visible');
    toast.classList.add('toast-exit');
    toast.addEventListener('transitionend', () => toast.remove(), { once: true });
    // Fallback removal if transition doesn't fire
    setTimeout(() => { if (toast.parentNode) toast.remove(); }, 500);
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

/**
 * Show a confirmation modal replacing confirm().
 * Returns a Promise<boolean>.
 */
export function showConfirm(message, title = 'Confirm') {
    return new Promise((resolve) => {
        const overlay = document.createElement('div');
        overlay.className = 'modal-overlay confirm-overlay';
        overlay.style.display = 'flex';
        overlay.innerHTML = `
            <div class="modal confirm-modal">
                <h3>${escapeHtml(title)}</h3>
                <p>${escapeHtml(message)}</p>
                <div class="modal-actions">
                    <button class="btn-cancel" id="confirm-no">Cancel</button>
                    <button class="btn-save btn-danger" id="confirm-yes">Confirm</button>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);

        overlay.querySelector('#confirm-yes').addEventListener('click', () => {
            overlay.remove();
            resolve(true);
        });
        overlay.querySelector('#confirm-no').addEventListener('click', () => {
            overlay.remove();
            resolve(false);
        });
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) { overlay.remove(); resolve(false); }
        });
    });
}

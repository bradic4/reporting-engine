import { showToast } from './toast.js';

/**
 * Centralized API wrapper for all fetch requests.
 * Handles errors (401, 403, 500), JSON parsing, and headers.
 */
export async function api(url, options = {}) {
    try {
        const res = await fetch(url, {
            credentials: 'same-origin',
            ...options,
            headers: {
                ...(options.body && !(options.body instanceof FormData) ? { 'Content-Type': 'application/json' } : {}),
                ...options.headers,
            },
        });

        if (res.status === 401) {
            // Avoid redirect loop if we're already on the login page
            if (!window.location.pathname.endsWith('/login.html')) {
                window.location.href = '/login.html';
            }
            throw new Error('Unauthorized');
        }

        if (res.status === 403) {
            showToast('Access denied — insufficient permissions', 'error');
            throw new Error('Forbidden');
        }

        if (res.status === 429) {
            showToast('Too many requests — please slow down', 'warning');
            throw new Error('Rate limited');
        }

        if (!res.ok) {
            const err = await res.json().catch(() => ({ error: `Request failed (${res.status})` }));
            const msg = err.error || `Request failed (${res.status})`;
            showToast(msg, 'error');
            throw new ApiError(msg, res.status, err);
        }

        // Some endpoints return 200 with no body
        const text = await res.text();
        return text ? JSON.parse(text) : null;
    } catch (err) {
        if (err instanceof ApiError) throw err;
        if (err.message === 'Unauthorized' || err.message === 'Forbidden' || err.message === 'Rate limited') throw err;
        showToast('Network error — check your connection', 'error');
        throw err;
    }
}

/**
 * Convenience for GET requests.
 */
export function apiGet(url) {
    return api(url);
}

/**
 * Convenience for POST with JSON body.
 */
export function apiPost(url, body) {
    return api(url, { method: 'POST', body: JSON.stringify(body) });
}

/**
 * Convenience for PUT with JSON body.
 */
export function apiPut(url, body) {
    return api(url, { method: 'PUT', body: JSON.stringify(body) });
}

/**
 * Convenience for DELETE.
 */
export function apiDelete(url) {
    return api(url, { method: 'DELETE' });
}

/**
 * POST with FormData (for file uploads).
 */
export function apiUpload(url, formData) {
    return api(url, { method: 'POST', body: formData });
}

/**
 * Custom error class for API responses.
 */
export class ApiError extends Error {
    constructor(message, status, data) {
        super(message);
        this.status = status;
        this.data = data;
    }
}

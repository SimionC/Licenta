// src/hooks/useAuth.js
import { useState, useEffect } from 'react';

/**
 * Purpose: Minimal auth hook that verifies active session and exposes { user, loading }.
 * API touched: GET /api/auth/me with credentials included.
 * Output contract: user=null means unauthenticated; loading gates route rendering.
 */

export function useAuth() {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetch('/api/Auth/Me', { credentials: 'include' })
            .then(res => {
                if (!res.ok) throw new Error('Not logged in');
                return res.json();
            })
            .then(data => setUser(data))
            .catch(() => setUser(null))
            .finally(() => setLoading(false));
    }, []);

    return { user, loading };
}

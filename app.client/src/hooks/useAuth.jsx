import { useState, useEffect } from 'react';

/**
 * Purpose: Minimal auth hook that verifies active session and exposes { user, loading }.
 * Output user=null means unauthenticated; loading gates route rendering.
 */

export function useAuth() {
    const [user, setUser] = useState(null);         //useState stores component/hook
    const [loading, setLoading] = useState(true);

    useEffect(() => {                               //useEffect runs when the component loads
        fetch('/api/Auth/Me', { credentials: 'include' }) //end cookies together with this request - cookie authentification
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

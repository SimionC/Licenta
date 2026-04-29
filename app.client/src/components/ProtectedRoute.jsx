import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'  // check `user`

/**
 * Purpose: Route gate for private pages.
 * Guards: waits for auth check; redirects when user is missing.
 * Navigation side effect: sends unauthenticated users to login with returnUrl.
 */

export default function ProtectedRoute({ children }) {
    const { user, loading } = useAuth()
    const location = useLocation()

    if (loading) {
        return <div>Loading…</div>
    }

    if (!user) {
        // not logged in → send to login, carry along returnUrl
        return (
            <Navigate
                to={`/?returnUrl=${encodeURIComponent(location.pathname)}`}
                replace
            />
        )
    }

    return children
}

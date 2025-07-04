import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'  // check `user`

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
                to={`/account/login?returnUrl=${encodeURIComponent(location.pathname)}`}
                replace
            />
        )
    }

    return children
}

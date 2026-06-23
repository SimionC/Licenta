import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

/**
 * Purpose: Route gate for private pages.
 * Guards: waits for auth check; redirects missing users to login and forced-password users to /change-password.
 */
export default function ProtectedRoute({ children }) {
    const { user, loading } = useAuth();
    const location = useLocation();

    if (loading) {
        return <div>Loading...</div>;
    }

    if (!user) {
        return (
            <Navigate
                to={`/?returnUrl=${encodeURIComponent(location.pathname)}`}
                replace
            />
        );
    }

    if (user.mustChangePassword) {
        return <Navigate to="/change-password" replace />;
    }

    return children;
}

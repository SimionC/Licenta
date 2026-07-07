import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

/**
 * Purpose: guard for private pages in the frontend, for backend [Authorize] prevents unauthorized API access even if someone manually calls the endpoint.
 * Guards: waits for auth check; redirects missing users to login and forced-password users to /change-password.
 */
export default function ProtectedRoute({ children }) { //whatever page is wrapped inside ProtectedRoute
    // -------------------------
    // AUTH CHECK
    // -------------------------
    const { user, loading } = useAuth();
    const location = useLocation();

    // Wait until /api/Auth/Me finishes before deciding where to send the user.
    if (loading) {
        return <div>Loading...</div>;
    }

    // Missing session: go back to login and remember the original page.
    if (!user) {
        return (
            <Navigate
                to={`/?returnUrl=${encodeURIComponent(location.pathname)}`}
                replace
            />
        );
    }

    // Forced password-change accounts cannot open normal private pages yet.
    if (user.mustChangePassword) {
        return <Navigate to="/change-password" replace />;
    }

    // Authenticated and allowed: render the protected page.
    return children;
}

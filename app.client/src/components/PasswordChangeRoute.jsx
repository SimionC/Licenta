import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

/**
* Purpose: only logged-in users who are required to change their password can access /change-password
*/
export default function PasswordChangeRoute({ children }) {
    // -------------------------
    // PASSWORD-CHANGE ROUTE GUARD
    // -------------------------
    const { user, loading } = useAuth();

    if (loading) {
        return <div>Loading...</div>;
    }

    if (!user) {
        return <Navigate to="/" replace />;
    }

    if (!user.mustChangePassword) {
        return <Navigate to="/dashboard" replace />;
    }

    // Only users with mustChangePassword=true reach the actual page.
    return children;
}

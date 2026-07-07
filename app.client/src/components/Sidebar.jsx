import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { BookOpen, FileText, LayoutDashboard, LogOut, UserRound, Users } from 'lucide-react'; //icon
import { useAuth } from '../hooks/useAuth';
import './Sidebar.css';

/**
 * Purpose: global navigation shell for dashboard, courses, notes, and admin accounts.
 */

const Sidebar = () => {
    // -------------------------
    // AUTH AND DISPLAY STATE
    // -------------------------
    const { user } = useAuth();
    const navigate = useNavigate();
    const isAdmin = user?.userType === 'admin';
    const displayName = user ? `${user.name || ''} ${user.lastName || ''}`.trim() : 'Account';

    // Logs out on the backend, clears cached identity, then returns to login.
    const handleLogout = async () => {
        try {
            await fetch('/api/Auth/logout', {
                method: 'POST',
                credentials: 'include'
            });
        } finally {
            localStorage.removeItem('userEmail');
            localStorage.removeItem('userType');
            navigate('/');
        }
    };

    return (
        <div className="sidebar">
            {/* Brand block at the top of the sidebar. */}
            <div className="logo">
                <div className="logo-circle">O</div>
                <div>
                    <strong>O.M.L.</strong><br />
                    <small>Learning Platform</small>
                </div>
            </div>

            {/* Main navigation links. Admin-only pages are hidden for non-admin users. */}
            <nav className="menu">
                <NavLink to="/dashboard" className="menu-item">
                    <LayoutDashboard size={18} />
                    <span>Dashboard</span>
                </NavLink>

                <NavLink to="/courses/all" className="menu-item">
                    <BookOpen size={18} />
                    <span>Courses</span>
                </NavLink>

                <NavLink to="/notes" className="menu-item">
                    <FileText size={18} />
                    <span>Notes</span>
                </NavLink>

                {isAdmin && (
                    <NavLink to="/accounts" className="menu-item">
                        <Users size={18} />
                        <span>Accounts</span>
                    </NavLink>
                )}
            </nav>

            {/* Current account shortcut and logout action at the bottom. */}
            <div className="sidebar-user-actions">
                <NavLink to="/account" className="menu-item sidebar-account-link">
                    <UserRound size={18} />
                    <span>
                        <strong>{displayName || 'Account'}</strong>
                        <small>{user?.userType || 'Profile'}</small>
                    </span>
                </NavLink>

                <button type="button" className="menu-item sidebar-logout-btn" onClick={handleLogout}>
                    <LogOut size={18} />
                    <span>Logout</span>
                </button>
            </div>
        </div>
    );
};

export default Sidebar;

import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { BookOpen, FileText, LayoutDashboard, LogOut, UserRound, Users } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import './Sidebar.css';

/**
 * Purpose: global navigation shell for dashboard, courses, notes, and admin accounts.
 * Contract: route links are hardcoded and should mirror App route map.
 */

const Sidebar = () => {
    const { user } = useAuth();
    const navigate = useNavigate();
    const isAdmin = user?.userType === 'admin';
    const displayName = user ? `${user.name || ''} ${user.lastName || ''}`.trim() : 'Account';

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
            <div className="logo">
                <div className="logo-circle">O</div>
                <div>
                    <strong>O.M.L.</strong><br />
                    <small>Learning Platform</small>
                </div>
            </div>

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

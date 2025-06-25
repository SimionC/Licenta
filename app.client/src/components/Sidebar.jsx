import React from 'react';
import { NavLink } from 'react-router-dom';
import { BookOpen, LayoutDashboard, FileText } from 'lucide-react';
import './Sidebar.css';

const Sidebar = () => {
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
            </nav>
        </div>
    );
};

export default Sidebar;

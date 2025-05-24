import React from 'react';
import { NavLink } from 'react-router-dom';
import './Sidebar.css';

const Sidebar = () => {
    return (
        <div className="sidebar">
            <div className="logo">O.M.L</div>

            <nav className="menu">
                <NavLink to="/dashboard" className="menu-item">Dashboard</NavLink>

                <NavLink to="/courses/all" className="menu-item">Courses</NavLink>

                <NavLink to="/notes" className="menu-item">Notes</NavLink>
                <NavLink to="/settings" className="menu-item">Settings</NavLink>
            </nav>

            <div className="profile">
                <img src="/path-to-avatar.jpg" alt="User" />
                <div className="info">
                    <p>Jenny Wilson</p>
                    <small>jennywils@gmail.com</small>
                </div>
            </div>
        </div>
    );
};

export default Sidebar;

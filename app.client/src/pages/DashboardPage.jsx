import React from 'react';
import Sidebar from '../components/Sidebar';

const DashboardPage = () => {
    return (
        <div style={{ display: 'flex' }}>
            <Sidebar />
            <div style={{ marginLeft: '250px', padding: '2rem', width: '100%' }}>
                <h1>Welcome to your Dashboard</h1>
                {/* Other content */}
            </div>
        </div>
    );
};

export default DashboardPage;

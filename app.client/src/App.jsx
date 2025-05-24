import React from 'react';
import { Routes, Route } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import AllCoursesPage from './pages/AllCoursesPage';
import 'bootstrap/dist/css/bootstrap.min.css';


function App() {
    return (
        <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/courses/all" element={<AllCoursesPage />} />
            {/* Add a home or default route later */}
        </Routes>
    );
}

export default App;

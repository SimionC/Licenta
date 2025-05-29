import React, { useEffect, useState } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import AllCoursesPage from './pages/AllCoursesPage';
import CoursePage from './pages/CoursePage';
import 'bootstrap/dist/css/bootstrap.min.css';
import axios from 'axios';


function App() {

    const [userType, setUserType] = useState("");

    useEffect(() => {
        fetch("/api/Auth/Me", {credentials: "include" })
            .then((res) => {
                if (!res.ok) throw new Error("Not logged in");
                return res.json();
            })
            .then((data) => {
                setUserType(data.userType); // "teacher" or "student"
                console.log("Logged in as:", data.userType);
            })
            .catch(() => {
                setUserType(""); // or redirect to login
            });
        axios.get("/api/Auth/Me", { withCredentials: true })
            .then(res => {
                localStorage.setItem("userEmail", res.data.email); // ✅ this is key
                localStorage.setItem("userType", res.data.userType); // (optional)
            })
            .catch(() => {
                localStorage.removeItem("userEmail"); // clear on failure
            });
    }, []);



    return (
        <Routes>
            <Route path="/" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/courses/all" element={<AllCoursesPage userType={userType} />} />
            <Route path="/courses/:courseId" element={<CoursePage />} />
            {/* Add a home or default route later */}
        </Routes>
    );
}

export default App;

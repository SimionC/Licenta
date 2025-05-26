import React, { useEffect, useState } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import AllCoursesPage from './pages/AllCoursesPage';
import 'bootstrap/dist/css/bootstrap.min.css';


function App() {

    const [userType, setUserType] = useState("");

    useEffect(() => {
        fetch("https://localhost:7166/Auth/Me", {credentials: "include" })
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
          /*  .then(data => {
                console.log("Me endpoint returned:", data);
                setUserType(data.userType); // <- 'teacher' or 'student'
            })
            .catch(err => {
                console.error("Failed to fetch user info", err);
            });*/
    }, []);



    return (
        <Routes>
            <Route path="/" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/courses/all" element={<AllCoursesPage userType={userType} />} />
            {/* Add a home or default route later */}
        </Routes>
    );
}

export default App;

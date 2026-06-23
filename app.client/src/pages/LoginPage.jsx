import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import 'bootstrap/dist/css/bootstrap.min.css';
import "../App.css";

/**
 * Purpose: Sign-in form and session bootstrap for dashboard access.
 * API touched: POST /api/Auth/Login, GET /api/Auth/Me.
 * Side effects: stores userEmail/userType in localStorage, then navigates to /dashboard.
 */

const LoginPage = () => {
    const navigate = useNavigate();

    const [formData, setFormData] = useState({
        email: '',
        password: '',
    });

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    // handleSubmit: validates login flow, hydrates profile, persists local identity cache.
    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            const res = await fetch('/api/Auth/Login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(formData),
            });

            if (res.ok) {
                // After login, fetch user info
                const meRes = await fetch('/api/Auth/Me', {
                    credentials: 'include'
                });
                if (meRes.ok) {
                    const me = await meRes.json();
                    localStorage.setItem("userEmail", me.email);
                    localStorage.setItem("userType", me.userType); // ✅ store userType
                    if (me.mustChangePassword) {
                        navigate('/change-password');
                        return;
                    }
                }
                // respect optional returnUrl
                const params = new URLSearchParams(window.location.search);
                const returnUrl = params.get('returnUrl');
                navigate(returnUrl || '/dashboard');
            } else {
                alert('Login failed');
            }
        } catch (err) {
            console.error(err);
            alert('An error occurred');
        }
    };


    useEffect(() => {
        let mounted = true;
        axios.get("/api/Auth/Me", { withCredentials: true })
            .then(res => {
                if (!mounted) return;
                localStorage.setItem("userEmail", res.data.email); // store email for later
            })
            .catch(() => {
                // ignore; no active session
            });
        return () => { mounted = false };
    }, []);

    return (
        <div className="container mt-5">
            <div className="row justify-content-center">
                <div className="col-md-6">
                    <div className="card p-4 shadow">
                        <h2 className="text-center mb-4">Login</h2>
                        <form onSubmit={handleSubmit}>
                            {/* Email Field */}
                            <div className="mb-3">
                                <label className="form-label">Email</label>
                                <input
                                    name="email"
                                    type="email"
                                    className="form-control"
                                    placeholder="Enter your email"
                                    onChange={handleChange}
                                    required
                                />
                            </div>

                            {/* Password Field */}
                            <div className="mb-3">
                                <label className="form-label">Password</label>
                                <input
                                    name="password"
                                    type="password"
                                    className="form-control"
                                    placeholder="Enter your password"
                                    onChange={handleChange}
                                    required
                                />
                            </div>

                            {/* Submit Button */}
                            <button type="submit" className="btn btn-green w-100">
                                Login
                            </button>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default LoginPage;

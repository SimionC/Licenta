import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import "../App.css";

const RegisterPage = () => {
    const [formData, setFormData] = useState({
        email: '',
        nume: '',
        prenume: '',
        password: '',
        studentId: '',
        userTypeId: 1,
    });

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            const res = await fetch('https://localhost:7166/Auth/Register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(formData),
            });

            if (res.ok) {
                alert('Registration successful');
            } else {
                alert('Registration failed');
            }
        } catch (err) {
            console.error(err);
            alert('An error occurred');
        }
    };

    return (
        <div className="container mt-5">
            <div className="row justify-content-center">
                <div className="col-md-6">
                    <div className="card p-4 shadow">
                        <h2 className="text-center mb-4">Register</h2>
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

                            {/* First Name */}
                            <div className="mb-3">
                                <label className="form-label">First Name</label>
                                <input
                                    name="nume"
                                    className="form-control"
                                    placeholder="Enter your first name"
                                    onChange={handleChange}
                                    required
                                />
                            </div>

                            {/* Last Name */}
                            <div className="mb-3">
                                <label className="form-label">Last Name</label>
                                <input
                                    name="prenume"
                                    className="form-control"
                                    placeholder="Enter your last name"
                                    onChange={handleChange}
                                    required
                                />
                            </div>

                            {/* Password */}
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

                            {/*Student ID */}
                            <div className="mb-3">
                                <label className="form-label">Student ID</label>
                                <input
                                    name="studentId"
                                    className="form-control"
                                    placeholder="123456"
                                    onChange={handleChange}
                                />
                            </div>

                            {/* User Type Dropdown */}
                            <div className="mb-3">
                                <label className="form-label">User Type</label>
                                <select
                                    name="userTypeId"
                                    className="form-select"
                                    onChange={handleChange}
                                >
                                    <option value="1">Student</option>
                                    <option value="2">Teacher</option>
                                </select>
                            </div>

                            {/* Submit Button */}
                            <button type="submit" className="btn btn-green btn-success w-100">
                                Register
                            </button>
                        </form>

                        {/* Login Link */}
                        <p className="text-center mt-3">
                            Already have an account? <Link to="/login">Login here</Link>
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default RegisterPage;

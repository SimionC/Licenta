import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap.min.css';
import '../App.css';

const ChangePasswordPage = () => {
    const navigate = useNavigate();
    const [formData, setFormData] = useState({
        newPassword: '',
        confirmPassword: ''
    });
    const [message, setMessage] = useState('');
    const [saving, setSaving] = useState(false);

    const handleChange = (event) => {
        const { name, value } = event.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const handleSubmit = async (event) => {
        event.preventDefault();
        setMessage('');

        if (formData.newPassword.length < 8) {
            setMessage('Password must be at least 8 characters.');
            return;
        }

        if (formData.newPassword !== formData.confirmPassword) {
            setMessage('The passwords do not match.');
            return;
        }

        setSaving(true);
        try {
            const response = await fetch('/api/Auth/change-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ newPassword: formData.newPassword })
            });

            if (!response.ok) {
                const text = await response.text();
                throw new Error(text || 'Password could not be changed.');
            }

            localStorage.removeItem('userEmail');
            localStorage.removeItem('userType');
            navigate('/');
        } catch (error) {
            setMessage(error.message || 'Password could not be changed.');
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="container mt-5">
            <div className="row justify-content-center">
                <div className="col-md-6">
                    <div className="card p-4 shadow">
                        <h2 className="text-center mb-3">Change Password</h2>
                        <p className="text-muted text-center mb-4">
                            Choose a new password before continuing.
                        </p>

                        {message && (
                            <div className="alert alert-warning" role="alert">
                                {message}
                            </div>
                        )}

                        <form onSubmit={handleSubmit}>
                            <div className="mb-3">
                                <label className="form-label">New password</label>
                                <input
                                    name="newPassword"
                                    type="password"
                                    className="form-control"
                                    value={formData.newPassword}
                                    onChange={handleChange}
                                    required
                                />
                            </div>

                            <div className="mb-3">
                                <label className="form-label">Confirm password</label>
                                <input
                                    name="confirmPassword"
                                    type="password"
                                    className="form-control"
                                    value={formData.confirmPassword}
                                    onChange={handleChange}
                                    required
                                />
                            </div>

                            <button type="submit" className="btn btn-green w-100" disabled={saving}>
                                {saving ? 'Saving...' : 'Save new password'}
                            </button>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ChangePasswordPage;

import React, { useState } from 'react';

/**
 * Purpose: displays two password inputs, validates them on the frontend, sends the new password to /api/Auth/change-password, and then calls onChanged when the password was successfully changed
 */

const PasswordChangeForm = ({ onChanged, submitLabel = 'Save new password' }) => {
    // -------------------------
    // FORM STATE
    // -------------------------
    const [formData, setFormData] = useState({
        newPassword: '',
        confirmPassword: ''
    });
    const [message, setMessage] = useState('');
    const [saving, setSaving] = useState(false);

    // Keeps both password fields in one form object.
    const handleChange = (event) => {
        const { name, value } = event.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    // Validates locally, sends the new password, then notifies the parent page.
    const handleSubmit = async (event) => {
        event.preventDefault();             //React handle the submit manually, no HTML reload
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

            setFormData({ newPassword: '', confirmPassword: '' });
            onChanged?.();
        } catch (error) {
            setMessage(error.message || 'Password could not be changed.');
        } finally {
            setSaving(false);
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            {/* Validation/API feedback. */}
            {message && (
                <div className="alert alert-warning" role="alert">
                    {message}
                </div>
            )}

            {/* New password + confirmation fields. */}
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
                {saving ? 'Saving...' : submitLabel}
            </button>
        </form>
    );
};

export default PasswordChangeForm;

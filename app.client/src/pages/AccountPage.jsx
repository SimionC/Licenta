import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Moon, Save, Shield, Sun, UserRound } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import PasswordChangeForm from '../components/PasswordChangeForm';
import '../App.css';

// Maps backend role names into readable labels for the profile form.
const roleLabels = {
    student: 'Student',
    teacher: 'Teacher',
    admin: 'Admin'
};

// Applies the selected theme globally and remembers it in the browser.
const applyTheme = (theme) => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem('theme', theme);
};

const AccountPage = () => {
    // -------------------------
    // ROUTE AND PAGE STATE
    // -------------------------
    const navigate = useNavigate();

    // Profile data, editable fields, theme preference, and loading/action flags.
    const [profile, setProfile] = useState(null);
    const [formData, setFormData] = useState({ nume: '', prenume: '' });
    const [theme, setTheme] = useState(() => localStorage.getItem('theme') || 'light');
    const [message, setMessage] = useState('');
    const [saving, setSaving] = useState(false);
    const [loading, setLoading] = useState(true);
    const [showPasswordForm, setShowPasswordForm] = useState(false);

    // -------------------------
    // THEME HANDLING
    // -------------------------
    useEffect(() => {
        applyTheme(theme);
    }, [theme]);

    // -------------------------
    // PROFILE LOADING
    // -------------------------
    useEffect(() => {
        const fetchProfile = async () => {
            setLoading(true);
            setMessage('');
            try {
                const response = await fetch('/api/Auth/profile', { credentials: 'include' });
                let data;

                if (response.ok) {
                    data = await response.json();
                } else {
                    const fallback = await fetch('/api/Auth/Me', { credentials: 'include' });
                    if (!fallback.ok) throw new Error('Could not load your account.');
                    const sessionProfile = await fallback.json();
                    data = {
                        id: sessionProfile.id,
                        email: sessionProfile.email,
                        nume: sessionProfile.name || '',
                        prenume: sessionProfile.lastName || '',
                        userTypeId: sessionProfile.userTypeId,
                        userType: sessionProfile.userType,
                        studentId: sessionProfile.studentId || ''
                    };
                }

                setProfile(data);
                setFormData({
                    nume: data.nume || '',
                    prenume: data.prenume || ''
                });
            } catch (error) {
                setMessage(error.message || 'Could not load your account.');
            } finally {
                setLoading(false);
            }
        };

        fetchProfile();
    }, []);

    // -------------------------
    // PROFILE AND SECURITY ACTIONS
    // -------------------------
    const handleChange = (event) => {
        const { name, value } = event.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const handleSaveProfile = async (event) => {
        event.preventDefault();
        setMessage('');
        setSaving(true);

        try {
            const response = await fetch('/api/Auth/profile', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(formData)
            });

            if (!response.ok) {
                const text = await response.text();
                throw new Error(text || 'Profile could not be saved.');
            }

            const updated = await response.json();
            setProfile(updated);
            setMessage('Profile saved.');
        } catch (error) {
            setMessage(error.message || 'Profile could not be saved.');
        } finally {
            setSaving(false);
        }
    };

    const handlePasswordChanged = () => {
        localStorage.removeItem('userEmail');
        localStorage.removeItem('userType');
        navigate('/');
    };

    return (
        <div className="account-page">
            {/* Page shell: sidebar plus the account settings workspace. */}
            <Sidebar />
            <main className="account-main">
                {/* Page heading. */}
                <header className="account-header">
                    <div>
                        <h1>Account</h1>
                        <p>Profile details, preferences, and password settings.</p>
                    </div>
                </header>

                {message && <div className="dashboard-data-message">{message}</div>}

                {/* Loading state while profile data is fetched. */}
                {loading ? (
                    <div className="account-card">Loading account...</div>
                ) : (
                    <div className="account-layout">
                        {/* Profile card: editable name fields plus read-only identity fields. */}
                        <section className="account-card">
                            <div className="account-card-title">
                                <UserRound size={20} />
                                <h2>Profile</h2>
                            </div>

                            <form className="account-form" onSubmit={handleSaveProfile}>
                                <div className="account-form-grid">
                                    <label>
                                        First name
                                        <input
                                            name="nume"
                                            value={formData.nume}
                                            onChange={handleChange}
                                            required
                                        />
                                    </label>

                                    <label>
                                        Last name
                                        <input
                                            name="prenume"
                                            value={formData.prenume}
                                            onChange={handleChange}
                                            required
                                        />
                                    </label>

                                    <label>
                                        Email
                                        <input value={profile?.email || ''} readOnly />
                                    </label>

                                    <label>
                                        Role
                                        <input value={roleLabels[profile?.userType] || profile?.userType || ''} readOnly />
                                    </label>

                                    {profile?.userTypeId === 1 && (
                                        <label>
                                            Student ID
                                            <input value={profile.studentId || ''} readOnly />
                                        </label>
                                    )}
                                </div>

                                <button className="btn btn-green account-save-btn" type="submit" disabled={saving}>
                                    <Save size={16} />
                                    {saving ? 'Saving...' : 'Save profile'}
                                </button>
                            </form>
                        </section>

                        {/* Preferences card: device-local theme toggle. */}
                        <section className="account-card">
                            <div className="account-card-title">
                                {theme === 'dark' ? <Moon size={20} /> : <Sun size={20} />}
                                <h2>Preferences</h2>
                            </div>

                            <div className="account-preference-row">
                                <div>
                                    <strong>Dark mode</strong>
                                    <p>Change the app appearance on this device.</p>
                                </div>
                                <button
                                    type="button"
                                    className={`theme-toggle ${theme === 'dark' ? 'active' : ''}`}
                                    onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
                                    aria-pressed={theme === 'dark'}
                                >
                                    <span>{theme === 'dark' ? 'On' : 'Off'}</span>
                                </button>
                            </div>
                        </section>

                        {/* Security card: opens the password-change form. */}
                        <section className="account-card">
                            <div className="account-card-title">
                                <Shield size={20} />
                                <h2>Security</h2>
                            </div>

                            <p className="account-muted">
                                Changing your password will log you out. Sign in again with the new password.
                            </p>

                            {showPasswordForm ? (
                                <PasswordChangeForm
                                    onChanged={handlePasswordChanged}
                                    submitLabel="Change password"
                                />
                            ) : (
                                <button
                                    type="button"
                                    className="account-secondary-btn"
                                    onClick={() => setShowPasswordForm(true)}
                                >
                                    Change password
                                </button>
                            )}
                        </section>
                    </div>
                )}
            </main>
        </div>
    );
};

export default AccountPage;

import React, { useEffect, useMemo, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { KeyRound, Plus, RefreshCw } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import { useAuth } from '../hooks/useAuth';
import '../App.css';

// Default values for the admin create-account form.
const emptyForm = {
    firstName: '',
    lastName: '',
    email: '',
    role: '1',
    studentId: ''
};

// Maps numeric backend role ids into readable labels for the table.
const roleLabels = {
    1: 'Student',
    2: 'Teacher',
    3: 'Admin'
};

const AccountsPage = () => {
    const { user, loading } = useAuth();

    // User list, create form, feedback, and loading/action flags.
    const [users, setUsers] = useState([]);
    const [formData, setFormData] = useState(emptyForm);
    const [temporaryPassword, setTemporaryPassword] = useState(null);
    const [message, setMessage] = useState('');
    const [fetching, setFetching] = useState(true);
    const [saving, setSaving] = useState(false);
    const [resettingId, setResettingId] = useState(null);

    const isStudent = formData.role === '1';

    const sortedUsers = useMemo(() => users, [users]);

    // -------------------------
    // LOAD EXISTING ACCOUNTS
    // -------------------------
    const fetchUsers = async () => {
        setFetching(true);
        setMessage('');
        try {
            const response = await fetch('/api/Auth/users', { credentials: 'include' });
            if (!response.ok) throw new Error('Could not load users.');
            const data = await response.json();
            setUsers(data);
        } catch (error) {
            setMessage(error.message || 'Could not load users.');
        } finally {
            setFetching(false);
        }
    };

    useEffect(() => {
        fetchUsers();
    }, []);

    // -------------------------
    // ROUTE GUARDS
    // -------------------------
    if (loading) {
        return <div>Loading...</div>;
    }

    if (user?.userType !== 'admin') {
        return <Navigate to="/dashboard" replace />;
    }

    // -------------------------
    // FORM AND ACCOUNT ACTIONS
    // -------------------------
    const handleChange = (event) => {
        const { name, value } = event.target;
        setFormData((prev) => ({
            ...prev,
            [name]: value,
            ...(name === 'role' && value !== '1' ? { studentId: '' } : {})
        }));
    };

    const handleCreate = async (event) => {
        event.preventDefault();
        setMessage('');
        setTemporaryPassword(null);
        setSaving(true);

        const payload = {
            nume: formData.firstName,
            prenume: formData.lastName,
            email: formData.email,
            userTypeId: Number(formData.role),
            studentId: isStudent ? formData.studentId : null
        };

        try {
            const response = await fetch('/api/Auth/users', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                const text = await response.text();
                throw new Error(text || 'Account could not be created.');
            }

            const result = await response.json();
            setTemporaryPassword({
                title: 'Temporary password created',
                email: result.user.email,
                password: result.temporaryPassword
            });
            setFormData(emptyForm);
            await fetchUsers();
        } catch (error) {
            setMessage(error.message || 'Account could not be created.');
        } finally {
            setSaving(false);
        }
    };

    const handleResetPassword = async (account) => {
        setMessage('');
        setTemporaryPassword(null);
        setResettingId(account.id);

        try {
            const response = await fetch(`/api/Auth/users/${account.id}/reset-password`, {
                method: 'POST',
                credentials: 'include'
            });

            if (!response.ok) {
                const text = await response.text();
                throw new Error(text || 'Password could not be regenerated.');
            }

            const result = await response.json();
            setTemporaryPassword({
                title: 'Temporary password regenerated',
                email: result.user.email,
                password: result.temporaryPassword
            });
            await fetchUsers();
        } catch (error) {
            setMessage(error.message || 'Password could not be regenerated.');
        } finally {
            setResettingId(null);
        }
    };

    return (
        <div className="accounts-page">
            {/* Page shell: sidebar plus admin account-management workspace. */}
            <Sidebar />
            <main className="accounts-main">
                {/* Page heading. */}
                <div className="accounts-header">
                    <div>
                        <h1>Accounts</h1>
                        <p>Create users and manage temporary passwords.</p>
                    </div>
                </div>

                {message && <div className="dashboard-data-message">{message}</div>}

                {/* One-time temporary password display after create/reset actions. */}
                {temporaryPassword && (
                    <div className="accounts-temp-password">
                        <div>
                            <strong>{temporaryPassword.title}</strong>
                            <p>Show this password to {temporaryPassword.email}. It will not be shown again.</p>
                        </div>
                        <code>{temporaryPassword.password}</code>
                    </div>
                )}

                <section className="accounts-layout">
                    {/* Create account form: admin chooses role and fills identity fields. */}
                    <form className="accounts-card accounts-form" onSubmit={handleCreate}>
                        <div className="accounts-card-title">
                            <Plus size={18} />
                            <h2>Create account</h2>
                        </div>

                        <div className="accounts-form-grid">
                            <label>
                                First name
                                <input
                                    name="firstName"
                                    value={formData.firstName}
                                    onChange={handleChange}
                                    required
                                />
                            </label>

                            <label>
                                Last name
                                <input
                                    name="lastName"
                                    value={formData.lastName}
                                    onChange={handleChange}
                                    required
                                />
                            </label>

                            <label>
                                Email
                                <input
                                    name="email"
                                    type="email"
                                    value={formData.email}
                                    onChange={handleChange}
                                    required
                                />
                            </label>

                            <label>
                                Role
                                <select name="role" value={formData.role} onChange={handleChange}>
                                    <option value="1">Student</option>
                                    <option value="2">Teacher</option>
                                    <option value="3">Admin</option>
                                </select>
                            </label>

                            {isStudent && (
                                <label>
                                    Student ID
                                    <input
                                        name="studentId"
                                        value={formData.studentId}
                                        onChange={handleChange}
                                        required
                                    />
                                </label>
                            )}
                        </div>

                        <button className="btn btn-green accounts-submit" type="submit" disabled={saving}>
                            {saving ? 'Creating...' : 'Create Account'}
                        </button>
                    </form>

                    {/* User table: shows account status and password-regeneration action. */}
                    <section className="accounts-card accounts-list-card">
                        <div className="accounts-card-title">
                            <KeyRound size={18} />
                            <h2>User list</h2>
                        </div>

                        {fetching ? (
                            <div className="dashboard-empty-card">Loading users...</div>
                        ) : (
                            <div className="accounts-table-wrap">
                                <table className="accounts-table">
                                    <thead>
                                        <tr>
                                            <th>Name</th>
                                            <th>Email</th>
                                            <th>Role</th>
                                            <th>Status</th>
                                            <th>Action</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {sortedUsers.map((account) => (
                                            <tr key={account.id}>
                                                <td>{account.nume} {account.prenume}</td>
                                                <td>{account.email}</td>
                                                <td>{roleLabels[account.userTypeId] || account.role}</td>
                                                <td>
                                                    {account.mustChangePassword
                                                        ? <span className="accounts-status pending">Temporary</span>
                                                        : <span className="accounts-status active">Active</span>}
                                                </td>
                                                <td>
                                                    <button
                                                        type="button"
                                                        className="accounts-reset-btn"
                                                        onClick={() => handleResetPassword(account)}
                                                        disabled={resettingId === account.id}
                                                    >
                                                        <RefreshCw size={15} />
                                                        {resettingId === account.id ? 'Working...' : 'Regenerate'}
                                                    </button>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </section>
                </section>
            </main>
        </div>
    );
};

export default AccountsPage;

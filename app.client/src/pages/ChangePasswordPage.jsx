import React from 'react';
import { useNavigate } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap.min.css';
import '../App.css';
import PasswordChangeForm from '../components/PasswordChangeForm';

const ChangePasswordPage = () => {
    const navigate = useNavigate();

    const handleChanged = () => {
        localStorage.removeItem('userEmail');
        localStorage.removeItem('userType');
        navigate('/');
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

                        <PasswordChangeForm onChanged={handleChanged} />
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ChangePasswordPage;

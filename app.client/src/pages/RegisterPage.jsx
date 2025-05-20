import React, { useState } from 'react';

const RegisterPage = () => {
    const [formData, setFormData] = useState({
        email: '',
        nume: '',
        prenume: '',
        password: '',
        studentId: '',
        userTypeId: 1, // example default value
    });

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            const res = await fetch('https://localhost:5001/Auth/Register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include', // Important for cookies
                body: JSON.stringify(formData),
            });

            if (res.ok) {
                alert('Registration successful');
                // Optional: navigate to login
            } else {
                alert('Registration failed');
            }
        } catch (err) {
            console.error(err);
            alert('An error occurred');
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <input name="email" placeholder="Email" onChange={handleChange} /><br />
            <input name="nume" placeholder="First Name" onChange={handleChange} /><br />
            <input name="prenume" placeholder="Last Name" onChange={handleChange} /><br />
            <input name="password" placeholder="Password" type="password" onChange={handleChange} /><br />
            <input name="studentId" placeholder="Student ID (optional)" onChange={handleChange} /><br />
            <select name="userTypeId" onChange={handleChange}>
                <option value="1">Student</option>
                <option value="2">Admin</option>
                {/* adapt based on real values */}
            </select><br />
            <button type="submit">Register</button>
        </form>
    );
};

export default RegisterPage;

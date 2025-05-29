import React, { useState } from 'react';

const CreateCourseForm = ({ onCreate }) => {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [joinCode, setJoinCode] = useState("");

    const handleSubmit = (e) => {
        e.preventDefault();
        if (title && description) {
            const newCourse = {
                id: Date.now(),
                title,
                description,
                teacher: "Current Teacher", // You can fetch this dynamically later
            };
            onCreate(newCourse);
            setTitle('');
            setDescription('');
        }
    };

    const handleJoinCourse = async () => {
        const res = await fetch("/api/Course/join", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(joinCode),
        });

        if (res.ok) {
            const course = await res.json();
            alert(`Joined course: ${course.title}`);
            // optionally add course to student view
        } else {
            alert("Invalid password or course not found.");
        }
    };

    return (
        <>
            <form onSubmit={handleSubmit} style={{ marginBottom: '2rem' }}>
                <h3>Create New Course</h3>
                <input
                    type="text"
                    placeholder="Course Title"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    required
                    style={{ display: 'block', marginBottom: '1rem' }}
                />
                <textarea
                    placeholder="Course Description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    required
                    style={{ display: 'block', marginBottom: '1rem' }}
                />
                <button type="submit">Create Course</button>
            </form>

            {/* Join Course Section (for students) */}
            <div style={{ marginBottom: '2rem' }}>
                <h3>Join a Course</h3>
                <input
                    type="text"
                    placeholder="Enter 4-digit code"
                    value={joinCode}
                    onChange={(e) => setJoinCode(e.target.value)}
                    required
                    style={{ display: 'block', marginBottom: '1rem' }}
                />
                <button onClick={handleJoinCourse}>Join</button>
            </div>
        </>
    );

};

export default CreateCourseForm;

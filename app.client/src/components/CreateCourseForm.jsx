import React, { useState } from 'react';

const CreateCourseForm = ({ onCreate }) => {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');

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

    return (
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
    );
};

export default CreateCourseForm;

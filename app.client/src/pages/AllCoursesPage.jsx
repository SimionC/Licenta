import React, { useState, useEffect } from 'react';

const AllCoursesPage = () => {
    const [courses, setCourses] = useState([]);
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const userType = 'teacher'; //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! may be a problem later 

    useEffect(() => {
        fetch("http://localhost:5141/Course/all")
            .then(res => res.json())
            .then(data => setCourses(data));
    }, []);

    const handleCreateCourse = async (e) => {
        e.preventDefault();

        const newCourse = {
            title,
            description,
            teacherEmail: "teacher@example.com", // replace with logged-in teacher email
        };

        const res = await fetch("http://localhost:5141/Course/create", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(newCourse),
        });

        if (res.ok) {
            const added = await res.json();
            setCourses([...courses, added]);
            setTitle('');
            setDescription('');
        } else {
            alert("Failed to create course");
        }
    };

    return (
        <div>
            <h2>Courses</h2>

            {/* Only show to teachers */}
            {userType === 'teacher' && (
                <form onSubmit={handleCreateCourse} style={{ marginBottom: '2rem' }}>
                    <input
                        type="text"
                        placeholder="Course Title"
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        required
                    />
                    <textarea
                        placeholder="Course Description"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        required
                    />
                    <button type="submit">Create Course</button>
                </form>
            )}

            <ul>
                {courses.map(course => (
                    <li key={course.id}>
                        <strong>{course.title}</strong><br />
                        <small>{course.description}</small>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default AllCoursesPage;

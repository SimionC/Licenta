import React, { useState, useEffect } from 'react';
import CreateCourseForm from '../components/CreateCourseForm'; // Optional: if separated

const AllCoursesPage = ({ userType }) => {
    const [courses, setCourses] = useState([]);
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');

    useEffect(() => {
        fetch("/api/Course/all")
            .then(res => res.json())
            .then(data => setCourses(data));
    }, []);

    const handleCreateCourse = async (e) => {
        e.preventDefault();

        const newCourse = {
            title,
            description,
            teacherEmail: "teacher@email.com", // replace with logged-in teacher email
        };

        const res = await fetch("/api/Course/create", {
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
        <div className="container mt-4">
            <h2>Courses</h2>

            {/* ✅ Show this only if user is teacher */}
            {userType === 'teacher' ? (
                <form onSubmit={handleCreateCourse} className="mb-4">
                    <div className="mb-3">
                        <input
                            type="text"
                            className="form-control"
                            placeholder="Course Title"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            required
                        />
                    </div>
                    <div className="mb-3">
                        <textarea
                            className="form-control"
                            placeholder="Course Description"
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            required
                        />
                    </div>
                    <button type="submit" className="btn btn-success">
                        Create Course
                    </button>
                </form>
            ) : (
                <p className="text-muted">Only teachers can create courses.</p>
            )}

            <ul className="list-group">
                {courses.map(course => (
                    <li key={course.id} className="list-group-item">
                        <strong>{course.title}</strong><br />
                        <small>{course.description}</small>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default AllCoursesPage;

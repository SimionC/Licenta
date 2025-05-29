import React, { useState, useEffect } from 'react';
//import CreateCourseForm from '../components/CreateCourseForm'; // Optional: if separated

const AllCoursesPage = ({ userType }) => {
    const [courses, setCourses] = useState([]);
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [joinCode, setJoinCode] = useState("");
    const [joinedCourse, setJoinedCourse] = useState(null);
    const [registeredCourses, setRegisteredCourses] = useState([]);
    const [otherCourses, setOtherCourses] = useState([]);

    useEffect(() => {
        fetch("/api/Course/all")
            .then(res => res.json())
            .then(data => setCourses(data))
            .catch(err => console.error("Failed to fetch courses", err));

        if (userType === 'student') {
            fetch("/api/Course/student", { credentials: "include" })
                .then(res => res.json())
                .then(data => {
                    setRegisteredCourses(data.registered);
                    setOtherCourses(data.others);
                });
        }

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

    const handleDeleteCourse = async (id) => {
        if (!window.confirm("Are you sure you want to delete this course?")) return;

        const res = await fetch(`/api/Course/delete/${id}`, {
            method: "DELETE",
        });

        if (res.ok) {
            setCourses(courses.filter(c => c.id !== id));
        } else {
            alert("Failed to delete course.");
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
            setJoinedCourse(course);
            alert(`Successfully joined: ${course.title}`);

            // ⬇️ FETCH updated course lists
            fetch("/api/Course/student", { credentials: "include" })
                .then(res => res.json())
                .then(data => {
                    setRegisteredCourses(data.registered);
                    setOtherCourses(data.others);
                });

            setJoinCode(""); // optional: clear input
        } else {
            alert("Invalid code. Please try again.");
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

            {/* ✅ Show full list only to teachers */}
            {userType === 'teacher' && (
                <ul className="list-group">
                    {courses.map(course => (
                        <li key={course.id} className="list-group-item d-flex justify-content-between align-items-start">
                            <div>
                                <strong>{course.title}</strong><br />
                                <small>{course.description}</small><br />
                                <small className="text-muted">
                                    Password: <strong>{course.joinPassword}</strong>
                                </small>
                            </div>
                            <button className="btn btn-sm btn-danger" onClick={() => handleDeleteCourse(course.id)}>
                                Delete
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            {userType === 'student' && (
                <>
                    <h4 className="mt-5">Register to a Course</h4>
                    <input
                        type="text"
                        className="form-control mb-2"
                        placeholder="Enter 4-digit password"
                        value={joinCode}
                        onChange={(e) => setJoinCode(e.target.value)}
                    />
                    <button className="btn btn-primary" onClick={handleJoinCourse}>
                        Join
                    </button>

                    {joinedCourse && (
                        <div className="alert alert-success mt-3">
                            Joined course: <strong>{joinedCourse.title}</strong>
                        </div>
                    )}

                    <hr />

                    <h4 className="mt-4">Your Courses</h4>
                    <ul className="list-group mb-4">
                        {registeredCourses.map(course => (
                            <li key={course.id} className="list-group-item">
                                <strong>{course.title}</strong><br />
                                <small>{course.description}</small>
                            </li>
                        ))}
                    </ul>

                    <h4>Other Available Courses</h4>
                    <ul className="list-group">
                        {otherCourses.map(course => (
                            <li key={course.id} className="list-group-item">
                                <strong>{course.title}</strong><br />
                                <small>{course.description}</small>
                            </li>
                        ))}
                    </ul>
                </>
            )}
        </div>
    );

};

export default AllCoursesPage;

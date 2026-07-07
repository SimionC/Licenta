import React, { useState, useEffect } from 'react';
import CoursesCourseCard from '../components/CoursesCourseCard';
import Sidebar from '../components/Sidebar';
//import CreateCourseForm from '../components/CreateCourseForm'; // Optional: if separated

/**
 * Purpose: different course actions depending on the user role: students can join courses, while teachers can create and manage their own courses
 */

const AllCoursesPage = ({ userType }) => {
    const [courses, setCourses] = useState([]);
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [joinCode, setJoinCode] = useState("");
    const [joinedCourse, setJoinedCourse] = useState(null);
    const [registeredCourses, setRegisteredCourses] = useState([]);
    const [showJoin, setShowJoin] = useState(false);
    
   
    useEffect(() => {
    // useEffect(userType): fetches role-specific course list when identity resolves.
        if (userType === 'student') {
            // only fetch the student's registered courses
            fetch("/api/Course/student", { credentials: "include" })
                .then(res => res.json())
                .then(data => setRegisteredCourses(data.registered))
                .catch(err => console.error(err));
        } else if (userType === 'teacher') {
            // fetch only the courses this teacher created
            fetch("/api/Course/all", { credentials: "include" })
                .then(res => {
                    if (res.ok) return res.json();
                    throw new Error("Failed to fetch teacher's courses");
                })
                .then(data => setCourses(Array.isArray(data) ? data : []))
                .catch(err => console.error(err));
        }
    }, [userType]);

    // handleCreateCourse: teacher flow to create and append course locally.
    const handleCreateCourse = async (e) => {
        e.preventDefault();

        const newCourse = {
            Title: title,
            Description: description
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
            setShowCreateModal(false);
        } else {
            alert("Failed to create course");
        }
    };

    // handleJoinCourse: student flow to join by code and refresh registered list.
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

            fetch("/api/Course/student", { credentials: "include" })
                .then(res => res.json())
                .then(data => {
                    setRegisteredCourses(data.registered);
                });

            setJoinCode("");
        } else {
            alert("Invalid code. Please try again.");
        }
    };

    return (
        <div className="app-layout-page courses-list-page">
            <Sidebar />
            <div className="app-page-main">
                <div className="dashboard-main">
                    <div className="dashboard-left">

                        {/* WRAPPER that centers and limits width */}
                        <div className="courses-content-wrapper">

                            {/* Top title + button */}
                            <div className="d-flex justify-content-between align-items-center mb-4">
                                <h1 className="dashboard-title">Courses</h1>

                                {userType === 'student' && (
                                    <button
                                        className="courses-join-btn"
                                        onClick={() => setShowJoin(prev => !prev)}
                                    >
                                        <span className="me-2">+</span> Join Course
                                    </button>
                                )}

                                {userType === 'teacher' && (
                                    <button
                                        className="courses-join-btn"
                                        onClick={() => setShowCreateModal(true)}
                                    >
                                        <span className="me-2">+</span> Create Course
                                    </button>
                                )}
                            </div>

                            {/* Join input form */}
                            {showJoin && (
                                <div className="d-flex gap-2 mb-4">
                                    <input
                                        type="text"
                                        className="form-control"
                                        placeholder="Enter password"
                                        value={joinCode}
                                        onChange={(e) => setJoinCode(e.target.value)}
                                        style={{ maxWidth: '200px' }}
                                    />
                                    <button className="btn btn-success" onClick={handleJoinCourse}>
                                        Submit
                                    </button>
                                </div>
                            )}

                            {/* Alert */}
                            {joinedCourse && (
                                <div className="alert alert-success mt-3">
                                    Joined course: <strong>{joinedCourse.title}</strong>
                                </div>
                            )}

                            {/* Student view */}
                            {userType === 'student' && (
                                <div className="courses-grid">
                                    {registeredCourses.length === 0
                                        ? <p>No enrolled courses yet. Use the join code from your teacher.</p>
                                        : registeredCourses.map(course => (
                                            <CoursesCourseCard key={course.id} course={course} />
                                        ))}
                                </div>
                            )}

                            {/*course list*/}
                            {userType === 'teacher' && (
                                <div className="courses-grid">
                                    {courses.length === 0
                                        ? <p>No courses created yet.</p>
                                        : courses.map(course => (
                                            <CoursesCourseCard key={course.id} course={course} />
                                        ))}
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>


            {/* to create a new course*/}
            {showCreateModal && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3 className="mb-3">Create New Course</h3>
                        <form onSubmit={handleCreateCourse}>
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
                            <div className="d-flex justify-content-between">
                                <button
                                    type="button"
                                    className="btn btn-secondary"
                                    onClick={() => setShowCreateModal(false)}
                                >
                                    Cancel
                                </button>
                                <button type="submit" className="btn btn-success">Create</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

        </div>
    );




};

export default AllCoursesPage;

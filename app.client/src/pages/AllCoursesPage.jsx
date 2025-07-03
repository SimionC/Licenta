import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import CoursesCourseCard from '../components/CoursesCourseCard';
import Sidebar from '../components/Sidebar';
//import CreateCourseForm from '../components/CreateCourseForm'; // Optional: if separated

const AllCoursesPage = ({ userType }) => {
    const [courses, setCourses] = useState([]);
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [showCreateModal, setShowCreateModal] = useState(false);
    const [joinCode, setJoinCode] = useState("");
    const [joinedCourse, setJoinedCourse] = useState(null);
    const [registeredCourses, setRegisteredCourses] = useState([]);
    // const [otherCourses, setOtherCourses] = useState([]);
    const [showJoin, setShowJoin] = useState(false);
  


   //TEST
    //useEffect(() => {
    //    if (userType === 'student') {
    //        const mockCourses = [
    //            {
    //                id: 1,
    //                title: "Finance",
    //                description: "Understand the core principles of corporate finance.",
    //                teacherName: "Dr. Sarah Johnson",
    //            },
    //            {
    //                id: 2,
    //                title: "Microeconomics",
    //                description: "Analyze individual markets and consumer behavior.",
    //                teacherName: "Prof. Michael Chen",
    //            },
    //            {
    //                id: 3,
    //                title: "Databases",
    //                description: "Learn SQL and relational database design.",
    //                teacherName: "Ms. Emily Rodriguez",
    //            },
    //            {
    //                id: 4,
    //                title: "React Fundamentals",
    //                description: "Build interactive UIs with modern JavaScript.",
    //                teacherName: "Mr. David Wilson",
    //            },
    //            {
    //                id: 5,
    //                title: "Finance",
    //                description: "Understand the core principles of corporate finance.",
    //                teacherName: "Dr. Sarah Johnson",
    //            },
    //            {
    //                id: 6,
    //                title: "Microeconomics",
    //                description: "Analyze individual markets and consumer behavior.",
    //                teacherName: "Prof. Michael Chen",
    //            },
    //            {
    //                id: 7,
    //                title: "Databases",
    //                description: "Learn SQL and relational database design.",
    //                teacherName: "Ms. Emily Rodriguez",
    //            },
    //            {
    //                id: 8,
    //                title: "React Fundamentals",
    //                description: "Build interactive UIs with modern JavaScript.",
    //                teacherName: "Mr. David Wilson",
    //            }
    //        ];

    //        setRegisteredCourses(mockCourses);
    //    }
    //}, [userType]);


    //useEffect(() => {
    //    fetch("/api/Course/all")
    //        .then(res => {
    //            if (res.ok) return res.json();
    //            throw new Error("Failed to fetch courses");
    //        })
    //        .then(data => setCourses(Array.isArray(data) ? data : []))
    //        .catch(err => console.error("Failed to fetch courses", err));

    //    if (userType === 'student') {
    //        fetch("/api/Course/student", { credentials: "include" })
    //            .then(res => res.json())
    //            .then(data => {
    //                setRegisteredCourses(data.registered);
    //                //setOtherCourses(data.others);
    //            });
    //    }

    //}, []);

    useEffect(() => {
        if (userType === 'student') {
            // only fetch the student's registered courses
            fetch("/api/Course/student", { credentials: "include" })
                .then(res => res.json())
                .then(data => setRegisteredCourses(data.registered))
                .catch(err => console.error(err));
        } else if (userType === 'teacher') {
            // fetch only the courses this teacher created
            fetch("/api/Course/teacher", { credentials: "include" })
                .then(res => {
                    if (res.ok) return res.json();
                    throw new Error("Failed to fetch teacher's courses");
                })
                .then(data => setCourses(Array.isArray(data) ? data : []))
                .catch(err => console.error(err));
        }
    }, [userType]);

    const handleCreateCourse = async (e) => {
        e.preventDefault();

        const newCourse = {
            Title: title,
            Description: description
            //teacherEmail: "teacher@email.com", // replace with logged-in teacher email
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
                    //setOtherCourses(data.others);
                });

            setJoinCode(""); // optional: clear input
        } else {
            alert("Invalid code. Please try again.");
        }
    };

    return (
        <div style={{ display: 'flex', backgroundColor: '#FBF6E9', minHeight: '100vh' }}>
            <Sidebar />
            <div style={{ flex: 1, padding: '3rem 4rem' }}>
                <div className="dashboard-main">
                    <div className="dashboard-left">

                        {/* 🆕 WRAPPER that centers and limits width */}
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
                                    {registeredCourses.map(course => (
                                        <CoursesCourseCard key={course.id} course={course} />
                                    ))}
                                </div>
                            )}

                            {/*course list*/}
                            <div className="courses-grid">
                                {courses.map(course => (
                                    <div key={course.id}>
                                        <CoursesCourseCard course={course} />
                                        <button
                                            className="btn btn-danger btn-sm mt-2"
                                            onClick={() => handleDeleteCourse(course.id)}
                                        >
                                            Delete
                                        </button>
                                    </div>
                                ))}
                            </div>
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

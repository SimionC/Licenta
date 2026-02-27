import { useParams, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import React from 'react';
import { ArrowLeft, Code, Upload, Plus } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import CourseDescriptionBox from '../components/CourseDescriptionBox';
import ResourceCard from '../components/ResourceCard';
import CreateCourseWorkForm from '../components/CreateCourseWorkForm';
import CourseAssignmentBox from '../components/CourseAssignmentBox';
import './CoursePage.css';
//import axios from 'axios';

/**
 * Purpose: Single-course view (description, resources tab, assignments tab).
 * API touched: GET /api/Course/{courseId}, GET/POST /api/Course/{courseId}/courseworks.
 * UI contract: teacher-only actions (join code visibility and assignment creation).
 */

const mockResources = [
    {
        title: 'Financial Ratios Cheat Sheet',
        type: 'PDF',
        size: '2.3 MB',
        uploadedAt: '1/10/2024',
    },
    {
        title: 'Week 1 Lecture Notes',
        type: 'DOC',
        size: '1.8 MB',
        uploadedAt: '1/8/2024',
    },
    {
        title: 'Excel Templates',
        type: 'ZIP',
        size: '5.2 MB',
        uploadedAt: '1/5/2024',
    },
    {
        title: 'Extra Practice Problems',
        type: 'TXT',
        size: '0.9 MB',
        uploadedAt: '1/4/2024',
    },
];

const CoursePage = () => {
    const { courseId } = useParams();
    const navigate = useNavigate();
    const [course, setCourse] = useState(null);
    const [activeTab, setActiveTab] = useState('resources');
    const [courseWorks, setCourseWorks] = useState([]);
    const [showForm, setShowForm] = useState(false);
    const [showAssignModal, setShowAssignModal] = useState(false);

    // useEffect(courseId): loads course metadata + assignment list.
    useEffect(() => {
        // 1) fetch the course metadata
        fetch(`/api/Course/${courseId}`)
            .then(r => r.json())
            .then(setCourse)
            .catch(err => console.error("Error loading course", err));

        // 2) fetch all the existing assignments
        fetch(`/api/Course/${courseId}/courseworks`)
            .then(r => {
                if (!r.ok) throw new Error("Could not load assignments");
                return r.json();
            })
            .then(setCourseWorks)
            .catch(err => console.error("Error loading assignments", err));
    }, [courseId]);

    if (!course) return <div className="text-center mt-5">Loading course...</div>;

    return (
        <div className="course-page-container">
            <Sidebar />
            <div className="course-page">
                {/* Course Header */}
                <div className="course-header">
                    <div className="course-header-left">
                        <button className="back-btn" onClick={() => navigate(-1)}>
                            <ArrowLeft size={20} />
                        </button>
                        <h1 className="course-title">{course.title}</h1>
                    </div>
                    {localStorage.getItem('userType') === 'teacher' && (
                        <div className="course-join-code">
                            <Code size={16} style={{ marginRight: '6px' }} />
                            Join Code: <strong>{course.joinPassword}</strong>
                        </div>
                    )}
                </div>

                {/* description */}
                <CourseDescriptionBox description={course.description} />

                {/* tabs + action button */}
                <div className="tabs-actions">
                    <div className="course-tabs">
                        <button
                            className={activeTab === 'resources' ? 'tab active' : 'tab'}
                            onClick={() => { setActiveTab('resources'); setShowForm(false); }}
                        >
                            Resources
                        </button>
                        <button
                            className={activeTab === 'assignments' ? 'tab active' : 'tab'}
                            onClick={() => { setActiveTab('assignments'); setShowForm(false); }}
                        >
                            Assignments
                        </button>
                    </div>
                    {activeTab === 'resources' && localStorage.getItem('userType') === 'teacher' && (
                        <button
                            className="upload-btn"
                            onClick={() => {/* open your upload form/modal here */ }}
                        >
                            <Upload size={18} style={{ marginRight: 8 }} />
                            Upload Resource
                        </button>
                    )}
                    {activeTab === 'assignments' && localStorage.getItem('userType') === 'teacher' && (
                        <button
                            className="upload-btn"
                            onClick={() => setShowAssignModal(true)}
                        >
                            <Plus size={18} style={{ marginRight: 8 }} />
                            Create Assignment
                        </button>
                    )}
                </div>

                {/* resources grid */}
                {activeTab === 'resources' && (
                    <div className="resource-grid">
                        {mockResources.map((r, i) => (
                            <ResourceCard
                                key={i}
                                title={r.title}
                                type={r.type}
                                size={r.size}
                                uploadedAt={r.uploadedAt}
                            />
                        ))}
                    </div>
                )}

                {/* assignments list + form */}
                {activeTab === 'assignments' && (
                    <>
                        {showForm && (
                            <CreateCourseWorkForm
                                courseId={courseId}
                                onCreated={cw => {
                                    setCourseWorks(prev => [cw, ...prev]);
                                    setShowForm(false);
                                }}
                            />
                        )}

                        <div className="resource-grid">
                            {courseWorks.map(cw => (
                                <CourseAssignmentBox
                                    key={cw.id}
                                    title={cw.title}
                                    description={cw.description}
                                    deadline={cw.deadline}
                                />
                            ))}
                        </div>
                    </>
                )}

            </div>

            {showAssignModal && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h3>Create Assignment</h3>
                            <button className="modal-close" onClick={() => setShowAssignModal(false)}>×</button>
                        </div>
                        <form onSubmit={async e => {
                            e.preventDefault();
                            // grab form values
                            const title = e.target.title.value;
                            const description = e.target.description.value;
                            const deadline = e.target.deadline.value;
                            // post to backend
                            const res = await fetch(`/api/Course/${courseId}/coursework`, {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify({ title, description, deadline })
                            });
                            if (res.ok) {
                                const cw = await res.json();
                                setCourseWorks(prev => [...prev, cw]);
                                setShowAssignModal(false);
                            } else {
                                alert('Failed to create assignment');
                            }
                        }}>
                            <div className="form-group">
                                <label>Assignment Title</label>
                                <input name="title" type="text" required placeholder="Enter assignment title" />
                            </div>
                            <div className="form-group">
                                <label>Description</label>
                                <textarea name="description" placeholder="Enter assignment description" />
                            </div>
                            <div className="form-group">
                                <label>Due Date</label>
                                <input name="deadline" type="date" required />
                            </div>
                            <div className="modal-actions">
                                <button type="button" onClick={() => setShowAssignModal(false)} className="btn-cancel">
                                    Cancel
                                </button>
                                <button type="submit" className="btn-confirm">
                                    Create
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}


        </div>
    );
};

export default CoursePage;


//export default function CoursePage() {
//    const { courseId } = useParams();
//    const [course, setCourse] = useState(null);
//    const [courseWorks, setCourseWorks] = useState([]);

//    useEffect(() => {
//        // Get course info
//        axios.get(`/api/Course/${courseId}`)
//            .then(res => setCourse(res.data))
//            .catch(err => console.error("Error loading course", err));

//        // Save courseId to local storage
//        const userEmail = localStorage.getItem("userEmail"); // Or another identifier you're already storing
//        if (!userEmail) return;
//        const key = `recentCourses_${userEmail}`;
//        const existing = JSON.parse(localStorage.getItem(key) || '[]');
//        const updated = [courseId, ...existing.filter(id => id !== courseId)].slice(0, 5);
//        localStorage.setItem(key, JSON.stringify(updated));

//        // Get assignments
//        axios.get(`/api/Course/${courseId}/courseworks`)
//            .then(res => setCourseWorks(res.data))
//            .catch(err => console.error("Error loading courseworks", err));

//    }, [courseId]);

//    if (!course) return <div className="text-center mt-5">Loading course...</div>;

//    return (
//        <div className="container mt-4">
//            <h2 className="mb-4">📘 {course.title}</h2>

//            {/* Assignment creation form */}
//            {localStorage.getItem("userType") === "teacher" && (
//                <CreateCourseWorkForm courseId={courseId} onCreated={(cw) =>
//                    setCourseWorks(prev => [...prev, cw])
//                } />
//            )}

//            {/* Assignment list */}
//            <h4 className="mt-5">Assignments</h4>
//            {courseWorks.length === 0 ? (
//                <p className="text-muted">No assignments yet.</p>
//            ) : (
//                <ul className="list-group">
//                    {courseWorks.map((cw) => (
//                        <li key={cw.id} className="list-group-item">
//                            <strong>{cw.title}</strong><br />
//                            <small>{cw.description}</small><br />
//                            <small className="text-muted">📅 Deadline: {cw.deadline?.split('T')[0]}</small>
//                        </li>
//                    ))}
//                </ul>
//            )}
//        </div>
//    );
//}

import { useParams, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import React from 'react';
import { ArrowLeft, Code, Lock, Pencil, Plus, RotateCcw, Save, Unlock, Upload } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import CourseDescriptionBox from '../components/CourseDescriptionBox';
import ResourceCard from '../components/ResourceCard';
import CourseAssignmentBox from '../components/CourseAssignmentBox';
import './CoursePage.css';

/**
 * Purpose: Single-course view with role-aware management.
 * API touched: course detail, resources upload/download/delete, close/reopen, edit, coursework list/create.
 * UI contract: teacher owners manage; enrolled students consume resources and assignments.
 */

const formatBytes = (bytes = 0) => {
    if (!bytes) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB'];
    const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
    const value = bytes / Math.pow(1024, index);
    return `${value.toFixed(value >= 10 || index === 0 ? 0 : 1)} ${units[index]}`;
};

const formatDate = (value) => {
    if (!value) return 'No date';
    return new Date(value).toLocaleDateString();
};

const CoursePage = () => {
    const { courseId } = useParams();
    const navigate = useNavigate();
    const [course, setCourse] = useState(null);
    const [activeTab, setActiveTab] = useState('resources');
    const [courseWorks, setCourseWorks] = useState([]);
    const [resources, setResources] = useState([]);
    const [showAssignModal, setShowAssignModal] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editValues, setEditValues] = useState({ title: '', description: '' });
    const [selectedFile, setSelectedFile] = useState(null);
    const [message, setMessage] = useState('');

    const loadCourseData = async () => {
        setMessage('');
        try {
            const [courseRes, resourcesRes, worksRes] = await Promise.all([
                fetch(`/api/Course/${courseId}`, { credentials: 'include' }),
                fetch(`/api/Course/${courseId}/resources`, { credentials: 'include' }),
                fetch(`/api/Course/${courseId}/courseworks`, { credentials: 'include' })
            ]);

            if (!courseRes.ok) throw new Error('Could not load course');
            const courseData = await courseRes.json();
            setCourse(courseData);
            setEditValues({ title: courseData.title, description: courseData.description });

            if (resourcesRes.ok) setResources(await resourcesRes.json());
            if (worksRes.ok) setCourseWorks(await worksRes.json());
        } catch (err) {
            console.error(err);
            setMessage('Could not load this course. Make sure you are enrolled or own it.');
        }
    };

    useEffect(() => {
        loadCourseData();
    }, [courseId]);

    const handleSaveCourse = async (e) => {
        e.preventDefault();
        const res = await fetch(`/api/Course/${courseId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(editValues)
        });

        if (!res.ok) {
            setMessage('Failed to update course details.');
            return;
        }

        const updated = await res.json();
        setCourse(updated);
        setIsEditing(false);
        setMessage('Course details updated.');
    };

    const handleToggleClosed = async () => {
        const action = course.isClosed ? 'reopen' : 'close';
        const res = await fetch(`/api/Course/${courseId}/${action}`, {
            method: 'POST',
            credentials: 'include'
        });

        if (!res.ok) {
            setMessage(`Failed to ${action} course.`);
            return;
        }

        setCourse(await res.json());
    };

    const handleUploadResource = async (e) => {
        e.preventDefault();
        if (!selectedFile) return;

        const formData = new FormData();
        formData.append('file', selectedFile);

        const res = await fetch(`/api/Course/${courseId}/resources`, {
            method: 'POST',
            credentials: 'include',
            body: formData
        });

        if (!res.ok) {
            setMessage('Resource upload failed.');
            return;
        }

        const resource = await res.json();
        setResources(prev => [resource, ...prev]);
        setSelectedFile(null);
        e.target.reset();
        setMessage('Resource uploaded.');
    };

    const handleDeleteResource = async (resourceId) => {
        if (!window.confirm('Remove this resource from the course?')) return;

        const res = await fetch(`/api/Course/resources/${resourceId}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (res.ok) {
            setResources(prev => prev.filter(resource => resource.id !== resourceId));
        } else {
            setMessage('Failed to remove resource.');
        }
    };

    const handleCreateAssignment = async (e) => {
        e.preventDefault();
        const title = e.target.title.value;
        const description = e.target.description.value;
        const deadline = e.target.deadline.value;

        const res = await fetch(`/api/Course/${courseId}/coursework`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ title, description, deadline })
        });

        if (res.ok) {
            const cw = await res.json();
            setCourseWorks(prev => [...prev, cw]);
            setShowAssignModal(false);
        } else {
            setMessage('Failed to create assignment.');
        }
    };

    if (message && !course) {
        return (
            <div className="course-page-container">
                <Sidebar />
                <div className="course-page">
                    <button className="back-btn" onClick={() => navigate(-1)}>
                        <ArrowLeft size={20} />
                    </button>
                    <div className="course-empty-state">{message}</div>
                </div>
            </div>
        );
    }

    if (!course) return <div className="text-center mt-5">Loading course...</div>;

    const canManage = course.canManage;

    return (
        <div className="course-page-container">
            <Sidebar />
            <div className="course-page">
                <div className="course-header">
                    <div className="course-header-left">
                        <button className="back-btn" onClick={() => navigate(-1)}>
                            <ArrowLeft size={20} />
                        </button>
                        <div>
                            <div className="course-title-row">
                                <h1 className="course-title">{course.title}</h1>
                                <span className={`course-status ${course.isClosed ? 'course-status--closed' : 'course-status--open'}`}>
                                    {course.isClosed ? 'Closed' : 'Open'}
                                </span>
                            </div>
                            <p className="course-teacher">Teacher: {course.teacherName}</p>
                        </div>
                    </div>

                    {canManage && (
                        <div className="course-actions">
                            <div className="course-join-code">
                                <Code size={16} />
                                Join Code: <strong>{course.joinPassword}</strong>
                            </div>
                            <button className="secondary-action-btn" onClick={() => setIsEditing(true)}>
                                <Pencil size={16} />
                                Edit
                            </button>
                            <button className="secondary-action-btn" onClick={handleToggleClosed}>
                                {course.isClosed ? <Unlock size={16} /> : <Lock size={16} />}
                                {course.isClosed ? 'Reopen' : 'Close'}
                            </button>
                        </div>
                    )}
                </div>

                {message && <div className="course-message">{message}</div>}

                {isEditing ? (
                    <form className="course-edit-box" onSubmit={handleSaveCourse}>
                        <label>
                            Course title
                            <input
                                value={editValues.title}
                                onChange={(e) => setEditValues(prev => ({ ...prev, title: e.target.value }))}
                                required
                            />
                        </label>
                        <label>
                            Description
                            <textarea
                                value={editValues.description}
                                onChange={(e) => setEditValues(prev => ({ ...prev, description: e.target.value }))}
                                required
                            />
                        </label>
                        <div className="course-edit-actions">
                            <button type="button" className="btn-cancel" onClick={() => setIsEditing(false)}>
                                Cancel
                            </button>
                            <button type="submit" className="btn-confirm">
                                <Save size={16} />
                                Save
                            </button>
                        </div>
                    </form>
                ) : (
                    <CourseDescriptionBox description={course.description} />
                )}

                <div className="tabs-actions">
                    <div className="course-tabs">
                        <button
                            className={activeTab === 'resources' ? 'tab active' : 'tab'}
                            onClick={() => setActiveTab('resources')}
                        >
                            Resources
                        </button>
                        <button
                            className={activeTab === 'assignments' ? 'tab active' : 'tab'}
                            onClick={() => setActiveTab('assignments')}
                        >
                            Assignments
                        </button>
                    </div>
                    {activeTab === 'assignments' && canManage && (
                        <button className="upload-btn" onClick={() => setShowAssignModal(true)}>
                            <Plus size={18} />
                            Create Assignment
                        </button>
                    )}
                </div>

                {activeTab === 'resources' && (
                    <>
                        {canManage && (
                            <form className="resource-upload-box" onSubmit={handleUploadResource}>
                                <label>
                                    Upload course resource
                                    <input type="file" onChange={(e) => setSelectedFile(e.target.files?.[0] ?? null)} />
                                </label>
                                <button type="submit" className="upload-btn" disabled={!selectedFile}>
                                    <Upload size={18} />
                                    Upload Resource
                                </button>
                            </form>
                        )}

                        {resources.length === 0 ? (
                            <div className="course-empty-state">No resources have been added yet.</div>
                        ) : (
                            <div className="resource-grid">
                                {resources.map(resource => (
                                    <ResourceCard
                                        key={resource.id}
                                        title={resource.title}
                                        type={resource.type || 'FILE'}
                                        size={formatBytes(resource.size)}
                                        uploadedAt={formatDate(resource.uploadedAt)}
                                        canManage={canManage}
                                        onDownload={() => {
                                            window.location.href = `/api/Course/resources/${resource.id}/download`;
                                        }}
                                        onDelete={() => handleDeleteResource(resource.id)}
                                    />
                                ))}
                            </div>
                        )}
                    </>
                )}

                {activeTab === 'assignments' && (
                    <div className="resource-grid">
                        {courseWorks.length === 0 ? (
                            <div className="course-empty-state">No assignments yet.</div>
                        ) : (
                            courseWorks.map(cw => (
                                <CourseAssignmentBox
                                    key={cw.id}
                                    title={cw.title}
                                    description={cw.description}
                                    deadline={formatDate(cw.deadline)}
                                    status={course.isClosed ? 'completed' : 'pending'}
                                    actionLabel={!canManage ? 'Answer Assignment' : undefined}
                                    onAction={() => setMessage('Assignment answering will be implemented in the assignments module.')}
                                />
                            ))
                        )}
                    </div>
                )}
            </div>

            {showAssignModal && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h3>Create Assignment</h3>
                            <button className="modal-close" onClick={() => setShowAssignModal(false)}>x</button>
                        </div>
                        <form onSubmit={handleCreateAssignment}>
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

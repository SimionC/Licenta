import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import CoursesCourseCard from '../components/CoursesCourseCard';
import NotesNoteCard from '../components/NotesNoteCard';
import AssignmentCard from '../components/AssignmentCard';
import { Clock } from "react-feather";
import { BookOpen, FileText } from 'lucide-react';

/**
 * Purpose: Landing dashboard with real recent courses, notes, and assignment urgency.
 * API touched: GET /api/Dashboard.
 */

const formatDate = (value) => {
    if (!value) return 'No deadline';
    return new Date(value).toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    });
};

const cleanPreview = (value = '') => value
    .replace(/[#*_`~>-]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();

const getDueDateStatus = (value) => {
    if (!value) return 'ok';

    const now = new Date();
    const due = new Date(value);
    if (due.getHours() === 0 && due.getMinutes() === 0 && due.getSeconds() === 0) {
        due.setHours(23, 59, 59, 999);
    }

    if (due < now) return 'closed';
    if (due.toDateString() === now.toDateString()) return 'urgent';

    const soon = new Date(now);
    soon.setDate(soon.getDate() + 3);
    return due <= soon ? 'warning' : 'ok';
};

const DashboardPage = () => {
    const navigate = useNavigate();
    const [dashboard, setDashboard] = useState({
        role: '',
        recentCourses: [],
        recentNotes: [],
        urgentAssignments: []
    });
    const [message, setMessage] = useState('');

    useEffect(() => {
        const loadDashboard = async () => {
            try {
                const res = await fetch('/api/Dashboard', { credentials: 'include' });
                if (!res.ok) throw new Error('Dashboard request failed.');
                const data = await res.json();
                setDashboard({
                    role: data.role || '',
                    recentCourses: data.recentCourses || [],
                    recentNotes: data.recentNotes || [],
                    urgentAssignments: data.urgentAssignments || []
                });
            } catch (err) {
                console.error(err);
                setMessage('Could not load dashboard data.');
            }
        };

        loadDashboard();
    }, []);

    const assignmentCards = dashboard.urgentAssignments.map(assignment => ({
        id: assignment.id,
        courseId: assignment.courseId,
        title: assignment.title,
        course: assignment.courseTitle,
        due: `${assignment.label} - ${formatDate(assignment.deadline)}`,
        status: getDueDateStatus(assignment.deadline)
    }));

    const noteCards = dashboard.recentNotes.map(note => ({
        ...note,
        title: note.title || 'Untitled Note',
        desc: cleanPreview(note.preview) || 'No content yet.',
        tag: note.collaborationName ? 'Workspace' : 'Note',
        date: formatDate(note.updatedAt),
        folderName: note.collaborationName || note.folderName || 'Personal'
    }));

    return (
        <div className="app-layout-page dashboard-page">
            <Sidebar />
            <div className="app-page-main">
                <div className="dashboard-main">
                    <div className="dashboard-left">
                        <h1 className="dashboard-title">Dashboard</h1>

                        {message && <div className="dashboard-data-message">{message}</div>}

                        <div className="section-header">
                            <BookOpen size={20} color="#118B50" />
                            <h4>Recent Courses</h4>
                        </div>

                        <div className="card-grid dashboard-card-grid">
                            {dashboard.recentCourses.length === 0 ? (
                                <div className="dashboard-empty-card">No courses yet.</div>
                            ) : (
                                dashboard.recentCourses.slice(0, 4).map(course => (
                                    <CoursesCourseCard key={course.id} course={course} />
                                ))
                            )}
                        </div>

                        <div className="section-header" style={{ marginTop: '3rem' }}>
                            <FileText size={20} color="#118B50" />
                            <h4>Recent Notes</h4>
                        </div>

                        <div className="card-grid dashboard-card-grid">
                            {noteCards.length === 0 ? (
                                <div className="dashboard-empty-card">No notes yet.</div>
                            ) : (
                                noteCards.slice(0, 4).map(note => (
                                    <button
                                        key={note.guid || note.id}
                                        type="button"
                                        className="dashboard-note-card-link"
                                        onClick={() => navigate(note.collaborationId
                                            ? `/collaborations/${note.collaborationId}/notes/${note.guid}`
                                            : `/notes/${note.guid}`)}
                                    >
                                        <NotesNoteCard note={note} />
                                    </button>
                                ))
                            )}
                        </div>
                    </div>

                    <div className="dashboard-right">
                        <div className="section-header">
                            <Clock size={20} color="#118B50" />
                            <h4>Upcoming Assignments</h4>
                        </div>
                        {assignmentCards.length === 0 ? (
                            <div className="dashboard-empty-card">No urgent assignments.</div>
                        ) : (
                            assignmentCards.map(assign => (
                                <AssignmentCard
                                    key={`${assign.id}-${assign.course}`}
                                    assignment={assign}
                                    onClick={() => navigate(`/courses/${assign.courseId}?tab=assignments`)}
                                />
                            ))
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default DashboardPage;

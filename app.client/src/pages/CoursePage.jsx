import { useParams, useNavigate, useSearchParams } from 'react-router-dom'; //route param from the URL
import { useEffect, useState } from 'react';
import React from 'react';
import {ArrowLeft,Code,Download,FileText,Flag,CheckCircle,Lock,Pencil,Plus,Save,Trash2,Unlock,Upload} from 'lucide-react';
import Sidebar from '../components/Sidebar';
import CourseDescriptionBox from '../components/CourseDescriptionBox';
import ResourceCard from '../components/ResourceCard';
import CourseAssignmentBox from '../components/CourseAssignmentBox';
import './CoursePage.css';

/**
 * Purpose: Single-course view with role-aware course, resource, assignment, submission, and grade management.
 * Teachers can edit/delete/close the course, upload resources, create assignments, view submissions, and grade students. 
 * Students can view resources, submit text/file answers, submit note snapshots, and see grades. 
 */

//converts file sizes into readable text like 12KN or 2MB
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

const toDateInput = (value) => { //for HTML data iuput
    if (!value) return '';
    return new Date(value).toISOString().slice(0, 10);
};

const isDeadlineReached = (value) => {
    if (!value) return false;
    const deadline = new Date(value);
    if (deadline.getHours() === 0 && deadline.getMinutes() === 0 && deadline.getSeconds() === 0) {
        deadline.setHours(23, 59, 59, 999);
    }
    return Date.now() > deadline.getTime();
};

//to preview submitted note snapshots in a cleaner way
const cleanInlineMarkdown = (text = '') => text
    .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
    .replace(/[*_`~]/g, '')
    .trim();

const renderSnapshotContent = (content = '', compact = false) => {
    const lines = content.split(/\r?\n/);
    const visibleLines = compact ? lines.slice(0, 8) : lines;
    const elements = visibleLines.map((line, index) => {
        const trimmed = line.trim();
        if (!trimmed) return <div key={index} className="snapshot-line-break" />;

        if (trimmed.startsWith('### ')) {
            return <h4 key={index}>{cleanInlineMarkdown(trimmed.slice(4))}</h4>;
        }

        if (trimmed.startsWith('## ')) {
            return <h3 key={index}>{cleanInlineMarkdown(trimmed.slice(3))}</h3>;
        }

        if (trimmed.startsWith('# ')) {
            return <h2 key={index}>{cleanInlineMarkdown(trimmed.slice(2))}</h2>;
        }

        const bulletMatch = trimmed.match(/^[-*]\s+(.+)/);
        if (bulletMatch) {
            return (
                <p key={index} className="snapshot-bullet">
                    <span aria-hidden="true">*</span>
                    {cleanInlineMarkdown(bulletMatch[1])}
                </p>
            );
        }

        const numberedMatch = trimmed.match(/^(\d+)\.\s+(.+)/);
        if (numberedMatch) {
            return (
                <p key={index} className="snapshot-numbered">
                    <span>{numberedMatch[1]}.</span>
                    {cleanInlineMarkdown(numberedMatch[2])}
                </p>
            );
        }

        return <p key={index}>{cleanInlineMarkdown(trimmed)}</p>;
    });

    if (compact && lines.length > visibleLines.length) {
        elements.push(<p key="snapshot-more" className="snapshot-more">Open snapshot to read the full answer.</p>);
    }

    return elements;
};

//shows a compact preview of a submitted answer or note snapshot
const SubmissionAnswerPreview = ({ title, content, label = 'Submitted answer', actionLabel = 'Open full answer', onOpen }) => {
    if (!content) return null;

    return (
        <button
            type="button"
            className="note-snapshot-preview"
            onClick={() => onOpen({ title: title || label, content, label })}
        >
            <div className="note-snapshot-preview__header">
                <span>{label}</span>
                <strong>{title || 'Untitled Answer'}</strong>
            </div>
            <div className="note-snapshot-preview__body">
                {renderSnapshotContent(content, true)}
            </div>
            <span className="note-snapshot-preview__open">{actionLabel}</span>
        </button>
    );
};

const CoursePage = () => {
    const { courseId } = useParams();
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const [course, setCourse] = useState(null);
    const [activeTab, setActiveTab] = useState(searchParams.get('tab') === 'assignments' ? 'assignments' : 'resources');
    const [courseWorks, setCourseWorks] = useState([]);
    const [resources, setResources] = useState([]);
    const [availableNotes, setAvailableNotes] = useState([]); //notes a student can submit as snapshots
    const [gradesData, setGradesData] = useState(null);
    const [submissionsByAssignment, setSubmissionsByAssignment] = useState({});
    const [savedGradeIds, setSavedGradeIds] = useState({});
    const [showAssignModal, setShowAssignModal] = useState(false);
    const [editingAssignment, setEditingAssignment] = useState(null);
    const [assignmentToDelete, setAssignmentToDelete] = useState(null);
    const [isDeletingAssignment, setIsDeletingAssignment] = useState(false);
    const [deleteAssignmentError, setDeleteAssignmentError] = useState('');
    const [resourceToDelete, setResourceToDelete] = useState(null);
    const [showDeleteCourseModal, setShowDeleteCourseModal] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editValues, setEditValues] = useState({ title: '', description: '' });
    const [selectedFile, setSelectedFile] = useState(null);
    const [message, setMessage] = useState('');
    const [openSnapshot, setOpenSnapshot] = useState(null);

    // ---------------------------------shared general data-----------------------------------------------------

    const loadCourseData = async () => {
        setMessage('');
        try {
            const [courseRes, resourcesRes, worksRes, gradesRes] = await Promise.all([
                fetch(`/api/Course/${courseId}`, { credentials: 'include' }),
                fetch(`/api/Course/${courseId}/resources`, { credentials: 'include' }),
                fetch(`/api/Course/${courseId}/courseworks`, { credentials: 'include' }),
                fetch(`/api/Course/${courseId}/grades`, { credentials: 'include' })
            ]);

            if (!courseRes.ok) throw new Error('Could not load course');
            const courseData = await courseRes.json();
            setCourse(courseData);
            setEditValues({ title: courseData.title, description: courseData.description });

            if (resourcesRes.ok) setResources(await resourcesRes.json());
            if (worksRes.ok) setCourseWorks(await worksRes.json());
            if (gradesRes.ok) setGradesData(await gradesRes.json());

            if (!courseData.canManage) { //true = treacher, false = student
                const notesRes = await fetch('/api/Notes/accessible-notes', { credentials: 'include' });
                if (notesRes.ok) setAvailableNotes(await notesRes.json());
            }
        } catch (err) {
            console.error(err);
            setMessage('Could not load this course. Make sure you are enrolled or own it.');
        }
    };

    useEffect(() => {
        loadCourseData();
    }, [courseId]);

    useEffect(() => {
        const requestedTab = searchParams.get('tab');
        if (['resources', 'assignments', 'grades'].includes(requestedTab)) {
            setActiveTab(requestedTab);
        }
    }, [searchParams]);


    const refreshGrades = async () => {
        const res = await fetch(`/api/Course/${courseId}/grades`, { credentials: 'include' });
        if (res.ok) setGradesData(await res.json());
    };

    const refreshAssignments = async () => {
        const res = await fetch(`/api/Course/${courseId}/courseworks`, { credentials: 'include' });
        if (res.ok) setCourseWorks(await res.json());
    };

    // ---------------------------------TEACHER COURSE-----------------------------------------------------

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

    const handleDeleteCourse = async () => {
        const res = await fetch(`/api/Course/delete/${courseId}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (res.ok) {
            navigate('/courses/all');
        } else {
            setShowDeleteCourseModal(false);
            setMessage('Failed to delete course.');
        }
    };

    // ---------------------------------TEACHER RESOURCES-----------------------------------------------------

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

    const handleDeleteResource = async () => {
        if (!resourceToDelete) return;

        const res = await fetch(`/api/Course/resources/${resourceToDelete.id}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (res.ok) {
            setResources(prev => prev.filter(resource => resource.id !== resourceToDelete.id));
            setResourceToDelete(null);
            setMessage('Resource deleted.');
        } else {
            setMessage('Failed to remove resource.');
        }
    };

    // ---------------------------------TEACHER ASSIGNMENT-----------------------------------------------------

    const handleDeleteAssignment = async () => {
        if (!assignmentToDelete) return;

        setIsDeletingAssignment(true);
        setDeleteAssignmentError('');

        const res = await fetch(`/api/Course/${courseId}/coursework/${assignmentToDelete.id}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (res.ok) {
            setCourseWorks(prev => prev.filter(assignment => assignment.id !== assignmentToDelete.id));
            setSubmissionsByAssignment(prev => {
                const next = { ...prev };
                delete next[assignmentToDelete.id];
                return next;
            });
            await refreshGrades();
            setAssignmentToDelete(null);
            setShowAssignModal(false);
            setEditingAssignment(null);
            setMessage('Assignment deleted.');
        } else {
            const errorText = await res.text();
            setDeleteAssignmentError(errorText || `Failed to delete assignment. Server returned ${res.status}.`);
            setMessage('Failed to delete assignment.');
        }

        setIsDeletingAssignment(false);
    };

    const handleCreateOrUpdateAssignment = async (e) => {
        e.preventDefault();
        const payload = {
            title: e.target.title.value,
            description: e.target.description.value,
            deadline: e.target.deadline.value || null,
            weightPercent: Number(e.target.weightPercent.value || 0)
        };

        const isEditingAssignment = Boolean(editingAssignment);
        const url = isEditingAssignment
            ? `/api/Course/${courseId}/coursework/${editingAssignment.id}`
            : `/api/Course/${courseId}/coursework`;

        const res = await fetch(url, {
            method: isEditingAssignment ? 'PUT' : 'POST', 
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            await refreshAssignments();
            await refreshGrades();
            setShowAssignModal(false);
            setEditingAssignment(null);
        } else {
            setMessage('Failed to save assignment.');
        }
    };

    const handleUploadSupportingFile = async (assignmentId, e) => {
        e.preventDefault();
        const file = e.target.file.files?.[0];
        if (!file) return;

        const formData = new FormData();
        formData.append('file', file);

        const res = await fetch(`/api/Course/coursework/${assignmentId}/supporting-files`, {
            method: 'POST',
            credentials: 'include',
            body: formData
        });

        if (res.ok) {
            const fileData = await res.json();
            setCourseWorks(prev => prev.map(cw =>
                cw.id === assignmentId
                    ? { ...cw, supportingFiles: [fileData, ...(cw.supportingFiles || [])] }
                    : cw
            ));
            e.target.reset();
        } else {
            setMessage('Could not upload assignment file.');
        }
    };

    const loadSubmissions = async (assignmentId) => {
        if (submissionsByAssignment[assignmentId]) {
            setSubmissionsByAssignment(prev => ({ ...prev, [assignmentId]: null }));
            return;
        }

        const res = await fetch(`/api/Course/${courseId}/coursework/${assignmentId}/submissions`, {
            credentials: 'include'
        });

        if (res.ok) {
            const submissions = await res.json();
            setSubmissionsByAssignment(prev => ({ ...prev, [assignmentId]: submissions }));
        } else {
            setMessage('Could not load submissions.');
        }
    };

    const handleGradeSubmission = async (submissionId, assignmentId, e) => {
        e.preventDefault();
        const res = await fetch(`/api/Course/submissions/${submissionId}/grade`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                givenGrade: Number(e.target.givenGrade.value),
                comment: e.target.comment.value
            })
        });

        if (res.ok) {
            const updatedSubmission = await res.json();
            setSubmissionsByAssignment(prev => ({
                ...prev,
                [assignmentId]: (prev[assignmentId] || []).map(item =>
                    item.submission?.id === submissionId
                        ? { ...item, submission: updatedSubmission }
                        : item
                )
            }));
            setSavedGradeIds(prev => ({ ...prev, [submissionId]: true }));
            await refreshGrades();
        } else {
            setMessage('Could not save grade.');
        }
    };


    // ---------------------------------STUDENT ASSIGNMENT-----------------------------------------------------

    const handleSubmitAssignment = async (assignment, e) => {
        e.preventDefault();
        const formData = new FormData();
        formData.append('textAnswer', e.target.textAnswer.value);
        if (e.target.file.files?.[0]) formData.append('file', e.target.file.files[0]);

        const res = await fetch(`/api/Course/coursework/${assignment.id}/submission`, {
            method: 'POST',
            credentials: 'include',
            body: formData
        });

        if (res.ok) {
            const submission = await res.json();
            setCourseWorks(prev => prev.map(cw =>
                cw.id === assignment.id ? { ...cw, status: 'completed', submission } : cw
            ));
            await refreshGrades();
            setMessage('Assignment submitted.');
        } else {
            setMessage('Could not submit. Check the deadline and your answer.');
        }
    };

    const handleSubmitNoteSnapshot = async (assignment, e) => {
        e.preventDefault();
        const noteGuid = e.target.noteGuid.value;
        if (!noteGuid) {
            setMessage('Choose a note to submit.');
            return;
        }

        const res = await fetch(`/api/Course/coursework/${assignment.id}/submit-note`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ noteGuid })
        });

        if (res.ok) {
            const submission = await res.json();
            setCourseWorks(prev => prev.map(cw =>
                cw.id === assignment.id ? { ...cw, status: 'completed', submission } : cw
            ));
            await refreshGrades();
            e.target.reset();
            setMessage('Note snapshot submitted.');
        } else {
            setMessage('Could not submit the note. Check the deadline and note access.');
        }
    };

    // ---- START RETURN

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
                {/* Course header: title, teacher name, status, and teacher actions */}
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
                        /* Teacher-only actions: join code, edit, close/reopen, delete */
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
                            <button className="danger-action-btn" onClick={() => setShowDeleteCourseModal(true)}>
                                <Trash2 size={16} />
                                Delete
                            </button>
                        </div>
                    )}
                </div>

                {/* Course message area for success or error feedback */}
                {message && <div className="course-message">{message}</div>}

                {/* Course edit form shown only when the teacher clicks Edit */}
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

                {/* Tab navigation: resources, assignments, grades */}
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
                        <button
                            className={activeTab === 'grades' ? 'tab active' : 'tab'}
                            onClick={() => setActiveTab('grades')}
                        >
                            Grades
                        </button>
                    </div>
                    {activeTab === 'assignments' && canManage && (
                        <button className="upload-btn" onClick={() => { setEditingAssignment(null); setShowAssignModal(true); }}>
                            <Plus size={18} />
                            Create Assignment
                        </button>
                    )}
                </div>

                {/* Resources tab: course files and teacher upload area */}
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
                                        onDelete={() => setResourceToDelete(resource)}
                                    />
                                ))}
                            </div>
                        )}
                    </>
                )}

                {/* Assignments tab: teacher management or student submission panels */}
                {activeTab === 'assignments' && (
                    <div className="assignment-list">
                        {courseWorks.length === 0 ? (
                            <div className="course-empty-state">No assignments yet.</div>
                        ) : (
                            courseWorks.map(assignment => (
                                <CourseAssignmentBox
                                    key={assignment.id}
                                    title={assignment.title}
                                    description={assignment.description}
                                    deadline={formatDate(assignment.deadline)}
                                    weightPercent={assignment.weightPercent}
                                    status={assignment.status}
                                    actionLabel={canManage ? 'View Submissions' : undefined}
                                    onAction={() => loadSubmissions(assignment.id)}
                                >
                                    <div className="assignment-files">
                                        {(assignment.supportingFiles || []).map(file => (
                                            <button
                                                key={file.id}
                                                className="file-link-btn"
                                                onClick={() => { window.location.href = `/api/Course/coursework-files/${file.id}/download`; }}
                                            >
                                                <Download size={14} />
                                                {file.title}
                                            </button>
                                        ))}
                                    </div>

                                    {canManage ? (
                                        <TeacherAssignmentPanel
                                            assignment={assignment}
                                            submissions={submissionsByAssignment[assignment.id]}
                                            onEdit={() => { setEditingAssignment(assignment); setShowAssignModal(true); }}
                                            onUpload={(e) => handleUploadSupportingFile(assignment.id, e)}
                                            onGrade={(submissionId, e) => handleGradeSubmission(submissionId, assignment.id, e)}
                                            savedGradeIds={savedGradeIds}
                                            onOpenSnapshot={setOpenSnapshot}
                                        />
                                    ) : (
                                        <StudentAssignmentPanel
                                            assignment={assignment}
                                            onSubmit={(e) => handleSubmitAssignment(assignment, e)}
                                            onSubmitNote={(e) => handleSubmitNoteSnapshot(assignment, e)}
                                            availableNotes={availableNotes}
                                        />
                                    )}
                                </CourseAssignmentBox>
                            ))
                        )}
                    </div>
                )}

                {/* Grades tab: student grade view or teacher grade overview */}
                {activeTab === 'grades' && (
                    <GradesPanel gradesData={gradesData} canManage={canManage} />
                )}
            </div>

            {/* Assignment create/edit modal */}
            {showAssignModal && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h3>{editingAssignment ? 'Edit Assignment' : 'Create Assignment'}</h3>
                            <button
                                className="modal-close"
                                onClick={() => { setShowAssignModal(false); setEditingAssignment(null); }}
                            >
                                x
                            </button>
                        </div>
                        <form onSubmit={handleCreateOrUpdateAssignment}>
                            <div className="form-group">
                                <label>Assignment Title</label>
                                <input
                                    name="title"
                                    type="text"
                                    required
                                    placeholder="Enter assignment title"
                                    defaultValue={editingAssignment?.title || ''}
                                />
                            </div>
                            <div className="form-group">
                                <label>Description</label>
                                <textarea
                                    name="description"
                                    placeholder="Enter assignment description"
                                    defaultValue={editingAssignment?.description || ''}
                                />
                            </div>
                            <div className="form-group">
                                <label>Due Date</label>
                                <input
                                    name="deadline"
                                    type="date"
                                    required
                                    defaultValue={toDateInput(editingAssignment?.deadline)}
                                />
                            </div>
                            <div className="form-group">
                                <label>Final grade weight (%)</label>
                                <input
                                    name="weightPercent"
                                    type="number"
                                    min="0"
                                    max="100"
                                    step="0.1"
                                    defaultValue={editingAssignment?.weightPercent ?? 0}
                                />
                            </div>
                            <div className="modal-actions">
                                {editingAssignment && (
                                    <button
                                        type="button"
                                        onClick={() => {
                                            setDeleteAssignmentError('');
                                            setAssignmentToDelete(editingAssignment);
                                        }}
                                        className="btn-danger-confirm"
                                    >
                                        Delete
                                    </button>
                                )}
                                <button
                                    type="button"
                                    onClick={() => { setShowAssignModal(false); setEditingAssignment(null); }}
                                    className="btn-cancel"
                                >
                                    Cancel
                                </button>
                                <button type="submit" className="btn-confirm">
                                    Save
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Assignment delete confirmation modal */}
            {assignmentToDelete && (
                <div className="modal-overlay">
                    <div className="modal-content confirm-modal">
                        <div className="modal-header">
                            <h3>Delete assignment?</h3>
                            <button className="modal-close" onClick={() => setAssignmentToDelete(null)} disabled={isDeletingAssignment}>
                                x
                            </button>
                        </div>
                        <p className="confirm-modal-text">
                            Are you sure you want to delete <strong>{assignmentToDelete.title}</strong>? This will remove its submissions, grades, and supporting files.
                        </p>
                        {deleteAssignmentError && (
                            <p className="confirm-modal-error">{deleteAssignmentError}</p>
                        )}
                        <div className="modal-actions">
                            <button
                                type="button"
                                className="btn-cancel"
                                onClick={() => setAssignmentToDelete(null)}
                                disabled={isDeletingAssignment}
                            >
                                Cancel
                            </button>
                            <button
                                type="button"
                                className="btn-danger-confirm"
                                onClick={handleDeleteAssignment}
                                disabled={isDeletingAssignment}
                            >
                                {isDeletingAssignment ? 'Deleting...' : 'Delete assignment'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Resource delete confirmation modal */}
            {resourceToDelete && (
                <div className="modal-overlay">
                    <div className="modal-content confirm-modal">
                        <div className="modal-header">
                            <h3>Delete resource?</h3>
                            <button className="modal-close" onClick={() => setResourceToDelete(null)}>
                                x
                            </button>
                        </div>
                        <p className="confirm-modal-text">
                            Are you sure you want to delete <strong>{resourceToDelete.title}</strong>?
                        </p>
                        <div className="modal-actions">
                            <button type="button" className="btn-cancel" onClick={() => setResourceToDelete(null)}>
                                Cancel
                            </button>
                            <button type="button" className="btn-danger-confirm" onClick={handleDeleteResource}>
                                Delete resource
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Course delete confirmation modal */}
            {showDeleteCourseModal && (
                <div className="modal-overlay">
                    <div className="modal-content confirm-modal">
                        <div className="modal-header">
                            <h3>Delete course?</h3>
                            <button className="modal-close" onClick={() => setShowDeleteCourseModal(false)}>
                                x
                            </button>
                        </div>
                        <p className="confirm-modal-text">
                            Are you sure you want to delete <strong>{course.title}</strong>? This will remove the course and its related course data.
                        </p>
                        <div className="modal-actions">
                            <button type="button" className="btn-cancel" onClick={() => setShowDeleteCourseModal(false)}>
                                Cancel
                            </button>
                            <button type="button" className="btn-danger-confirm" onClick={handleDeleteCourse}>
                                Delete course
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Full submitted answer / note snapshot modal */}
            {openSnapshot && (
                <div className="modal-overlay">
                    <div className="modal-content snapshot-modal">
                        <div className="modal-header">
                            <div>
                                <span className="snapshot-modal-label">{openSnapshot.label || 'Submitted answer'}</span>
                                <h3>{openSnapshot.title || 'Untitled Note'}</h3>
                            </div>
                            <button className="modal-close" onClick={() => setOpenSnapshot(null)}>
                                x
                            </button>
                        </div>
                        <div className="snapshot-reader">
                            {renderSnapshotContent(openSnapshot.content || '')}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

//student submission UI
const StudentAssignmentPanel = ({ assignment, onSubmit, onSubmitNote, availableNotes }) => {
    const submission = assignment.submission;
    const locked = isDeadlineReached(assignment.deadline);
    const textAnswerValue = submission?.noteSnapshot ? '' : submission?.textAnswer || '';

    return (
        <div className="assignment-panel">
            {submission?.grade && (
                <div className="grade-note">
                    Grade: <strong>{submission.grade.givenGrade}</strong>
                    {submission.grade.comment && <span> - {submission.grade.comment}</span>}
                </div>
            )}
            {submission?.hasFile && (
                <button
                    className="file-link-btn"
                    onClick={() => { window.location.href = `/api/Course/submissions/${submission.id}/download`; }}
                >
                    <Download size={14} />
                    Download submitted file
                </button>
            )}
            {submission?.noteSnapshot && (
                <div className="grade-note">
                    Submitted note snapshot: <strong>{submission.noteSnapshot.title || 'Untitled Note'}</strong>
                </div>
            )}
            {!locked ? (
                <>
                    <form className="submission-form" onSubmit={onSubmit}>
                        <textarea
                            name="textAnswer"
                            placeholder={submission?.noteSnapshot ? 'Write a new text answer if you want to replace the note snapshot' : 'Write your answer'}
                            defaultValue={textAnswerValue}
                        />
                        <input name="file" type="file" />
                        <button type="submit" className="upload-btn">
                            {submission ? 'Update Answer' : 'Submit Answer'}
                        </button>
                    </form>
                    <form className="note-snapshot-form" onSubmit={onSubmitNote}>
                        <FileText size={16} />
                        <select name="noteGuid" defaultValue="">
                            <option value="">Choose a note snapshot</option>
                            {(availableNotes || []).map(note => (
                                <option key={`${note.guid}-${note.id}`} value={note.guid}>
                                    {note.title || 'Untitled Note'}
                                </option>
                            ))}
                        </select>
                        <button type="submit" className="secondary-action-btn">
                            Submit Note Snapshot
                        </button>
                    </form>
                </>
            ) : (
                <div className="deadline-reached-state">
                    <button type="button" className="deadline-reached-btn" disabled>
                        Deadline reached
                    </button>
                </div>
            )}
        </div>
    );
};

// teacher tools for each assgnment - edit, upload file, view stud subm, download subm files, save grade&comments
const TeacherAssignmentPanel = ({ submissions, onEdit, onUpload, onGrade, savedGradeIds, onOpenSnapshot }) => {
    return (
        <div className="assignment-panel">
            <div className="assignment-toolbar">
                <button className="secondary-action-btn" onClick={onEdit}>
                    <Pencil size={16} />
                    Edit Assignment
                </button>
                <form className="support-upload-form" onSubmit={onUpload}>
                    <input name="file" type="file" />
                    <button type="submit" className="secondary-action-btn">
                        <Upload size={16} />
                        Upload Supporting File
                    </button>
                </form>
            </div>

            {submissions && (
                <div className="submissions-list">
                    {submissions.length === 0 ? (
                        <div className="course-empty-state">No enrolled students yet.</div>
                    ) : (
                        submissions.map(item => (
                            <div key={item.studentId} className="submission-row">
                                <div>
                                    <strong>{item.studentName}</strong>
                                    <p>{item.email}</p>
                                    <span className={`course-status course-status--${item.status === 'completed' ? 'open' : 'closed'}`}>
                                        {item.status}
                                    </span>
                                </div>
                                {item.submission ? (
                                    <div className="submission-detail">
                                        {item.submission.noteSnapshot && (
                                            <SubmissionAnswerPreview
                                                title={item.submission.noteSnapshot.title || 'Untitled Note'}
                                                content={item.submission.noteSnapshot.content || item.submission.textAnswer || ''}
                                                label="Note snapshot"
                                                actionLabel="Open full snapshot"
                                                onOpen={onOpenSnapshot}
                                            />
                                        )}
                                        {!item.submission.noteSnapshot && (
                                            item.submission.textAnswer ? (
                                                <SubmissionAnswerPreview
                                                    title="Text answer"
                                                    content={item.submission.textAnswer}
                                                    label="Submitted answer"
                                                    actionLabel="Open full answer"
                                                    onOpen={onOpenSnapshot}
                                                />
                                            ) : (
                                                <p>No text answer.</p>
                                            )
                                        )}
                                        {item.submission.hasFile && (
                                            <button
                                                className="file-link-btn"
                                                onClick={() => { window.location.href = `/api/Course/submissions/${item.submission.id}/download`; }}
                                            >
                                                <Download size={14} />
                                                Download file
                                            </button>
                                        )}
                                        <form className="grade-form" onSubmit={(e) => onGrade(item.submission.id, e)}>
                                            <input
                                                name="givenGrade"
                                                type="number"
                                                min="0"
                                                max="10"
                                                step="0.1"
                                                placeholder="Grade"
                                                defaultValue={item.submission.grade?.givenGrade ?? ''}
                                            />
                                            <input
                                                name="comment"
                                                placeholder="Comment"
                                                defaultValue={item.submission.grade?.comment ?? ''}
                                            />
                                            <button className="btn-confirm" type="submit">Save Grade</button>
                                            {savedGradeIds[item.submission.id] && (
                                                <span className="grade-saved-indicator">
                                                    <CheckCircle size={18} />
                                                    Saved
                                                </span>
                                            )}
                                        </form>
                                    </div>
                                ) : (
                                    <div className="missing-submission">No submission</div>
                                )}
                            </div>
                        ))
                    )}
                </div>
            )}
        </div>
    );
};

//shows gerades by role
//students - their final grade and assignment rows
//teacher  - each student with final grade and assignment rows
const GradesPanel = ({ gradesData, canManage }) => {
    if (!gradesData) return <div className="course-empty-state">Loading grades...</div>;

    if (!canManage) {
        return (
            <div className="grades-panel">
                <div className="final-grade-box">
                    Final grade: <strong>{gradesData.finalGrade ?? 'Not available yet'}</strong>
                </div>
                {gradesData.assignments?.map(row => (
                    <GradeRow key={row.assignmentId} row={row} />
                ))}
            </div>
        );
    }

    return (
        <div className="grades-panel">
            {gradesData.students?.length === 0 ? (
                <div className="course-empty-state">No registered students yet.</div>
            ) : (
                gradesData.students?.map(student => (
                    <details key={student.studentId} className="student-grade-card">
                        <summary>
                            <span>{student.studentName}</span>
                            <strong>{student.finalGrade ?? 'No final grade'}</strong>
                        </summary>
                        {student.assignments.map(row => (
                            <GradeRow key={row.assignmentId} row={row} />
                        ))}
                    </details>
                ))
            )}
        </div>
    );
};

//one assignment grade row 
const GradeRow = ({ row }) => (
    <div className="grade-row">
        <div>
            <strong>{row.title}</strong>
            <p>{row.weightPercent}% - {row.status}</p>
            {row.comment && <p>{row.comment}</p>}
        </div>
        <div className="grade-value">
            {row.isGraded ? row.grade : <Flag size={18} />}
        </div>
    </div>
);

export default CoursePage;

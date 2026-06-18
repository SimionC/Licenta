import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { ArrowLeft, Save, Eye, X, Share2, Copy, Folder } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import CollaboratorsSection from '../components/CollaboratorsSection'
import './NoteEditorPage.css';

//for markdown editor
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';
import rehypeKatex from 'rehype-katex';
//+ highlighting
import rehypeHighlight from 'rehype-highlight'
import 'katex/dist/katex.min.css'
import 'highlight.js/styles/github.css'

/**
 * Purpose: Unified create/edit/read page for both personal and collaboration notes.
 * API touched: GET/POST/PUT/DELETE /api/Notes..., GET/DELETE /api/Collaborations...
 * State contract: noteGuid controls new-vs-existing mode; collaborationId controls collaborator panel.
 */

const NoteEditorPage = () => {
    const { noteGuid } = useParams();
    const navigate = useNavigate();
    const location = useLocation();
    const [note, setNote] = useState(null);
    const [title, setTitle] = useState('Untitled Note');
    const [content, setContent] = useState('# Welcome to your new note\n\nStart writing here...');
    const [isEditing, setIsEditing] = useState(false); // Controls editing for title, content, and collaboration
    const [isSaving, setIsSaving] = useState(false);
    const [isNewNote, setIsNewNote] = useState(false);
    const [isPublic, setIsPublic] = useState(false);
    const [fetchError, setFetchError] = useState('')
    const [members, setMembers] = useState([]);
    const [folders, setFolders] = useState([]);
    const [folderId, setFolderId] = useState('');
    const [statusMessage, setStatusMessage] = useState('');
    const [statusTone, setStatusTone] = useState('success');
    const [deleteModalOpen, setDeleteModalOpen] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);

    useEffect(() => {
        fetch('/api/NoteFolders/my-folders', { credentials: 'include' })
            .then(res => res.ok ? res.json() : [])
            .then(data => setFolders(Array.isArray(data) ? data : []))
            .catch(() => setFolders([]));
    }, []);

    // useEffect to fetch note data when noteGuid changes
    useEffect(() => {

        // fetchNote effect: initializes editor for new note or loads existing note + members.
        const fetchNote = async () => {
            if (!noteGuid) {
                const params = new URLSearchParams(location.search);
                // This means it's a brand new note creation (e.g., /notes/new)
                setIsNewNote(true);
                setIsEditing(true); // Start in editing mode for new notes
                setNote(null); // Ensure note is null for new creations
                setTitle('Untitled Note');
                setContent('# Welcome to your new note\\n\\nStart writing here...');
                setIsPublic(false);
                setFolderId(params.get('folderId') || '');
                setMembers([]); // No members for a new non-collaboration note
                setStatusMessage('');
                return;
            }

            try {
                const response = await fetch(`/api/Notes/${noteGuid}`, {
                    credentials: 'include'
                });

                if (response.status === 404) {
                    setFetchError('Note not found or you do not have access.');
                    setNote(null);
                    return;
                }
                if (!response.ok) {
                    throw new Error(`Failed to fetch note: ${response.statusText}`);
                }

                const data = await response.json();
                setNote(data);
                setTitle(data.title);
                setContent(data.content);
                setIsPublic(data.isPublic);
                setFolderId(data.folderId ? String(data.folderId) : '');
                setIsNewNote(false); // It's an existing note
                setStatusMessage('');

                // If it's a collaboration note, set members
                if (data.collaborationId) {
                    // Fetch members for collaboration notes
                    const membersRes = await fetch(`/api/Collaborations/${data.collaborationId}`, {
                        credentials: 'include'
                    });
                    if (membersRes.ok) {
                        const membersData = await membersRes.json();
                        setMembers(membersData.members || []);
                    } else {
                        console.error('Failed to fetch collaboration members');
                    }
                } else {
                    setMembers([]); // Not a collaboration note, clear members
                }

            } catch (error) {
                console.error('Error fetching note:', error);
                setFetchError(`Error loading note: ${error.message}`);
                setNote(null);
            }
        };

        fetchNote();
    }, [noteGuid, location.search]);

    //const handlePublicToggleChange = () => {
    //    setIsPublic(prev => !prev);
    //};

    // handleSave: builds payload, selects POST vs PUT, then syncs local state and route.
    const showStatus = (message, tone = 'success') => {
        setStatusMessage(message);
        setStatusTone(tone);
    };

    const copyPublicLink = () => {
        if (!note) return;
        navigator.clipboard
            .writeText(`${window.location.origin}/notes/${note.guid}`)
            .then(() => showStatus('Link copied.'))
            .catch(() => showStatus('Could not copy link.', 'error'));
    };

    const handleSave = async () => {
        setIsSaving(true);
        setFetchError('');
        setStatusMessage('');

        // Ensure content is not null when sending
        const payload = {
            title: title,
            content: content || '', // Ensure content is not null
            isPublic: isPublic,
            folderId: note?.collaborationId ? null : (folderId ? Number(folderId) : null),
            // Include collaborationId in the payload ONLY if the 'note' state already has one.
            // This preserves it on updates.
            ...(note?.collaborationId && { collaborationId: note.collaborationId })
        };

        const url = isNewNote ? '/api/Notes/create' : `/api/Notes/${noteGuid}`;
        const method = isNewNote ? 'POST' : 'PUT';

        try {
            const res = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(payload),
                credentials: 'include'
            });

            if (!res.ok) {
                const errorData = await res.json();
                throw new Error(errorData.message || 'Failed to save note');
            }

            const savedNote = await res.json();
            setNote(savedNote);
            setFolderId(savedNote.folderId ? String(savedNote.folderId) : '');
            setIsNewNote(false); // No longer a new note once saved
            setIsEditing(false); // Exit editing mode after saving

            if (isNewNote) {
                // If it was a new note, navigate to its URL
                navigate(`/notes/${savedNote.guid}`);
            }

            showStatus('Note saved successfully.');
        } catch (err) {
            console.error('Error saving note:', err);
            showStatus(err.message || 'Error saving note.', 'error');
        } finally {
            setIsSaving(false);
        }
    };

    // handleDelete: deletes note; for collaboration notes also deletes collaboration container.
    const handleDelete = async () => {
        if (!note || isNewNote) return;

        setIsDeleting(true);
        setStatusMessage('');
        try {
            //1. delete the note
            let response = await fetch(`/api/Notes/${noteGuid}`, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (!response.ok) throw new Error('Failed to delete note');

            // 2️.if it was a collaboration note, delete the collaboration (server will remove members)
            if (note.collaborationId) {
                response = await fetch(
                    `/api/Collaborations/${note.collaborationId}`,
                    { method: 'DELETE', credentials: 'include' }
                );
                if (!response.ok) throw new Error('Failed to delete collaboration');
            }

            navigate('/notes');
        } catch (error) {
            console.error('Error deleting note:', error);
            showStatus('Failed to delete note. Please try again.', 'error');
            setDeleteModalOpen(false);
        } finally {
            setIsDeleting(false);
        }
    };

    // handleCancel: restores loaded values (existing) or exits to notes list (new).
    const handleCancel = () => {
        if (isNewNote) {
            navigate('/notes');
        } else {
            // Revert to original note details if existing note
            setTitle(note.title);
            setContent(note.content);
            setIsPublic(note.isPublic);
            setFolderId(note.folderId ? String(note.folderId) : '');
            setIsEditing(false); // Exit editing mode
            // If collaborators were modified, you might want to re-fetch them here or store original state
        }
    };

    if (fetchError) {
        return (
            <div className="note-error">
                <p>{fetchError}</p>
                <button onClick={() => window.history.back()}>
                    Go Back
                </button>
            </div>
        )
    }

    //NoteEditorPage
    return (
        <div className="note-editor-page">
            <Sidebar />
            <div className="note-editor-main">
                {/* Header */}
                <div className="note-editor-header">
                    <div className="note-editor-header-left">
                        <button
                            className="note-back-btn"
                            onClick={() => navigate('/notes')}
                        >
                            <ArrowLeft size={20} />
                        </button>
                        <div className="note-title-section">
                            {isEditing ? (
                                <input
                                    type="text"
                                    className="note-title-input"
                                    value={title}
                                    onChange={(e) => setTitle(e.target.value)}
                                    placeholder="Note title"
                                />
                            ) : (
                                <h1 className="note-title">{title}</h1>
                            )}
                        </div>
                    </div>

                    <div className="note-editor-actions">
                        {isEditing ? (
                            <>
                                <button
                                    className="note-action-btn save-btn"
                                    onClick={handleSave}
                                    disabled={isSaving}
                                >
                                    <Save size={16} />
                                    {isSaving ? 'Saving...' : 'Save'}
                                </button>
                                <button
                                    className="note-action-btn cancel-btn"
                                    onClick={handleCancel}
                                >
                                    <X size={16} />
                                    Cancel
                                </button>
                            </>
                        ) : (
                            <>
                                {/* Only show edit/delete if not a new note, and not currently editing */}
                                {!isNewNote && (
                                    <>
                                        <button
                                            className="note-action-btn delete-btn"
                                            onClick={() => setDeleteModalOpen(true)}
                                        >
                                            Delete
                                        </button>
                                    </>
                                )}
                            </>
                        )}
                    </div>
                </div>

                {statusMessage && (
                    <div className={`note-status-message ${statusTone === 'error' ? 'error' : ''}`}>
                        {statusMessage}
                    </div>
                )}

                {!note?.collaborationId && (
                    <div className="note-folder-bar">
                        <Folder size={18} />
                        <label>
                            Folder
                            <select
                                value={folderId}
                                onChange={(e) => setFolderId(e.target.value)}
                                disabled={!isEditing}
                            >
                                <option value="">No Folder</option>
                                {folders.map(folder => (
                                    <option key={folder.id} value={folder.id}>
                                        {folder.name}
                                    </option>
                                ))}
                            </select>
                        </label>
                    </div>
                )}

                {/* ─── Main Container ─── */}
                <div className="note-main-flex">

                    {/* Content */}
                    <div className="note-content-section">
                        <div className="note-content-header">
                            <h3>Content</h3>
                            {!isEditing && ( // Only show "Edit Note" button when not in editing mode
                                <button
                                    className="note-edit-content-btn"
                                    onClick={() => setIsEditing(true)}
                                >
                                    <Eye size={16} />
                                    Edit Note
                                </button>
                            )}
                        </div>

                        <div className="note-content-box">
                            {isEditing ? (
                                <textarea
                                    className="note-content-textarea"
                                    value={content}
                                    onChange={(e) => setContent(e.target.value)}
                                    placeholder="Start writing your note here..."
                                />
                            ) : (
                                <div className="note-content-display">
                                    {content ? (
                                        <ReactMarkdown
                                            children={content}
                                            remarkPlugins={[remarkGfm, remarkMath]}
                                            rehypePlugins={[
                                                rehypeKatex,
                                                rehypeHighlight,
                                            ]}
                                        />
                                    ) : (
                                        <p className="note-content-placeholder">
                                            This note is empty. Click "Edit Note" to start writing.
                                        </p>
                                    )}
                                </div>
                            )}
                        </div>
                    </div>

                    {/* ─── Sharing Permissions ─── */}
                    <div className="note-sharing-section">
                        <h3 className="sharing-header">
                            <Share2 size={20} />
                            Sharing Permissions
                            {note && isPublic && (
                                <button
                                    className="copy-link-icon"
                                    onClick={copyPublicLink}
                                >
                                    <Copy size={16} />
                                </button>
                            )}
                        </h3>

                        {isEditing && ( // Only show public toggle when in editing mode
                            <label className="note-sharing-toggle">
                                <input
                                    type="checkbox"
                                    checked={isPublic}
                                    onChange={() => setIsPublic(v => !v)}
                                />
                                Make Public {' '}
                                (anyone with link can view)
                            </label>
                        )}

                        {/* ──────── COLLABORATORS BOX ───────── */}
                        {note?.collaborationId && (
                            <CollaboratorsSection
                                collaborationId={note.collaborationId}
                                collaborators={members}
                                setCollaborators={setMembers}
                                // Pass isEditing from parent to control CollaboratorsSection's internal edit state
                                parentIsEditing={isEditing}
                            />
                        )}

                        {note && isPublic && (
                            <div className="note-sharing-link">
                                <input
                                    type="text"
                                    readOnly
                                    value={`${window.location.origin}/notes/${note.guid}`}
                                />
                                <button
                                    onClick={copyPublicLink}
                                >
                                    Copy Link
                                </button>
                            </div>
                        )}
                    </div>
                </div>

            </div>

            {deleteModalOpen && (
                <div className="note-modal-overlay">
                    <div className="note-confirm-modal">
                        <div className="note-confirm-header">
                            <h3>Delete note?</h3>
                            <button onClick={() => setDeleteModalOpen(false)} disabled={isDeleting}>
                                <X size={18} />
                            </button>
                        </div>
                        <p>
                            Are you sure you want to permanently delete <strong>{title}</strong>? This cannot be undone.
                        </p>
                        <div className="note-confirm-actions">
                            <button
                                className="note-confirm-cancel"
                                onClick={() => setDeleteModalOpen(false)}
                                disabled={isDeleting}
                            >
                                Cancel
                            </button>
                            <button
                                className="note-confirm-danger"
                                onClick={handleDelete}
                                disabled={isDeleting}
                            >
                                {isDeleting ? 'Deleting...' : 'Delete permanently'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default NoteEditorPage;

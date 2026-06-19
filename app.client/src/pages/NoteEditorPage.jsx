import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { ArrowLeft, Save, Eye, X, Share2, Folder, UserPlus, Trash2, Download } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import CollaboratorsSection from '../components/CollaboratorsSection'
import './NoteEditorPage.css';
import './CollaborationStyles.css';

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
    const { noteGuid, collabId } = useParams();
    const navigate = useNavigate();
    const location = useLocation();
    const routeCollaborationId = collabId ? Number(collabId) : null;
    const [note, setNote] = useState(null);
    const [collaboration, setCollaboration] = useState(null);
    const [title, setTitle] = useState('Untitled Note');
    const [content, setContent] = useState('# Welcome to your new note\n\nStart writing here...');
    const [isEditing, setIsEditing] = useState(false); // Controls editing for title, content, and collaboration
    const [isPreviewing, setIsPreviewing] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [isNewNote, setIsNewNote] = useState(false);
    const [fetchError, setFetchError] = useState('')
    const [members, setMembers] = useState([]);
    const [folders, setFolders] = useState([]);
    const [folderId, setFolderId] = useState('');
    const [statusMessage, setStatusMessage] = useState('');
    const [statusTone, setStatusTone] = useState('success');
    const [deleteModalOpen, setDeleteModalOpen] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);
    const [shareModalOpen, setShareModalOpen] = useState(false);
    const [permissions, setPermissions] = useState([]);
    const [shareEmail, setShareEmail] = useState('');
    const [shareRole, setShareRole] = useState('viewer');
    const [isSharing, setIsSharing] = useState(false);
    const [isDownloading, setIsDownloading] = useState(false);

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
            setFetchError('');
            if (!noteGuid) {
                const params = new URLSearchParams(location.search);
                if (routeCollaborationId) {
                    const collabRes = await fetch(`/api/Collaborations/${routeCollaborationId}`, {
                        credentials: 'include'
                    });

                    if (!collabRes.ok) {
                        setFetchError('This collaboration is no longer available, or you no longer have access to it.');
                        return;
                    }

                    const collabData = await collabRes.json();
                    const role = collabData.myRole || 'viewer';
                    if (role !== 'owner' && role !== 'editor') {
                        setFetchError('You have view-only access to this collaboration, so you cannot create notes in it.');
                        return;
                    }

                    setCollaboration(collabData);
                    setMembers(collabData.members || []);
                } else {
                    setCollaboration(null);
                    setMembers([]);
                }

                // This means it's a brand new note creation (e.g., /notes/new or /collaborations/:id/notes/new)
                setIsNewNote(true);
                setIsEditing(true); // Start in editing mode for new notes
                setIsPreviewing(false);
                setNote(null); // Ensure note is null for new creations
                setTitle(routeCollaborationId ? 'Untitled Collaboration Note' : 'Untitled Note');
                setContent(routeCollaborationId ? '# New collaboration note\\n\\nStart writing here...' : '# Welcome to your new note\\n\\nStart writing here...');
                setFolderId(routeCollaborationId ? '' : (params.get('folderId') || ''));
                setStatusMessage('');
                setPermissions([]);
                return;
            }

            try {
                const response = await fetch(`/api/Notes/${noteGuid}`, {
                    credentials: 'include'
                });

                if (response.status === 404) {
                    setFetchError('This note is no longer available. The owner may have deleted it or removed your access.');
                    setNote(null);
                    return;
                }
                if (response.status === 403) {
                    setFetchError('You no longer have access to this note.');
                    setNote(null);
                    return;
                }
                if (!response.ok) {
                    throw new Error(`Failed to fetch note: ${response.statusText}`);
                }

                const data = await response.json();
                if (routeCollaborationId && data.collaborationId !== routeCollaborationId) {
                    setFetchError('This note does not belong to this collaboration.');
                    setNote(null);
                    return;
                }

                setNote(data);
                setTitle(data.title);
                setContent(data.content);
                setFolderId(data.folderId ? String(data.folderId) : '');
                setIsNewNote(false); // It's an existing note
                setIsPreviewing(false);
                setStatusMessage('');

                if (data.canManageSharing && !data.collaborationId) {
                    await loadPermissions(data.guid);
                } else {
                    setPermissions([]);
                }

                // If it's a collaboration note, set members
                if (data.collaborationId) {
                    // Fetch members for collaboration notes
                    const membersRes = await fetch(`/api/Collaborations/${data.collaborationId}`, {
                        credentials: 'include'
                    });
                    if (membersRes.ok) {
                        const membersData = await membersRes.json();
                        setCollaboration(membersData);
                        setMembers(membersData.members || []);
                    } else {
                        console.error('Failed to fetch collaboration members');
                    }
                } else {
                    setCollaboration(null);
                    setMembers([]); // Not a collaboration note, clear members
                }

            } catch (error) {
                console.error('Error fetching note:', error);
                setFetchError(`Error loading note: ${error.message}`);
                setNote(null);
            }
        };

        fetchNote();
    }, [noteGuid, location.search, routeCollaborationId]);

    // handleSave: builds payload, selects POST vs PUT, then syncs local state and route.
    const showStatus = (message, tone = 'success') => {
        setStatusMessage(message);
        setStatusTone(tone);
    };

    const readApiMessage = async (res, fallback) => {
        try {
            const data = await res.json();
            return data.message || fallback;
        } catch {
            return fallback;
        }
    };

    const loadPermissions = async (guid) => {
        const res = await fetch(`/api/Notes/${guid}/permissions`, { credentials: 'include' });
        if (!res.ok) {
            setPermissions([]);
            return;
        }

        const data = await res.json();
        setPermissions(Array.isArray(data) ? data : []);
    };

    const handleAddPermission = async (e) => {
        e.preventDefault();
        if (!note || !shareEmail.trim()) return;

        setIsSharing(true);
        setStatusMessage('');
        try {
            const res = await fetch(`/api/Notes/${note.guid}/permissions`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ email: shareEmail.trim(), role: shareRole })
            });

            if (!res.ok) throw new Error(await readApiMessage(res, 'Could not share note.'));

            const permission = await res.json();
            setPermissions(prev => [...prev, permission]);
            setShareEmail('');
            setShareRole('viewer');
            showStatus('Access added.');
        } catch (err) {
            showStatus(err.message || 'Could not share note.', 'error');
        } finally {
            setIsSharing(false);
        }
    };

    const handlePermissionRoleChange = async (permissionId, role) => {
        if (!note) return;

        setStatusMessage('');
        try {
            const res = await fetch(`/api/Notes/${note.guid}/permissions/${permissionId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ role })
            });

            if (!res.ok) throw new Error(await readApiMessage(res, 'Could not update access.'));

            const updatedPermission = await res.json();
            setPermissions(prev => prev.map(permission =>
                permission.id === permissionId ? updatedPermission : permission
            ));
            showStatus('Access updated.');
        } catch (err) {
            showStatus(err.message || 'Could not update access.', 'error');
        }
    };

    const handleRemovePermission = async (permissionId) => {
        if (!note) return;

        setStatusMessage('');
        try {
            const res = await fetch(`/api/Notes/${note.guid}/permissions/${permissionId}`, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (!res.ok) throw new Error(await readApiMessage(res, 'Could not remove access.'));

            setPermissions(prev => prev.filter(permission => permission.id !== permissionId));
            showStatus('Access removed.');
        } catch (err) {
            showStatus(err.message || 'Could not remove access.', 'error');
        }
    };

    const handleSave = async () => {
        if (!canEditNote) {
            showStatus('You have view-only access to this note.', 'error');
            return;
        }

        setIsSaving(true);
        setFetchError('');
        setStatusMessage('');

        // Ensure content is not null when sending
        const activeCollaborationId = note?.collaborationId || routeCollaborationId;
        const payload = {
            title: title,
            content: content || '', // Ensure content is not null
            folderId: activeCollaborationId ? null : (folderId ? Number(folderId) : null),
            ...(activeCollaborationId && { collaborationId: activeCollaborationId })
        };

        const url = isNewNote && routeCollaborationId
            ? `/api/Collaborations/${routeCollaborationId}/notes`
            : isNewNote ? '/api/Notes/create' : `/api/Notes/${noteGuid}`;
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
                throw new Error(await readApiMessage(res, 'Failed to save note'));
            }

            const savedNote = await res.json();
            setNote(savedNote);
            setFolderId(savedNote.folderId ? String(savedNote.folderId) : '');
            setIsNewNote(false); // No longer a new note once saved
            setIsEditing(false); // Exit editing mode after saving
            setIsPreviewing(false);

            if (isNewNote) {
                // If it was a new note, navigate to its URL
                if (savedNote.collaborationId) {
                    navigate(`/collaborations/${savedNote.collaborationId}/notes/${savedNote.guid}`);
                } else {
                    navigate(`/notes/${savedNote.guid}`);
                }
            }

            showStatus('Note saved successfully.');
        } catch (err) {
            console.error('Error saving note:', err);
            showStatus(err.message || 'Error saving note.', 'error');
        } finally {
            setIsSaving(false);
        }
    };

    // handleDelete: deletes only this note. Collaboration notes leave their workspace intact.
    const handleDelete = async () => {
        if (!note || isNewNote) return;

        setIsDeleting(true);
        setStatusMessage('');
        try {
            const response = await fetch(`/api/Notes/${noteGuid}`, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (!response.ok) throw new Error('Failed to delete note');

            if (note.collaborationId) {
                navigate(`/collaborations/${note.collaborationId}`);
            } else {
                navigate('/notes');
            }
        } catch (error) {
            console.error('Error deleting note:', error);
            showStatus('Failed to delete note. Please try again.', 'error');
            setDeleteModalOpen(false);
        } finally {
            setIsDeleting(false);
        }
    };

    const handleDownload = async () => {
        if (!note?.guid || isNewNote) return;

        setIsDownloading(true);
        setStatusMessage('');
        try {
            const res = await fetch(`/api/Notes/${note.guid}/download?format=md`, {
                credentials: 'include'
            });

            if (!res.ok) {
                throw new Error(await readApiMessage(res, 'Could not download note.'));
            }

            const blob = await res.blob();
            const contentDisposition = res.headers.get('content-disposition') || '';
            const filenameMatch = contentDisposition.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i);
            const fallbackName = `${(title || 'note').replace(/[\\/:*?"<>|]+/g, '_')}.md`;
            const filename = filenameMatch
                ? decodeURIComponent(filenameMatch[1].replace(/"/g, ''))
                : fallbackName;

            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.URL.revokeObjectURL(url);
            showStatus('Note downloaded.');
        } catch (err) {
            showStatus(err.message || 'Could not download note.', 'error');
        } finally {
            setIsDownloading(false);
        }
    };

    // handleCancel: restores loaded values (existing) or exits to notes list (new).
    const handleCancel = () => {
        if (isNewNote) {
            navigate(routeCollaborationId ? `/collaborations/${routeCollaborationId}` : '/notes');
        } else {
            // Revert to original note details if existing note
            setTitle(note.title);
            setContent(note.content);
            setFolderId(note.folderId ? String(note.folderId) : '');
            setIsEditing(false); // Exit editing mode
            setIsPreviewing(false);
            // If collaborators were modified, you might want to re-fetch them here or store original state
        }
    };

    if (fetchError) {
        return (
            <div className="note-editor-page">
                <Sidebar />
                <div className="note-editor-main">
                    <div className="note-unavailable-card">
                        <div className="note-unavailable-icon">
                            <Share2 size={24} />
                        </div>
                        <h2>Note unavailable</h2>
                        <p>{fetchError}</p>
                        <div className="note-unavailable-actions">
                            <button className="note-action-btn save-btn" onClick={() => navigate('/notes')}>
                                Back to notes
                            </button>
                            <button className="note-action-btn cancel-btn" onClick={() => window.location.reload()}>
                                Reload
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        )
    }

    const accessRole = note?.accessRole || (isNewNote ? 'owner' : 'viewer');
    const canEditNote = isNewNote || note?.canEdit === true;
    const canManageSharing = !isNewNote && note?.canManageSharing === true && !note?.collaborationId;
    const canUseOwnerControls = isNewNote || canManageSharing;
    const isDirectSharedNote = !isNewNote && !note?.collaborationId && accessRole !== 'owner';
    const canOpenSharePanel = !isNewNote && (canManageSharing || isDirectSharedNote || note?.collaborationId);
    const activeCollaborationId = note?.collaborationId || routeCollaborationId;
    const canManageFolder = canUseOwnerControls && !activeCollaborationId;
    const canDeleteNote = !isNewNote && (canManageSharing || (note?.collaborationId && canEditNote));
    const backTarget = activeCollaborationId ? `/collaborations/${activeCollaborationId}` : '/notes';
    const currentFolderName = folderId
        ? folders.find(folder => String(folder.id) === String(folderId))?.name || 'Folder'
        : 'No Folder';

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
                            onClick={() => navigate(backTarget)}
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
                                <>
                                    <h1 className="note-title">{title}</h1>
                                    {note?.collaborationId && (
                                        <div className="note-collaboration-context">
                                            <span>{collaboration?.name || 'Collaboration workspace'}</span>
                                            <strong>{accessRole}</strong>
                                        </div>
                                    )}
                                </>
                            )}
                        </div>
                    </div>

                    <div className="note-editor-actions">
                        {isEditing ? (
                            <>
                                {canManageFolder && (
                                    <div className="note-header-folder">
                                        <Folder size={15} />
                                        <select
                                            value={folderId}
                                            onChange={(e) => setFolderId(e.target.value)}
                                        >
                                            <option value="">No Folder</option>
                                            {folders.map(folder => (
                                                <option key={folder.id} value={folder.id}>
                                                    {folder.name}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                )}
                                <button
                                    className={`note-action-btn preview-btn ${isPreviewing ? 'active' : ''}`}
                                    onClick={() => setIsPreviewing(prev => !prev)}
                                >
                                    <Eye size={16} />
                                    {isPreviewing ? 'Write' : 'Preview'}
                                </button>
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
                                {canManageFolder && (
                                    <div className="note-folder-readonly">
                                        <Folder size={15} />
                                        <span>{currentFolderName}</span>
                                    </div>
                                )}
                                {canOpenSharePanel && (
                                    <button
                                        className="note-action-btn share-btn"
                                        onClick={() => setShareModalOpen(true)}
                                    >
                                        <Share2 size={16} />
                                        {note?.collaborationId
                                            ? 'Collaboration'
                                            : canManageSharing
                                                ? `Share${permissions.length ? ` (${permissions.length})` : ''}`
                                            : 'Shared'}
                                    </button>
                                )}
                                {!isNewNote && note?.guid && (
                                    <button
                                        className="note-action-btn"
                                        onClick={handleDownload}
                                        disabled={isDownloading}
                                    >
                                        <Download size={16} />
                                        {isDownloading ? 'Downloading...' : 'Download'}
                                    </button>
                                )}
                                {/* Only show edit/delete if not a new note, and not currently editing */}
                                {canDeleteNote && (
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

                {/* ─── Main Container ─── */}
                <div className="note-main-flex">

                    {/* Content */}
                    <div className="note-content-section">
                        <div className="note-content-header">
                            <h3>Content</h3>
                            {!isEditing && canEditNote && ( // Only show "Edit Note" button when user can edit
                                <button
                                    className="note-edit-content-btn"
                                    onClick={() => {
                                        setIsPreviewing(false);
                                        setIsEditing(true);
                                    }}
                                >
                                    <Eye size={16} />
                                    Edit Note
                                </button>
                            )}
                            {!canEditNote && (
                                <span className="note-readonly-pill">View only</span>
                            )}
                        </div>

                        <div className="note-content-box">
                            {isEditing && !isPreviewing ? (
                                <textarea
                                    className="note-content-textarea"
                                    value={content}
                                    onChange={(e) => setContent(e.target.value)}
                                    placeholder="Start writing your note here..."
                                />
                            ) : (
                                <div className={`note-content-display ${isEditing ? 'previewing' : ''}`}>
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
                            {note?.collaborationId
                                ? <>Remove <strong>{title}</strong> from this collaboration? The workspace and other notes will stay available.</>
                                : <>Are you sure you want to permanently delete <strong>{title}</strong>? This cannot be undone.</>}
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
                                {isDeleting ? 'Deleting...' : note?.collaborationId ? 'Remove note' : 'Delete permanently'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {shareModalOpen && (
                <div className="note-modal-overlay">
                    <div className="note-share-modal">
                        <div className="note-confirm-header">
                            <div>
                                <h3>{note?.collaborationId ? 'Workspace members' : 'Share note'}</h3>
                                <p className="note-modal-subtitle">
                                    {note?.collaborationId
                                        ? `Members of ${collaboration?.name || 'this collaboration'} can access notes based on their workspace role.`
                                        : canManageSharing
                                            ? 'Invite registered users and choose what they can do.'
                                            : 'This note was shared with you.'}
                                </p>
                            </div>
                            <button onClick={() => setShareModalOpen(false)}>
                                <X size={18} />
                            </button>
                        </div>

                        {note?.collaborationId ? (
                            <CollaboratorsSection
                                collaborationId={note.collaborationId}
                                collaborators={members}
                                setCollaborators={setMembers}
                                parentIsEditing={collaboration?.myRole === 'owner'}
                            />
                        ) : canManageSharing ? (
                            <>
                                <form className="note-share-form modal-form" onSubmit={handleAddPermission}>
                                    <input
                                        type="email"
                                        value={shareEmail}
                                        onChange={(e) => setShareEmail(e.target.value)}
                                        placeholder="User email"
                                    />
                                    <select value={shareRole} onChange={(e) => setShareRole(e.target.value)}>
                                        <option value="viewer">Viewer</option>
                                        <option value="editor">Editor</option>
                                    </select>
                                    <button type="submit" disabled={isSharing}>
                                        <UserPlus size={15} />
                                        {isSharing ? 'Adding...' : 'Add'}
                                    </button>
                                </form>

                                <div className="note-permission-list modal-list">
                                    {permissions.length === 0 ? (
                                        <p className="note-sharing-muted">No one has direct access yet.</p>
                                    ) : permissions.map(permission => (
                                        <div className="note-permission-row" key={permission.id}>
                                            <div>
                                                <strong>{permission.displayName || permission.email}</strong>
                                                <span>{permission.email}</span>
                                            </div>
                                            <select
                                                value={permission.role}
                                                onChange={(e) => handlePermissionRoleChange(permission.id, e.target.value)}
                                            >
                                                <option value="viewer">Viewer</option>
                                                <option value="editor">Editor</option>
                                            </select>
                                            <button
                                                type="button"
                                                title="Remove access"
                                                onClick={() => handleRemovePermission(permission.id)}
                                            >
                                                <Trash2 size={15} />
                                            </button>
                                        </div>
                                    ))}
                                </div>
                            </>
                        ) : (
                            <p className="note-sharing-muted shared-note-message">
                                {accessRole === 'editor'
                                    ? 'You can edit the title and content, but only the owner can manage sharing.'
                                    : 'You have view-only access. Only the owner can manage sharing.'}
                            </p>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
};

export default NoteEditorPage;

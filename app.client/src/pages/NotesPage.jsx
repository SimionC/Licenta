import React, { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import NotesNoteCard from '../components/NotesNoteCard';
import {Clock, Folder,FolderPlus,Inbox,Pencil,Plus,Search,Trash2,UserPlus,UserRoundCheck,Users,X} from 'lucide-react';
import '../components/NotesNoteCard.css';

/**
 * Purpose: Personal notes workspace with folders, filtering, and search.
 */

const VIEW_RECENT = 'recent';
const VIEW_NO_FOLDER = 'no-folder';
const VIEW_SHARED = 'shared';

const NotesPage = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const removedCollaborationId = location.state?.removedCollaborationId;
    const workspaceAction = location.state?.workspaceAction;
    const [notes, setNotes] = useState([]);
    const [recentNotes, setRecentNotes] = useState([]);
    const [sharedNotes, setSharedNotes] = useState([]);
    const [folders, setFolders] = useState([]);
    const [collaborations, setCollaborations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedView, setSelectedView] = useState(VIEW_RECENT);
    const [selectedFolderId, setSelectedFolderId] = useState(null);
    const [searchTerm, setSearchTerm] = useState('');
    const [newFolderName, setNewFolderName] = useState('');
    const [editingFolderId, setEditingFolderId] = useState(null);
    const [editingFolderName, setEditingFolderName] = useState('');
    const [folderToDelete, setFolderToDelete] = useState(null);
    const [collaborationModalOpen, setCollaborationModalOpen] = useState(false);
    const [collaborationName, setCollaborationName] = useState('');
    const [collaborationMembers, setCollaborationMembers] = useState([]);
    const [collaborationEmail, setCollaborationEmail] = useState('');
    const [collaborationRole, setCollaborationRole] = useState('viewer');
    const [collaborationModalMessage, setCollaborationModalMessage] = useState('');
    const [isCreatingCollaboration, setIsCreatingCollaboration] = useState(false);
    const [message, setMessage] = useState('');
    const [recentMessage, setRecentMessage] = useState('');

    // ---------------------------------WORKSPACE DATA LOADING-----------------------------------------------

    const loadWorkspace = async () => {
        setLoading(true);
        setMessage('');
        setRecentMessage('');

        const loadJson = async (url, errorMessage) => {
            try {
                const res = await fetch(url, { credentials: 'include' });
                if (!res.ok) throw new Error(errorMessage);

                const data = await res.json();
                return { data: Array.isArray(data) ? data : [], error: '' };
            } catch (err) {
                console.error(`Error loading ${url}:`, err);
                return { data: [], error: errorMessage };
            }
        };

        const results = await Promise.allSettled([
            loadJson('/api/Notes/my-notes', 'Could not load personal notes.'),
            loadJson('/api/Notes/accessible-notes', 'Could not load recent notes.'),
            loadJson('/api/NoteFolders/my-folders', 'Could not load folders.'),
            loadJson('/api/Collaborations/my-collaborations', 'Could not load collaborations.'),
            loadJson('/api/Notes/shared-with-me', 'Could not load shared notes.')
        ]);

        const unwrap = (result, fallbackError) => (
            result.status === 'fulfilled'
                ? result.value
                : { data: [], error: fallbackError }
        );

        const personal = unwrap(results[0], 'Could not load personal notes.');
        const recent = unwrap(results[1], 'Could not load recent notes.');
        const folderData = unwrap(results[2], 'Could not load folders.');
        const collaborationData = unwrap(results[3], 'Could not load collaborations.');
        const shared = unwrap(results[4], 'Could not load shared notes.');

        const removeStaleCollaborationItems = (items) => removedCollaborationId
            ? items.filter(item => item.collaborationId !== removedCollaborationId)
            : items;

        setNotes(removeStaleCollaborationItems(personal.data));
        setRecentNotes(removeStaleCollaborationItems(recent.data));
        setFolders(folderData.data);
        setCollaborations(removedCollaborationId
            ? collaborationData.data.filter(collaboration => collaboration.id !== removedCollaborationId)
            : collaborationData.data);
        setSharedNotes(removeStaleCollaborationItems(shared.data));
        setRecentMessage(recent.error);

        const workspaceErrors = [
            personal.error,
            folderData.error,
            collaborationData.error,
            shared.error
        ].filter(Boolean);

        const actionMessage = workspaceAction === 'leave'
            ? 'You left the collaboration.'
            : workspaceAction === 'delete'
                ? 'Collaboration deleted.'
                : '';

        setMessage([actionMessage, ...workspaceErrors].filter(Boolean).join(' '));
        setLoading(false);
    };

    useEffect(() => {
        loadWorkspace();
    }, [removedCollaborationId, workspaceAction]);

    // ---------------------------------DERIVED NOTE DATA / FILTERING-----------------------------------------

    const folderNameById = useMemo(() => {
        return folders.reduce((acc, folder) => {
            acc[folder.id] = folder.name;
            return acc;
        }, {});
    }, [folders]);

    const selectedFolder = selectedFolderId
        ? folders.find(folder => folder.id === selectedFolderId)
        : null;

    const filteredNotes = useMemo(() => {
        const normalizedSearch = searchTerm.trim().toLowerCase();
        let nextNotes = selectedView === VIEW_SHARED
            ? [...sharedNotes]
            : selectedView === VIEW_RECENT
                ? [...recentNotes]
                : [...notes];

        if (selectedView === VIEW_RECENT) {
            nextNotes = nextNotes.slice(0, 10);
        } else if (selectedView === VIEW_NO_FOLDER) {
            nextNotes = nextNotes.filter(note => !note.folderId && !note.collaborationId);
        } else if (selectedFolderId) {
            nextNotes = nextNotes.filter(note => note.folderId === selectedFolderId && !note.collaborationId);
        }

        if (normalizedSearch) {
            nextNotes = nextNotes.filter(note => {
                const title = (note.title || '').toLowerCase();
                const content = (note.content || '').toLowerCase();
                return title.includes(normalizedSearch) || content.includes(normalizedSearch);
            });
        }

        return nextNotes;
    }, [notes, recentNotes, sharedNotes, searchTerm, selectedFolderId, selectedView]);

    // ---------------------------------VIEW / NOTE NAVIGATION------------------------------------------------

    const selectView = (view) => {
        setSelectedView(view);
        setSelectedFolderId(null);
    };

    const selectFolder = (folderId) => {
        setSelectedView('folder');
        setSelectedFolderId(folderId);
    };

    const handleCreateNote = () => {
        const query = selectedFolderId ? `?folderId=${selectedFolderId}` : '';
        navigate(`/notes/new${query}`);
    };

    // ---------------------------------COLLABORATION CREATION------------------------------------------------

    const resetCollaborationModal = () => {
        setCollaborationName('');
        setCollaborationMembers([]);
        setCollaborationEmail('');
        setCollaborationRole('viewer');
        setCollaborationModalMessage('');
    };

    const closeCollaborationModal = () => {
        setCollaborationModalOpen(false);
        resetCollaborationModal();
    };

    const handleAddCollaborationMember = () => {
        const email = collaborationEmail.trim().toLowerCase();
        if (!email) return;

        if (!email.includes('@')) {
            setCollaborationModalMessage('Please enter a valid collaborator email.');
            return;
        }

        if (collaborationMembers.some(member => member.email === email)) {
            setCollaborationModalMessage('This collaborator is already in the list.');
            return;
        }

        setCollaborationMembers(prev => [
            ...prev,
            { id: Date.now(), email, role: collaborationRole }
        ]);
        setCollaborationEmail('');
        setCollaborationRole('viewer');
        setCollaborationModalMessage('');
    };

    const handleCreateCollaboration = async (e) => {
        e.preventDefault();
        const name = collaborationName.trim();
        if (!name) {
            setCollaborationModalMessage('Collaboration name is required.');
            return;
        }

        setIsCreatingCollaboration(true);
        setCollaborationModalMessage('');

        try {
            const res = await fetch('/api/Collaborations/create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({
                    name,
                    members: collaborationMembers.map(({ email, role }) => ({ email, role }))
                })
            });

            if (!res.ok) {
                const errorText = await res.text();
                throw new Error(errorText || 'Could not create collaboration.');
            }

            const collaboration = await res.json();
            setCollaborations(prev => [...prev, collaboration].sort((a, b) => a.name.localeCompare(b.name)));
            closeCollaborationModal();
            navigate(`/collaborations/${collaboration.id}`);
        } catch (err) {
            setCollaborationModalMessage(err.message || 'Could not create collaboration.');
        } finally {
            setIsCreatingCollaboration(false);
        }
    };

    // ---------------------------------FOLDER MANAGEMENT-----------------------------------------------------

    const handleCreateFolder = async (e) => {
        e.preventDefault();
        const name = newFolderName.trim();
        if (!name) return;

        const res = await fetch('/api/NoteFolders', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ name })
        });

        if (res.ok) {
            const folder = await res.json();
            setFolders(prev => [...prev, folder].sort((a, b) => a.name.localeCompare(b.name)));
            setNewFolderName('');
            selectFolder(folder.id);
        } else {
            setMessage('Could not create folder.');
        }
    };

    const handleRenameFolder = async (folderId) => {
        const name = editingFolderName.trim();
        if (!name) return;

        const res = await fetch(`/api/NoteFolders/${folderId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ name })
        });

        if (res.ok) {
            const updatedFolder = await res.json();
            setFolders(prev => prev.map(folder => folder.id === folderId ? updatedFolder : folder));
            setEditingFolderId(null);
            setEditingFolderName('');
        } else {
            setMessage('Could not rename folder.');
        }
    };

    const handleDeleteFolder = async () => {
        if (!folderToDelete) return;

        const res = await fetch(`/api/NoteFolders/${folderToDelete.id}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (res.ok) {
            setFolders(prev => prev.filter(folder => folder.id !== folderToDelete.id));
            setNotes(prev => prev.map(note =>
                note.folderId === folderToDelete.id
                    ? { ...note, folderId: null, folderName: null }
                    : note
            ));
            if (selectedFolderId === folderToDelete.id) selectView(VIEW_RECENT);
            setFolderToDelete(null);
        } else {
            setMessage('Could not delete folder.');
        }
    };

    // ---------------------------------NOTE OPENING / EMPTY STATE TEXT---------------------------------------

    const openNote = (note) => {
        const isCollab = Boolean(note.collaborationId);
        const url = isCollab
            ? `/collaborations/${note.collaborationId}/notes/${note.guid}`
            : `/notes/${note.guid}`;
        navigate(url);
    };

    const emptyMessage = searchTerm
        ? 'No notes match your search.'
        : selectedFolder
            ? 'This folder is empty.'
            : selectedView === VIEW_RECENT
                ? 'No recent notes yet.'
            : selectedView === VIEW_SHARED
                ? 'No notes have been shared with you yet.'
            : selectedView === VIEW_NO_FOLDER
                ? 'No notes without a folder.'
                : 'No notes yet. Create your first note.';

    return (
        <div className="notes-page">
            <Sidebar />
            <div className="notes-main notes-workspace">
                {/* Workspace sidebar: view filters, folders, and collaboration links */}
                <aside className="notes-workspace-sidebar">
                    {/* Notes view filters: recent, no-folder, shared */}
                    <div className="notes-sidebar-title">Notes</div>
                    <button
                        className={`notes-view-btn ${selectedView === VIEW_RECENT ? 'active' : ''}`}
                        onClick={() => selectView(VIEW_RECENT)}
                    >
                        <Clock size={17} />
                        Recent
                    </button>
                    <button
                        className={`notes-view-btn ${selectedView === VIEW_NO_FOLDER ? 'active' : ''}`}
                        onClick={() => selectView(VIEW_NO_FOLDER)}
                    >
                        <Inbox size={17} />
                        No Folder
                    </button>
                    <button
                        className={`notes-view-btn ${selectedView === VIEW_SHARED ? 'active' : ''}`}
                        onClick={() => selectView(VIEW_SHARED)}
                    >
                        <UserRoundCheck size={17} />
                        Shared with me
                    </button>

                    {/* Folder management: create, select, rename, delete */}
                    <div className="notes-folder-heading">Folders</div>
                    <form className="notes-folder-create" onSubmit={handleCreateFolder}>
                        <input
                            value={newFolderName}
                            onChange={(e) => setNewFolderName(e.target.value)}
                            placeholder="New folder"
                        />
                        <button type="submit" title="Create folder">
                            <FolderPlus size={17} />
                        </button>
                    </form>

                    <div className="notes-folder-list">
                        {folders.map(folder => (
                            <div
                                key={folder.id}
                                className={`notes-folder-row ${selectedFolderId === folder.id ? 'active' : ''}`}
                            >
                                {editingFolderId === folder.id ? (
                                    <div className="notes-folder-edit">
                                        <input
                                            value={editingFolderName}
                                            onChange={(e) => setEditingFolderName(e.target.value)}
                                        />
                                        <div className="notes-folder-edit-actions">
                                            <button onClick={() => handleRenameFolder(folder.id)}>Save</button>
                                            <button onClick={() => setEditingFolderId(null)}>
                                                <X size={14} />
                                            </button>
                                        </div>
                                    </div>
                                ) : (
                                    <>
                                        <button className="notes-folder-select" onClick={() => selectFolder(folder.id)}>
                                            <Folder size={17} />
                                            <span>{folder.name}</span>
                                            <small>{folder.noteCount ?? 0}</small>
                                        </button>
                                        <button
                                            className="notes-icon-btn"
                                            title="Rename folder"
                                            onClick={() => {
                                                setEditingFolderId(folder.id);
                                                setEditingFolderName(folder.name);
                                            }}
                                        >
                                            <Pencil size={15} />
                                        </button>
                                        <button
                                            className="notes-icon-btn danger"
                                            title="Delete folder"
                                            onClick={() => setFolderToDelete(folder)}
                                        >
                                            <Trash2 size={15} />
                                        </button>
                                    </>
                                )}
                            </div>
                        ))}
                    </div>

                    {/* Collaboration workspaces: create a workspace or open an existing one */}
                    <div className="notes-folder-heading notes-heading-row">
                        <span>Collaborations</span>
                        <button
                            type="button"
                            className="notes-heading-action"
                            title="Create collaboration"
                            onClick={() => setCollaborationModalOpen(true)}
                        >
                            <Plus size={15} />
                        </button>
                    </div>

                    <div className="notes-folder-list">
                        {collaborations.length === 0 ? (
                            <p className="notes-sidebar-empty">No shared workspaces yet.</p>
                        ) : collaborations.map(collaboration => (
                            <div key={collaboration.id} className="notes-folder-row">
                                <button
                                    className="notes-folder-select"
                                    onClick={() => navigate(`/collaborations/${collaboration.id}`)}
                                >
                                    <Users size={17} />
                                    <span>{collaboration.name}</span>
                                    <small>{collaboration.myRole}</small>
                                </button>
                            </div>
                        ))}
                    </div>
                </aside>

                {/* Main notes area: title, search, status messages, and note cards */}
                <main className="notes-workspace-main">
                    <div className="notes-workspace-header">
                        <div>
                            <h1 className="dashboard-title">Notes</h1>
                            <p className="notes-subtitle">
                                {selectedFolder
                                    ? selectedFolder.name
                                    : selectedView === VIEW_RECENT
                                        ? 'Notes you personally worked on'
                                        : selectedView === VIEW_SHARED
                                            ? 'Notes other users shared with you'
                                            : selectedView === VIEW_NO_FOLDER
                                            ? 'Notes not assigned to a folder'
                                            : 'All personal notes'}
                            </p>
                        </div>
                        <div className="notes-actions">
                            <button className="notes-btn" onClick={handleCreateNote}>
                                <Plus size={16} />
                                Create Note
                            </button>
                        </div>
                    </div>

                    <div className="notes-search-row">
                        <Search size={18} />
                        <input
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            placeholder="Search by title or content"
                        />
                    </div>

                    {/* Workspace messages and recent-notes loading feedback */}
                    {message && <div className="notes-message">{message}</div>}
                    {selectedView === VIEW_RECENT && recentMessage && (
                        <div className="notes-message">{recentMessage}</div>
                    )}

                    {loading ? (
                        <div className="notes-empty-state">Loading your notes...</div>
                    ) : filteredNotes.length === 0 ? (
                        <div className="notes-empty-state">
                            <p>{emptyMessage}</p>
                            {!searchTerm && selectedView !== VIEW_SHARED && (
                                <button className="notes-btn" onClick={handleCreateNote}>
                                    <Plus size={16} />
                                    Create Note
                                </button>
                            )}
                        </div>
                    ) : (
                        /* Note grid: opens either personal notes or collaboration notes */
                        <div className="notes-grid">
                            {filteredNotes.map(note => (
                                <div key={note.id} onClick={() => openNote(note)}>
                                    <NotesNoteCard
                                        note={{
                                            id: note.id,
                                            title: note.title || 'Untitled Note',
                                            tag: selectedView === VIEW_SHARED
                                                ? note.accessRole === 'editor' ? 'Can edit' : 'Shared'
                                                : note.collaborationId ? 'Collaboration' : 'Private',
                                            desc: (note.content || '').substring(0, 140) + ((note.content || '').length > 140 ? '...' : ''),
                                            date: new Date(note.updatedAt || note.createdAt).toLocaleDateString(),
                                            folderName: selectedView === VIEW_SHARED
                                                ? note.ownerEmail ? `Owner: ${note.ownerEmail}` : ''
                                                : note.folderName || folderNameById[note.folderId]
                                        }}
                                    />
                                </div>
                            ))}
                        </div>
                    )}
                </main>
            </div>

            {/* Folder delete confirmation modal */}
            {folderToDelete && (
                <div className="modal-overlay">
                    <div className="modal-content notes-confirm-modal">
                        <div className="notes-modal-header">
                            <h3>Delete folder?</h3>
                            <button onClick={() => setFolderToDelete(null)}>
                                <X size={18} />
                            </button>
                        </div>
                        <p>
                            Delete <strong>{folderToDelete.name}</strong>? Notes inside it will move back to No Folder.
                        </p>
                        <div className="notes-modal-actions">
                            <button className="btn-cancel" onClick={() => setFolderToDelete(null)}>Cancel</button>
                            <button className="btn-danger-confirm" onClick={handleDeleteFolder}>Delete folder</button>
                        </div>
                    </div>
                </div>
            )}

            {/* Collaboration creation modal */}
            {collaborationModalOpen && (
                <div className="modal-overlay">
                    <div className="modal-content notes-confirm-modal notes-collaboration-modal">
                        <div className="notes-modal-header">
                            <h3>Create collaboration</h3>
                            <button onClick={closeCollaborationModal} disabled={isCreatingCollaboration}>
                                <X size={18} />
                            </button>
                        </div>

                        <form onSubmit={handleCreateCollaboration}>
                            <label className="notes-modal-label">
                                Collaboration name
                                <input
                                    value={collaborationName}
                                    onChange={(e) => setCollaborationName(e.target.value)}
                                    placeholder="Workspace name"
                                    autoFocus
                                />
                            </label>

                            <div className="notes-collab-member-form">
                                <input
                                    type="text"
                                    value={collaborationEmail}
                                    onChange={(e) => setCollaborationEmail(e.target.value)}
                                    placeholder="Collaborator email"
                                />
                                <select
                                    value={collaborationRole}
                                    onChange={(e) => setCollaborationRole(e.target.value)}
                                >
                                    <option value="viewer">Viewer</option>
                                    <option value="editor">Editor</option>
                                </select>
                                <button type="button" onClick={handleAddCollaborationMember}>
                                    <UserPlus size={15} />
                                    Add
                                </button>
                            </div>

                            {/* Pending collaborator list before workspace creation */}
                            <div className="notes-collab-member-list">
                                {collaborationMembers.length === 0 ? (
                                    <p>No collaborators yet. You can add members later.</p>
                                ) : collaborationMembers.map(member => (
                                    <div className="notes-collab-member-row" key={member.id}>
                                        <span>{member.email}</span>
                                        <strong>{member.role}</strong>
                                        <button
                                            type="button"
                                            onClick={() => setCollaborationMembers(prev => prev.filter(item => item.id !== member.id))}
                                            title="Remove collaborator"
                                        >
                                            <X size={14} />
                                        </button>
                                    </div>
                                ))}
                            </div>

                            {collaborationModalMessage && (
                                <div className="notes-message notes-modal-message">
                                    {collaborationModalMessage}
                                </div>
                            )}

                            <div className="notes-modal-actions">
                                <button type="button" className="btn-cancel" onClick={closeCollaborationModal} disabled={isCreatingCollaboration}>
                                    Cancel
                                </button>
                                <button type="submit" className="notes-btn" disabled={isCreatingCollaboration}>
                                    {isCreatingCollaboration ? 'Creating...' : 'Create'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
};

export default NotesPage;

import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import NotesNoteCard from '../components/NotesNoteCard';
import {
    Clock,
    FileText,
    Folder,
    FolderPlus,
    Inbox,
    Pencil,
    Plus,
    Search,
    Trash2,
    UserRoundCheck,
    Users,
    X
} from 'lucide-react';
import '../components/NotesNoteCard.css';

/**
 * Purpose: Personal notes workspace with folders, filtering, and search.
 * API touched: GET /api/Notes/my-notes, CRUD /api/NoteFolders.
 * Route contract: collaboration notes still route to /collaborations/{id}/notes/{guid}.
 */

const VIEW_ALL = 'all';
const VIEW_RECENT = 'recent';
const VIEW_NO_FOLDER = 'no-folder';
const VIEW_SHARED = 'shared';

const NotesPage = () => {
    const navigate = useNavigate();
    const [notes, setNotes] = useState([]);
    const [sharedNotes, setSharedNotes] = useState([]);
    const [folders, setFolders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedView, setSelectedView] = useState(VIEW_ALL);
    const [selectedFolderId, setSelectedFolderId] = useState(null);
    const [searchTerm, setSearchTerm] = useState('');
    const [newFolderName, setNewFolderName] = useState('');
    const [editingFolderId, setEditingFolderId] = useState(null);
    const [editingFolderName, setEditingFolderName] = useState('');
    const [folderToDelete, setFolderToDelete] = useState(null);
    const [message, setMessage] = useState('');

    const loadWorkspace = async () => {
        setLoading(true);
        setMessage('');
        try {
            const [notesRes, foldersRes] = await Promise.all([
                fetch('/api/Notes/my-notes', { credentials: 'include' }),
                fetch('/api/NoteFolders/my-folders', { credentials: 'include' })
            ]);
            const sharedRes = await fetch('/api/Notes/shared-with-me', { credentials: 'include' });

            if (!notesRes.ok) throw new Error('Failed to fetch notes');
            if (!foldersRes.ok) throw new Error('Failed to fetch folders');
            if (!sharedRes.ok) throw new Error('Failed to fetch shared notes');

            setNotes(await notesRes.json());
            setFolders(await foldersRes.json());
            setSharedNotes(await sharedRes.json());
        } catch (err) {
            console.error('Error loading notes workspace:', err);
            setMessage('Could not load notes workspace.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadWorkspace();
    }, []);

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
        let nextNotes = selectedView === VIEW_SHARED ? [...sharedNotes] : [...notes];

        if (selectedView === VIEW_RECENT) {
            nextNotes = nextNotes.slice(0, 10);
        } else if (selectedView === VIEW_NO_FOLDER) {
            nextNotes = nextNotes.filter(note => !note.folderId);
        } else if (selectedFolderId) {
            nextNotes = nextNotes.filter(note => note.folderId === selectedFolderId);
        }

        if (normalizedSearch) {
            nextNotes = nextNotes.filter(note => {
                const title = (note.title || '').toLowerCase();
                const content = (note.content || '').toLowerCase();
                return title.includes(normalizedSearch) || content.includes(normalizedSearch);
            });
        }

        return nextNotes;
    }, [notes, sharedNotes, searchTerm, selectedFolderId, selectedView]);

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
            if (selectedFolderId === folderToDelete.id) selectView(VIEW_ALL);
            setFolderToDelete(null);
        } else {
            setMessage('Could not delete folder.');
        }
    };

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
            : selectedView === VIEW_SHARED
                ? 'No notes have been shared with you yet.'
            : selectedView === VIEW_NO_FOLDER
                ? 'No notes without a folder.'
                : 'No notes yet. Create your first note.';

    return (
        <div className="notes-page">
            <Sidebar />
            <div className="notes-main notes-workspace">
                <aside className="notes-workspace-sidebar">
                    <div className="notes-sidebar-title">Notes</div>
                    <button
                        className={`notes-view-btn ${selectedView === VIEW_ALL && !selectedFolderId ? 'active' : ''}`}
                        onClick={() => selectView(VIEW_ALL)}
                    >
                        <FileText size={17} />
                        All Notes
                    </button>
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
                </aside>

                <main className="notes-workspace-main">
                    <div className="notes-workspace-header">
                        <div>
                            <h1 className="dashboard-title">Notes</h1>
                            <p className="notes-subtitle">
                                {selectedFolder
                                    ? selectedFolder.name
                                    : selectedView === VIEW_RECENT
                                        ? 'Recently updated notes'
                                        : selectedView === VIEW_SHARED
                                            ? 'Notes other users shared with you'
                                            : selectedView === VIEW_NO_FOLDER
                                            ? 'Notes not assigned to a folder'
                                            : 'All personal and collaboration notes'}
                            </p>
                        </div>
                        <div className="notes-actions">
                            <button className="notes-btn" onClick={handleCreateNote}>
                                <Plus size={16} />
                                Create Note
                            </button>
                            <button className="notes-btn secondary" onClick={() => navigate('/collaborations/new')}>
                                <Users size={16} />
                                Create Collaboration
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

                    {message && <div className="notes-message">{message}</div>}

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
        </div>
    );
};

export default NotesPage;

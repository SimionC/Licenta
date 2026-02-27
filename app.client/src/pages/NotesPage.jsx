import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Sidebar from '../components/Sidebar';
import FolderCard from '../components/FolderCard';
import NotesNoteCard from '../components/NotesNoteCard';
import { Users, Plus, FolderPlus } from 'lucide-react';

const mockFolders = ['Databases', 'Finance', 'Management', 'Data Structures'];

/**
 * Purpose: Notes index page with create actions and navigation into note editor routes.
 * API touched: GET /api/Notes/my-notes.
 * Route contract: collaboration notes route to /collaborations/{id}/notes/{guid}, personal notes to /notes/{guid}.
 */

const NotesPage = () => {
    const navigate = useNavigate();
    const [notes, setNotes] = useState([]);
    const [loading, setLoading] = useState(true);

    // useEffect: fetches current user's notes and resolves loading state.
    useEffect(() => {
        // Fetch user's notes
        fetch('/api/Notes/my-notes', { credentials: 'include' })
            .then(res => {
                if (!res.ok) throw new Error('Failed to fetch notes');
                return res.json();
            })
            .then(data => {
                setNotes(data);
                setLoading(false);
            })
            .catch(err => {
                console.error('Error fetching notes:', err);
                setLoading(false);
            });
    }, []);

    const handleCreateNote = () => {
        navigate('/notes/new');
    };

    const handleCreateFolder = () => {
        // TODO: Implement folder creation
        alert('Folder creation coming soon!');
    };

    const handleCreateCollaboration = () => {
        // Navigate to collaboration creation page
        navigate('/collaborations/new');
    };

    if (loading) {
        return (
            <div className="notes-page">
                <Sidebar />
                <div className="notes-main">
                    <div className="dashboard-main">
                        <div className="dashboard-left">
                            <div className="courses-content-wrapper">
                                <div className="text-center mt-5">
                                    <p>Loading your notes...</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="notes-page">
            <Sidebar />
            <div className="notes-main">
                <div className="dashboard-main">
                    <div className="dashboard-left">
                        <div className="courses-content-wrapper">
                            <div className="d-flex justify-content-between align-items-center mb-4">
                                <h1 className="dashboard-title">Notes</h1>
                                <button
                                    className="notes-btn"
                                    onClick={handleCreateFolder}
                                >
                                    <FolderPlus size={16} style={{ marginRight: '6px' }} />
                                    Create Folder
                                </button>
                            </div>

                            {/* Folders Section */}
                            <div className="notes-folders">
                                {mockFolders.map(name => (
                                    <FolderCard key={name} name={name} />
                                ))}
                            </div>

                            {/* Action Buttons */}
                            <div className="notes-actions">
                                <button
                                    className="notes-btn"
                                    onClick={handleCreateNote}
                                >
                                    <Plus size={16} style={{ marginRight: '6px' }} />
                                    Create Note
                                </button>
                                <button
                                    className="notes-btn"
                                    onClick={handleCreateCollaboration}
                                >
                                    <Users size={16} style={{ marginRight: '6px' }} />
                                    Create Collaboration
                                </button>
                            </div>

                            {/* Notes Grid */}
                            <div className="notes-grid">
                                {notes.length === 0 ? (
                                    <div className="text-center py-5">
                                        <p className="text-muted">No notes yet. Create your first note!</p>
                                        <button
                                            className="notes-btn mt-3"
                                            onClick={handleCreateNote}
                                        >
                                            <Plus size={16} style={{ marginRight: '6px' }} />
                                            Create Your First Note
                                        </button>
                                    </div>
                                ) : (
                                    notes.map(note => (
                                        <div key={note.id} onClick={() => {
                                            const isCollab = Boolean(note.collaborationId);
                                            const url = isCollab
                                                ? `/collaborations/${note.collaborationId}/notes/${note.guid}`
                                                : `/notes/${note.guid}`;
                                            navigate(url);
                                        }} >
                                            <NotesNoteCard
                                                note={{
                                                    id: note.id,
                                                    title: note.title,
                                                    tag: note.collaborationId ? 'Collaboration' : note.isPublic ? 'Public' : 'Private',
                                                    desc: note.content.substring(0, 100) + (note.content.length > 100 ? '...' : ''),
                                                    date: new Date(note.updatedAt || note.createdAt).toLocaleDateString()
                                                }}
                                            />
                                        </div>
                                    ))
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default NotesPage;
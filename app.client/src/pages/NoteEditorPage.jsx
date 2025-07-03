import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Save, Eye, X } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import './NoteEditorPage.css';

const NoteEditorPage = () => {
    const { noteGuid } = useParams();
    const navigate = useNavigate();
    const [note, setNote] = useState(null);
    const [title, setTitle] = useState('Untitled Note');
    const [content, setContent] = useState('# Welcome to your new note\n\nStart writing here...');
    const [isEditing, setIsEditing] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [isNewNote, setIsNewNote] = useState(false);

    useEffect(() => {
        if (noteGuid && noteGuid !== 'new') {
            // Load existing note
            fetch(`/api/Notes/${noteGuid}`, { credentials: 'include' })
                .then(res => {
                    if (!res.ok) throw new Error('Failed to fetch note');
                    return res.json();
                })
                .then(data => {
                    setNote(data);
                    setTitle(data.title);
                    setContent(data.content);
                })
                .catch(err => {
                    console.error('Error loading note:', err);
                    navigate('/notes');
                });
        } else {
            // New note
            setIsNewNote(true);
            setIsEditing(true);
        }
    }, [noteGuid, navigate]);

    const handleSave = async () => {
        setIsSaving(true);

        try {
            const payload = {
                title: title,
                content,
                isPublic: false
            };

            console.log('Sending payload:', payload); // Debug log

            let response;
            if (isNewNote) {
                // Create new note
                response = await fetch('/api/Notes/create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify(payload)
                });
            } else {
                // Update existing note
                response = await fetch(`/api/Notes/${noteGuid}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify(payload)
                });
            }

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Server error:', errorText);
                throw new Error(`Failed to save note: ${response.status} ${response.statusText}`);
            }

            const savedNote = await response.json();
            setNote(savedNote);
            setTitle(savedNote.title);
            setContent(savedNote.content);
            setIsEditing(false);
            setIsNewNote(false);

            // If it was a new note, navigate to the saved note's URL
            if (isNewNote) {
                navigate(`/notes/${savedNote.guid}`, { replace: true });
            }

        } catch (error) {
            console.error('Error saving note:', error);
            alert(`Failed to save note: ${error.message}`);
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async () => {
        if (!note || isNewNote) return;

        if (!window.confirm('Are you sure you want to delete this note?')) return;

        try {
            const response = await fetch(`/api/Notes/${noteGuid}`, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (!response.ok) throw new Error('Failed to delete note');

            navigate('/notes');
        } catch (error) {
            console.error('Error deleting note:', error);
            alert('Failed to delete note. Please try again.');
        }
    };

    const handleCancel = () => {
        if (isNewNote) {
            navigate('/notes');
        } else {
            setTitle(note.title);
            setContent(note.content);
            setIsEditing(false);
        }
    };

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
                                <button
                                    className="note-action-btn edit-btn"
                                    onClick={() => setIsEditing(true)}
                                >
                                    <Eye size={16} />
                                    Edit Note
                                </button>
                                {note?.isOwner && (
                                    <button
                                        className="note-action-btn delete-btn"
                                        onClick={handleDelete}
                                    >
                                        Delete
                                    </button>
                                )}
                            </>
                        )}
                    </div>
                </div>

                {/* Content */}
                <div className="note-content-section">
                    <div className="note-content-header">
                        <h3>Content</h3>
                        {!isEditing && (
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
                                    <pre className="note-content-text">{content}</pre>
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
    );
};

export default NoteEditorPage;
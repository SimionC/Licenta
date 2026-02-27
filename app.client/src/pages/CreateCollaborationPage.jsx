import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Save, X, Users, Share2, Eye } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import CollaboratorsSection from '../components/CollaboratorsSection';

//for markdown editor
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import remarkMath from 'remark-math';
import rehypeKatex from 'rehype-katex';
import rehypeHighlight from 'rehype-highlight'
import 'katex/dist/katex.min.css'
import 'highlight.js/styles/github.css'

/**
 * Purpose: Collaboration creation wizard for collaborative note bootstrap.
 * API touched: POST /api/Collaborations/create, then POST /api/Notes/create.
 * Flow contract: two-step create is required to obtain collaborationId before note creation.
 */

const CreateCollaborationPage = () => {
    const navigate = useNavigate();
    const [noteTitle, setNoteTitle] = useState('');
    const [noteContent, setNoteContent] = useState('# Welcome to your collaborative note\n\nStart writing here...');
    const [collaborators, setCollaborators] = useState([]);
    const [isSaving, setIsSaving] = useState(false);
    const [isPreview, setIsPreview] = useState(false);

    // handleSave: validates title+members, creates collaboration first, then linked note, then redirects.
    const handleSave = async () => {

        if (!noteTitle.trim()) {
            alert('Please enter a note title');
            return;
        }

        if (collaborators.length === 0) {
            alert('Please add at least one collaborator to create a collaborative note.');
            return;
        }

        setIsSaving(true);

        try {
            // First create the collaboration
            const collaborationPayload = {
                name: noteTitle,
                memberEmails: collaborators.map(c => c.email)
            };

            const collaborationResponse = await fetch('/api/Collaborations/create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(collaborationPayload)
            });

            if (!collaborationResponse.ok) {
                const errorText = await collaborationResponse.text();
                throw new Error(`Failed to create collaboration: ${collaborationResponse.status} ${collaborationResponse.statusText} - ${errorText}`);
            }

            const collaboration = await collaborationResponse.json();

            // Then create the note, linking it to the new collaboration
            const notePayload = {
                title: noteTitle,
                content: noteContent,
                isPublic: false, // Collaborative notes are not 'public' in the same way; access is via collaboration
                collaborationId: collaboration.id
            };

            const noteResponse = await fetch('/api/Notes/create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(notePayload)
            });

            if (!noteResponse.ok) {
                const errorText = await noteResponse.text();
                throw new Error(`Failed to create note: ${noteResponse.status} ${noteResponse.statusText} - ${errorText}`);
            }

            const note = await noteResponse.json();
 
            // Navigate into the collaboration’s note view
            navigate(
                `/collaborations/${collaboration.id}/notes/${note.guid}`,
                { replace: true }
            );
        } catch (error) {
            console.error('Error creating collaboration:', error);
            alert(`Failed to create collaboration: ${error.message}`);
        } finally {
            setIsSaving(false);
        }
    };

    //create coll page

    const handleCancel = () => {
        navigate('/notes');
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
                            <h1 className="note-title">
                                <Users size={24} style={{ marginRight: '0.5rem' }} />
                                Create Collaboration
                            </h1>
                        </div>
                    </div>

                    <div className="note-editor-actions">
                        <button
                            className="note-action-btn save-btn"
                            onClick={handleSave}
                            disabled={isSaving}
                        >
                            <Save size={16} />
                            {isSaving ? 'Creating...' : 'Create Collaboration'}
                        </button>
                        <button
                            className="note-action-btn cancel-btn"
                            onClick={handleCancel}
                        >
                            <X size={16} />
                            Cancel
                        </button>
                    </div>
                </div>

                {/* Main Container */}
                <div className="note-main-flex">
                    {/* Note Content Section */}
                    <div className="note-content-section">
                        <div className="note-content-header">
                            <h3>Content</h3>
                            <button
                                className="note-edit-content-btn"
                                onClick={() => setIsPreview(!isPreview)}
                            >
                                <Eye size={16} />
                                {isPreview ? 'Edit' : 'Preview'}
                            </button>
                        </div>

                        <div className="note-content-box">

                            {/* Note Title */}
                            <div className="form-group" style={{ marginBottom: '1rem' }}>
                                <label htmlFor="noteTitle">Note Title</label>
                                <input
                                    type="text"
                                    id="noteTitle"
                                    className="note-title-input"
                                    value={noteTitle}
                                    onChange={(e) => setNoteTitle(e.target.value)}
                                    placeholder="Enter note title"
                                />
                            </div>

                            {/* Note Content */}
                            <div className="form-group">
                                <label>Note Content</label>
                                {isPreview ? (
                                    <div className="note-content-display">
                                        <ReactMarkdown
                                            children={noteContent}
                                            remarkPlugins={[remarkGfm, remarkMath]}
                                            rehypePlugins={[
                                                rehypeKatex,
                                                rehypeHighlight,
                                            ]}
                                        />
                                    </div>
                                ) : (
                                    <textarea
                                        className="note-content-textarea"
                                        value={noteContent}
                                        onChange={(e) => setNoteContent(e.target.value)}
                                        placeholder="Start writing your collaborative note here..."
                                        style={{ minHeight: '300px' }}
                                    />
                                )}
                            </div>
                        </div>
                    </div>

                    {/* Collaboration Section */}
                    <div className="note-sharing-section">
                        <h3 className="sharing-header">
                            <Share2 size={20} />
                            Collaboration Settings
                        </h3>

                        <div className="sharing-properties-box">
                            <div className="collaboration-info">
                                <p className="collaboration-description">
                                    Create a collaborative note where you and your team can work together.
                                    All members will be able to view and edit this note based on their permissions.
                                </p>
                            </div>

                            <CollaboratorsSection
                                collaborators={collaborators}
                                setCollaborators={setCollaborators}
                                // For CreateCollaborationPage, the section is always editable
                                parentIsEditing={true}
                            />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default CreateCollaborationPage;
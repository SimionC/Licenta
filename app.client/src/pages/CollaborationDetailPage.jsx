import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, FileText, LogOut, Plus, Settings, Shield, Trash2, Users, X } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import NotesNoteCard from '../components/NotesNoteCard';
import CollaboratorsSection from '../components/CollaboratorsSection';
import '../components/NotesNoteCard.css';
import './CollaborationStyles.css';


 //Purpose: Collaboration workspace page with member context and contained notes
 
export default function CollaborationDetailPage() {
    // -------------------------
    // ROUTE AND PAGE STATE
    // -------------------------
    const { collabId } = useParams();
    const navigate = useNavigate();

    // Loaded workspace data.
    const [collab, setCollab] = useState(null);
    const [notes, setNotes] = useState([]);

    // Page status and modal state.
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [settingsOpen, setSettingsOpen] = useState(false);
    const [confirmAction, setConfirmAction] = useState(null);
    const [actionBusy, setActionBusy] = useState(false);

    // -------------------------
    // DATA LOADING
    // -------------------------
    const loadWorkspace = async () => {
        setLoading(true);
        setError('');

        try {
            const [collabRes, notesRes] = await Promise.all([
                fetch(`/api/Collaborations/${collabId}`, { credentials: 'include' }),
                fetch(`/api/Collaborations/${collabId}/notes`, { credentials: 'include' })
            ]);

            if (collabRes.status === 403) throw new Error('You no longer have access to this collaboration.');
            if (collabRes.status === 404) throw new Error('This collaboration no longer exists.');
            if (!collabRes.ok) throw new Error('Failed to load collaboration.');
            if (!notesRes.ok) throw new Error('Failed to load collaboration notes.');

            setCollab(await collabRes.json());
            const notesData = await notesRes.json();
            setNotes(Array.isArray(notesData) ? notesData : []);
        } catch (err) {
            setError(err.message || 'Could not load collaboration workspace.');
            setCollab(null);
            setNotes([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadWorkspace();
    }, [collabId]);

    // -------------------------
    // ROLE FLAGS
    // -------------------------
    const canCreateNotes = useMemo(() => {
        return collab?.myRole === 'owner' || collab?.myRole === 'editor';
    }, [collab?.myRole]);

    const canManageMembers = collab?.myRole === 'owner';
    const roleLabel = collab?.myRole || 'viewer';

    // -------------------------
    // WORKSPACE DELETE / LEAVE ACTION
    // -------------------------
    const handleWorkspaceAction = async () => {
        if (!confirmAction || actionBusy) return;

        setActionBusy(true);
        try {
            const endpoint = confirmAction === 'delete'
                ? `/api/Collaborations/${collabId}`
                : `/api/Collaborations/${collabId}/leave`;

            const res = await fetch(endpoint, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (!res.ok) {
                const message = await res.text();
                throw new Error(message || 'Workspace action failed.');
            }

            navigate('/notes', {
                replace: true,
                state: {
                    removedCollaborationId: Number(collabId),
                    workspaceAction: confirmAction
                }
            });
        } catch (err) {
            setError(err.message || 'Could not update workspace.');
            setConfirmAction(null);
            setSettingsOpen(false);
        } finally {
            setActionBusy(false);
        }
    };

    return (
        <div className="notes-page">
            {/* Page shell: global sidebar plus the selected collaboration workspace. */}
            <Sidebar />
            <main className="collaboration-workspace">
                {/* Back navigation to the general notes area. */}
                <button className="collaboration-back-btn" onClick={() => navigate('/notes')}>
                    <ArrowLeft size={18} />
                    Back to notes
                </button>

                {loading ? (
                    <div className="notes-empty-state">Loading collaboration...</div>
                ) : error ? (
                    <div className="collaboration-error-card">
                        <h2>Workspace unavailable</h2>
                        <p>{error}</p>
                        <button className="notes-btn" onClick={() => navigate('/notes')}>
                            Back to notes
                        </button>
                    </div>
                ) : (
                    <>
                        {/* Workspace header: title, member/note count, role badge, and create-note action. */}
                        <section className="collaboration-hero collaboration-folder-hero">
                            <div>
                                <div className="collaboration-eyebrow">
                                    <Users size={17} />
                                    Shared workspace
                                </div>
                                <h1>{collab.name}</h1>
                                <p>
                                    {notes.length} note{notes.length === 1 ? '' : 's'} · {collab.members?.length || 0} member{collab.members?.length === 1 ? '' : 's'}
                                </p>
                            </div>
                            <div className="collaboration-hero-actions">
                                <span className="collaboration-role-badge">
                                    <Shield size={15} />
                                    {roleLabel}
                                </span>
                                {canCreateNotes && (
                                    <button
                                        className="notes-btn"
                                        onClick={() => navigate(`/collaborations/${collabId}/notes/new`)}
                                    >
                                        <Plus size={16} />
                                        New note
                                    </button>
                                )}
                            </div>
                        </section>

                        {/* Main workspace layout: member list on the side, notes grid on the right. */}
                        <div className="collaboration-layout">
                            <aside className="collaboration-members-panel">
                                <div className="collaboration-panel-title">
                                    <h2>Members</h2>
                                    <button onClick={() => setSettingsOpen(true)} title="Workspace settings">
                                        <Settings size={15} />
                                    </button>
                                </div>
                                <div className="collaboration-member-list">
                                    {collab.members?.map(member => (
                                        <div className="collaboration-member-row" key={member.id}>
                                            <div>
                                                <strong>{member.email}</strong>
                                                {member.isOwner && <span>Workspace owner</span>}
                                            </div>
                                            <small>{member.role}</small>
                                        </div>
                                    ))}
                                </div>
                            </aside>

                            <section className="collaboration-notes-panel">
                                {notes.length === 0 ? (
                                    <div className="notes-empty-state collaboration-empty-state">
                                        <FileText size={24} />
                                        <p>No notes inside this collaboration yet.</p>
                                        {canCreateNotes && (
                                            <button
                                                className="notes-btn"
                                                onClick={() => navigate(`/collaborations/${collabId}/notes/new`)}
                                            >
                                                <Plus size={16} />
                                                Create first note
                                            </button>
                                        )}
                                    </div>
                                ) : (
                                    <div className="notes-grid">
                                        {notes.map(note => (
                                            <div
                                                key={note.guid}
                                                onClick={() => navigate(`/collaborations/${collabId}/notes/${note.guid}`)}
                                            >
                                                <NotesNoteCard
                                                    note={{
                                                        id: note.id,
                                                        title: note.title || 'Untitled Note',
                                                        tag: note.canEdit ? 'Can edit' : 'View only',
                                                        desc: (note.content || '').substring(0, 140) + ((note.content || '').length > 140 ? '...' : ''),
                                                        date: new Date(note.updatedAt || note.createdAt).toLocaleDateString(),
                                                        folderName: collab.name
                                                    }}
                                                />
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </section>
                        </div>

                        {/* Settings modal: manages members and exposes delete/leave workspace action. */}
                        {settingsOpen && (
                            <div className="note-modal-overlay">
                                <div className="note-share-modal collaboration-settings-modal">
                                    <div className="note-confirm-header">
                                        <div>
                                            <h3>Workspace settings</h3>
                                            <p className="note-modal-subtitle">
                                                {canManageMembers
                                                    ? 'Manage member access for this collaboration.'
                                                    : 'View the members who can access this collaboration.'}
                                            </p>
                                        </div>
                                        <button onClick={() => setSettingsOpen(false)}>
                                            <X size={18} />
                                        </button>
                                    </div>
                                    <CollaboratorsSection
                                        collaborationId={Number(collabId)}
                                        collaborators={collab.members || []}
                                        setCollaborators={(members) => setCollab(prev => ({ ...prev, members }))}
                                        parentIsEditing={canManageMembers}
                                    />
                                    <div className="collaboration-danger-zone">
                                        <div>
                                            <h4>{canManageMembers ? 'Delete workspace' : 'Leave workspace'}</h4>
                                            <p>
                                                {canManageMembers
                                                    ? 'Deleting this collaboration removes the workspace and all notes inside it for everyone.'
                                                    : 'Leaving removes your access to this workspace. The collaboration and its notes stay available to the remaining members.'}
                                            </p>
                                        </div>
                                        <button
                                            type="button"
                                            className={canManageMembers ? 'collaboration-danger-btn' : 'collaboration-leave-btn'}
                                            onClick={() => setConfirmAction(canManageMembers ? 'delete' : 'leave')}
                                        >
                                            {canManageMembers ? <Trash2 size={16} /> : <LogOut size={16} />}
                                            {canManageMembers ? 'Delete workspace' : 'Leave workspace'}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Final confirmation modal for deleting or leaving the workspace. */}
                        {confirmAction && (
                            <div className="note-modal-overlay">
                                <div className="note-share-modal collaboration-confirm-modal">
                                    <div className="note-confirm-header">
                                        <div>
                                            <h3>{confirmAction === 'delete' ? 'Delete workspace?' : 'Leave workspace?'}</h3>
                                            <p className="note-modal-subtitle">
                                                {confirmAction === 'delete'
                                                    ? `This will permanently delete "${collab.name}" and all notes inside it.`
                                                    : `You will no longer be able to open "${collab.name}" from your notes sidebar.`}
                                            </p>
                                        </div>
                                        <button onClick={() => setConfirmAction(null)} disabled={actionBusy}>
                                            <X size={18} />
                                        </button>
                                    </div>
                                    <div className="collaboration-confirm-actions">
                                        <button
                                            type="button"
                                            className="note-confirm-cancel"
                                            onClick={() => setConfirmAction(null)}
                                            disabled={actionBusy}
                                        >
                                            Cancel
                                        </button>
                                        <button
                                            type="button"
                                            className="note-confirm-danger"
                                            onClick={handleWorkspaceAction}
                                            disabled={actionBusy}
                                        >
                                            {actionBusy
                                                ? 'Working...'
                                                : confirmAction === 'delete'
                                                    ? 'Delete workspace'
                                                    : 'Leave workspace'}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        )}
                    </>
                )}
            </main>
        </div>
    );
}

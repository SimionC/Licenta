import React, { useState, useEffect } from 'react';
import { Plus, X, UserPlus } from 'lucide-react';

/**
 * Purpose: Reusable collaborator management UI for workspace member viewing/management.
 */

export default function CollaboratorsSection({
    collaborators = [],
    setCollaborators,
    collaborationId,
    parentIsEditing
}) {
    // -------------------------
    // LOCAL ADD-MEMBER STATE
    // -------------------------
    const [newEmail, setNewEmail] = useState('');
    const [newRole, setNewRole] = useState('viewer');
    const [showAddForm, setShowAddForm] = useState(false);

    // This useEffect is now solely for hiding the add form if parent editing is turned off.
    useEffect(() => {
        if (!parentIsEditing) {
            setShowAddForm(false);
        }
    }, [parentIsEditing]);

    // -------------------------
    // SERVER SYNC AND MEMBER ACTIONS
    // -------------------------
    // helper to re-load from server when in edit mode
    // refreshMembers: canonical re-sync from server after role/member mutations.
    const refreshMembers = async () => {
        if (!collaborationId) return;
        try {
            const res = await fetch(`/api/Collaborations/${collaborationId}`, {
                credentials: 'include'
            });
            if (res.ok) {
                const data = await res.json();
                setCollaborators(data.members || []);
            }
        } catch (err) {
            console.error(err);
        }
    };

    // addCollaborator: invite a registered user into an existing workspace.
    const addCollaborator = async () => {
        // Only allow adding if parent allows editing
        if (!newEmail.trim() || !parentIsEditing) return; // Ensure parentIsEditing is true

        const email = newEmail.trim();
        if (!email.includes('@')) {
            alert('Please enter a valid email address');
            return;
        }

        // prevent duplicates
        if (collaborators.some(c => c.email.toLowerCase() === email.toLowerCase())) {
            alert('This email is already added');
            return;
        }

        if (!collaborationId) {
            setCollaborators([
                ...collaborators,
                { id: Date.now(), email, role: newRole }
            ]);
        } else {
            // edit-flow: call backend invite endpoint
            try {
                const res = await fetch(
                    `/api/Collaborations/${collaborationId}/invite`,
                    {
                        method: 'POST',
                        credentials: 'include',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ email, role: newRole })
                    }
                );
                if (!res.ok) {
                    const err = await res.text();
                    return alert(err);
                }
                await refreshMembers();
            } catch (err) {
                console.error(err);
            }
        }

        setNewEmail('');
        setNewRole('viewer');
        setShowAddForm(false);
    };

    // removeCollaborator: remove a member from an existing workspace.
    const removeCollaborator = async (memberId) => {
        // Only allow removing if parent allows editing
        if (!parentIsEditing) return; // Ensure parentIsEditing is true

        if (!collaborationId) {
            setCollaborators(collaborators.filter(c => c.id !== memberId));
        } else {
            // edit-flow: call backend delete member
            try {
                const res = await fetch(
                    `/api/Collaborations/${collaborationId}/members/${memberId}`,
                    { method: 'DELETE', credentials: 'include' }
                );
                if (!res.ok) {
                    const err = await res.text();
                    return alert(err);
                }
                await refreshMembers();
            } catch (err) {
                console.error(err);
            }
        }
    };

    // updateCollaboratorRole: change a workspace member role.
    const updateCollaboratorRole = async (memberId, role) => {
        // Only allow updating role if parent allows editing
        if (!parentIsEditing) return; // Ensure parentIsEditing is true

        if (!collaborationId) {
            setCollaborators(collaborators.map(c =>
                c.id === memberId ? { ...c, role } : c
            ));
        } else {
            // edit-flow: call backend update role
            try {
                const res = await fetch(
                    `/api/Collaborations/${collaborationId}/members/${memberId}`,
                    {
                        method: 'PUT',
                        credentials: 'include',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ role })
                    }
                );
                if (!res.ok) {
                    const err = await res.text();
                    return alert(err);
                }
                await refreshMembers();
            } catch (err) {
                console.error(err);
            }
        }
    };

    // The 'disabled' state for inputs/buttons within this component
    // is now solely determined by the parentIsEditing prop.
    const disabled = !parentIsEditing;

    return (
        <div className="collaborators-section">
            {/* Header with optional Add Member action. */}
            <div className="collaborators-header">
                <h4>Collaborators</h4>

                {/* Only show "Add Member" button if parent allows editing */}
                {parentIsEditing && (
                    <button
                        className="add-collaborator-btn"
                        onClick={() => setShowAddForm(!showAddForm)}
                        // The button itself should only be clickable if parentIsEditing is true,
                        // but `disabled={disabled}` also achieves this.
                        disabled={disabled}
                    >
                        <UserPlus size={16} /> Add Member
                    </button>
                )}
            </div>

            {/* Add-member form shown only while the parent allows editing. */}
            {showAddForm && parentIsEditing && ( // Only show form if parent allows editing AND internal flag is true
                <div className="add-collaborator-form">
                    <div className="form-group">
                        <div className="email-input-group">
                            <input
                                type="text"
                                placeholder="Enter email address"
                                value={newEmail}
                                onChange={e => setNewEmail(e.target.value)}
                                onKeyPress={e => e.key === 'Enter' && addCollaborator()}
                                className="collaborator-email-input"
                                disabled={disabled}
                            />
                        </div>

                        <div className="form-actions">
                            <select
                                value={newRole}
                                onChange={e => setNewRole(e.target.value)}
                                className="role-select"
                                disabled={disabled}
                            >
                                <option value="viewer">Viewer</option>
                                <option value="editor">Editor</option>
                            </select>

                            <button
                                className="btn btn-primary btn-sm"
                                onClick={addCollaborator}
                                disabled={disabled}
                            >
                                <Plus size={14} /> Add
                            </button>

                            <button
                                className="btn btn-secondary btn-sm"
                                onClick={() => {
                                    setShowAddForm(false);
                                    setNewEmail('');
                                    setNewRole('viewer');
                                }}
                                disabled={disabled}
                            >
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Current collaborator list with role controls. */}
            <div className="collaborators-list">
                {collaborators.length === 0 ? (
                    <p className="no-collaborators">
                        No collaborators added yet. {parentIsEditing && 'Click "Add Member" to invite someone.'}
                        {!parentIsEditing && 'This note has no collaborators.'}
                    </p>
                ) : (
                    collaborators.map(collab => (
                        <div key={collab.id} className="collaborator-item">
                            <div className="collaborator-info">
                                {/* Display name if available, otherwise email */}
                                <span className="collaborator-email">{collab.name || collab.email}</span>
                            </div>

                            <div className="collaborator-actions">
                                {collab.role === 'owner' ? (
                                    <span className="role-select collaboration-owner-role">Owner</span>
                                ) : (
                                    <>
                                        <select
                                            className="role-select"
                                            value={collab.role}
                                            onChange={e => updateCollaboratorRole(collab.id, e.target.value)}
                                            disabled={disabled}
                                        >
                                            <option value="viewer">Viewer</option>
                                            <option value="editor">Editor</option>
                                        </select>

                                        <button
                                            className="remove-collaborator-btn"
                                            onClick={() => removeCollaborator(collab.id)}
                                            disabled={disabled}
                                        >
                                            <X size={14} />
                                        </button>
                                    </>
                                )}
                            </div>
                        </div>
                    ))
                )}
            </div>

            {/* Static explanation of the available workspace roles. */}
            <div className="permissions-info">
                <h5>Permission Levels:</h5>
                <ul>
                    <li><strong>Viewer:</strong> Can only view workspace notes</li>
                    <li><strong>Editor:</strong> Can create and edit workspace notes</li>
                    <li><strong>Owner:</strong> Can manage the collaboration</li>
                </ul>
            </div>
        </div>
    );
}

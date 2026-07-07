import React from 'react';
import './NotesNoteCard.css';
import { Folder } from 'lucide-react';

/**
 * Purpose: small folder tile used by the notes UI.
 * Contract: display-only; parent handles navigation/clicks if needed.
 */
const FolderCard = ({ name }) => (
    <div className="notes-folder-card">
        {/* Folder icon and folder name. */}
        <div className="notes-folder-icon-wrapper">
            <Folder size={28} strokeWidth={2} />
        </div>
        <div className="notes-folder-name">{name}</div>
    </div>
);

export default FolderCard;

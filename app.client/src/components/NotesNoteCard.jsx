import React from 'react';
import './NotesNoteCard.css';
import { FileText } from 'lucide-react';

/**
 * Purpose: note card used in the notes grid and collaboration workspace.
 * Contract: receives prepared display fields from the parent page.
 */
const NotesNoteCard = ({ note }) => (
    <div className="notes-card">
        {/* Top row: file icon and note tag/access label. */}
        <div className="notes-card-top">
            <FileText size={18} className="notes-icon" />
            <span className="notes-tag">{note.tag}</span>
        </div>
        {/* Main note text. */}
        <h4 className="notes-title">{note.title}</h4>
        <p className="notes-desc">{note.desc}</p>
        {/* Footer metadata: date and optional folder/workspace name. */}
        <div className="notes-footer">
            <span>{note.date}</span>
            {note.folderName && <span>{note.folderName}</span>}
        </div>
    </div>
);

export default NotesNoteCard;

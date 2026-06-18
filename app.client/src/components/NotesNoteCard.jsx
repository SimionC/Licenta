import React from 'react';
import './NotesNoteCard.css';
import { FileText } from 'lucide-react';

const NotesNoteCard = ({ note }) => (
    <div className="notes-card">
        <div className="notes-card-top">
            <FileText size={18} className="notes-icon" />
            <span className="notes-tag">{note.tag}</span>
        </div>
        <h4 className="notes-title">{note.title}</h4>
        <p className="notes-desc">{note.desc}</p>
        <div className="notes-footer">
            <span>{note.date}</span>
            {note.folderName && <span>{note.folderName}</span>}
        </div>
    </div>
);

export default NotesNoteCard;

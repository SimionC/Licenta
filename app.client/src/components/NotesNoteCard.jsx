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
        <div className="notes-footer">{note.date}</div>
    </div>
);

export default NotesNoteCard;

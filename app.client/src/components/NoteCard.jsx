// src/components/NoteCard.jsx
import React from 'react';
import './NoteCard.css';

const NoteCard = ({ note }) => {
    return (
        <div className="note-card">
            <div className="note-icon">📝</div>
            <h5 className="note-title">{note.title}</h5>
        </div>
    );
};

export default NoteCard;

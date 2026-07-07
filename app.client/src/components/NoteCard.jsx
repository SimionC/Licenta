// src/components/NoteCard.jsx
import React from 'react';
import './NoteCard.css';

/**
 * Purpose: minimal note preview card.
 * Contract: display-only; parent owns any click/navigation behavior.
 */
const NoteCard = ({ note }) => {
    return (
        <div className="note-card">
            {/* Simple icon/title display for a note preview. */}
            <div className="note-icon">📝</div>
            <h5 className="note-title">{note.title}</h5>
        </div>
    );
};

export default NoteCard;

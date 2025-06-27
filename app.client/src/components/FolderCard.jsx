import React from 'react';
import './NotesNoteCard.css';
import { Folder } from 'lucide-react';

const FolderCard = ({ name }) => (
    <div className="notes-folder-card">
        <div className="notes-folder-icon-wrapper">
            <Folder size={28} strokeWidth={2} />
        </div>
        <div className="notes-folder-name">{name}</div>
    </div>
);

export default FolderCard;

import React from 'react';
import Sidebar from '../components/Sidebar';
import FolderCard from '../components/FolderCard';
import NotesNoteCard from '../components/NotesNoteCard';
import { Users } from 'lucide-react';


const mockFolders = ['Databases', 'Finance', 'Management', 'Data Structures'];

const mockNotes = [
    { id: 1, title: 'Week 1 Summary', tag: 'Databases', desc: 'Introduction to database concepts...', date: 'Jan 15' },
    { id: 2, title: 'Financial Ratios', tag: 'Finance', desc: 'Key ratios for analyzing performance...', date: 'Jan 14' },
    { id: 3, title: 'Leadership Styles', tag: 'Management', desc: 'Different approaches to leadership...', date: 'Jan 13' },
    { id: 4, title: 'Binary Trees', tag: 'Data Structures', desc: 'Understanding tree structures...', date: 'Jan 12' },
    { id: 5, title: 'SQL Queries', tag: 'Databases', desc: 'Advanced SQL techniques...', date: 'Jan 10' },
];

const NotesPage = () => {



    return (
        <div className="notes-page">
            <Sidebar />
            <div className="notes-main">
                <div className="dashboard-main">
                    <div className="dashboard-left">
                        <div className="courses-content-wrapper">
                            <div className="d-flex justify-content-between align-items-center mb-4">
                                <h1 className="dashboard-title">Notes</h1>
                                <button className="notes-btn">+ Create Folder</button>
                            </div>

                            <div className="notes-folders">
                                {mockFolders.map(name => (
                                    <FolderCard key={name} name={name} />
                                ))}
                            </div>

                            <div className="notes-actions">
                                <button className="notes-btn">+ Create Note</button>
                                <button className="notes-btn">
                                    <Users size={16} style={{ marginRight: '6px' }} />
                                    Create Collaboration
                                </button>
                            </div>

                            <div className="notes-grid">
                                {mockNotes.map(note => (
                                    <NotesNoteCard key={note.id} note={note} />
                                ))}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );


};

export default NotesPage;

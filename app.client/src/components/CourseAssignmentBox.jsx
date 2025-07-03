// src/components/CourseAssignmentBox.jsx
import React from 'react';
import './CourseAssignmentBox.css';
import { Calendar, Clock } from 'lucide-react';

const CourseAssignmentBox = ({ title, description, deadline, status }) => {
    return (
        <div className="course-assignment-box">
            <div className="cab-header">
                <h3 className="cab-title">{title}</h3>
                <span className={`cab-status cab-status--${status}`}>{status}</span>
            </div>
            <p className="cab-desc">{description}</p>
            <div className="cab-meta">
                <div className="cab-meta-item">
                    <Calendar size={16} className="cab-meta-icon" />
                    Due: {deadline}
                </div>
              
            </div>
        </div>
    );
};

export default CourseAssignmentBox;

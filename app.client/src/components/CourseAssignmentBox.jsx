// src/components/CourseAssignmentBox.jsx
import React from 'react';
import './CourseAssignmentBox.css';
import { Calendar } from 'lucide-react';

/**
 * Purpose: presentational assignment summary card.
 * Contract: status controls visual badge class; no API side effects.
 */

const CourseAssignmentBox = ({
    title,
    description,
    deadline,
    weightPercent,
    status = 'pending',
    actionLabel,
    onAction,
    children
}) => {
    return (
        <div className="course-assignment-box">
            {/* Assignment title and current status badge. */}
            <div className="cab-header">
                <h3 className="cab-title">{title}</h3>
                <span className={`cab-status cab-status--${status}`}>{status}</span>
            </div>
            <p className="cab-desc">{description}</p>
            {/* Deadline, grade weight, and optional action button. */}
            <div className="cab-meta">
                <div className="cab-meta-item">
                    <Calendar size={16} className="cab-meta-icon" />
                    Due: {deadline}
                </div>
                {weightPercent !== undefined && (
                    <span className="cab-weight">{weightPercent}% of final grade</span>
                )}
                {actionLabel && (
                    <button className="cab-action" onClick={onAction}>
                        {actionLabel}
                    </button>
                )}
            </div>
            {/* Extra nested content, such as submissions or grading UI. */}
            {children && <div className="cab-extra">{children}</div>}
        </div>
    );
};

export default CourseAssignmentBox;

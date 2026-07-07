// src/components/DescriptionBox.jsx
import React from 'react';
import { BookOpen } from 'lucide-react';
import './CourseDescriptionBox.css';

/**
 * Purpose: read-only course description panel.
 * Contract: receives already-loaded description text from the parent page.
 */
const DescriptionBox = ({ description }) => {
    return (
        <div className="course-description-box">
            {/* Header with icon and label. */}
            <div className="course-description-header">
                <BookOpen size={24} className="course-description-icon" />
                <h2 className="course-description-title">Course Description</h2>
            </div>
            {/* Description body. */}
            <p className="course-description-text">{description}</p>
        </div>
    );
};

export default DescriptionBox;

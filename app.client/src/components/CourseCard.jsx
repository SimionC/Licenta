// src/components/CourseCard.jsx
import React from 'react';
import './CourseCard.css';

/**
 * Purpose: minimal dashboard card for recent courses.
 * Contract: read-only display component.
 */

const CourseCard = ({ course }) => {
    return (
        <div className="course-card">
            {/* Simple icon/title display for a course preview. */}
            <div className="course-icon">📚</div>
            <h4 className="course-title">{course.title}</h4>
        </div>
    );
};

export default CourseCard;

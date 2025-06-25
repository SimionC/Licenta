// src/components/CourseCard.jsx
import React from 'react';
import './CourseCard.css';

const CourseCard = ({ course }) => {
    return (
        <div className="course-card">
            <div className="course-icon">📚</div>
            <h4 className="course-title">{course.title}</h4>
        </div>
    );
};

export default CourseCard;

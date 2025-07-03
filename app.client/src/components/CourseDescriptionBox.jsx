// src/components/DescriptionBox.jsx
import React from 'react';
import { BookOpen } from 'lucide-react';
import './CourseDescriptionBox.css';

const DescriptionBox = ({ description }) => {
    return (
        <div className="course-description-box">
            <div className="course-description-header">
                <BookOpen size={24} className="course-description-icon" />
                <h2 className="course-description-title">Course Description</h2>
            </div>
            <p className="course-description-text">{description}</p>
        </div>
    );
};

export default DescriptionBox;

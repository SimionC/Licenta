import React from 'react';
import './CoursesCourseCard.css';
import { BookOpen, Eye } from 'lucide-react';

const CoursesCourseCard = ({ course }) => {
    return (
        <div className="courses-card">
            <div className="courses-card-header">
                <BookOpen size={40} color="white" />
            </div>
            <div className="courses-card-body">
                <h5 className="courses-card-title">{course.title}</h5>
                <p className="courses-card-teacher">{course.teacherName}</p>
                <button className="courses-card-btn">
                    <Eye size={16} style={{ marginRight: '6px' }} />
                    View Course
                </button>
            </div>
        </div>
    );
};

export default CoursesCourseCard;

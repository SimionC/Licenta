import React from 'react';
import './CoursesCourseCard.css';
import { BookOpen, Eye } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

/**
 * Purpose: course card used in courses listing with route navigation.
 * Contract: expects course.id and course.title; teacherName is optional display field.
 */

const CoursesCourseCard = ({ course }) => {

    // Used to open the selected course details page.
    const navigate = useNavigate();

    return (
        <div className="courses-card">
            {/* Visual header/icon area. */}
            <div className="courses-card-header">
                <BookOpen size={40} color="white" />
            </div>
            {/* Course details and navigation action. */}
            <div className="courses-card-body">
                <div className="courses-card-title-row">
                    <h5 className="courses-card-title">{course.title}</h5>
                    {course.isClosed && <span className="courses-card-status">Closed</span>}
                </div>
                <p className="courses-card-teacher">{course.teacherName}</p>
                <button className="courses-card-btn"
                    onClick={() => navigate(`/courses/${course.id}`)}>
                    <Eye size={16} style={{ marginRight: '6px' }} />
                    View Course
                </button>
            </div>
        </div>
    );
};

export default CoursesCourseCard;

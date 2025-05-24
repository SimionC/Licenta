import React, { useState } from 'react';
import CreateCourseForm from '../components/CreateCourseForm';

const AllCoursesPage = () => {
    const [courses, setCourses] = useState([]);
    const [showForm, setShowForm] = useState(false);

    const handleCreateCourse = (newCourse) => {
        setCourses([...courses, newCourse]);
        setShowForm(false); // hide form after submission
    };

    return (
        <div>
            <h2>Courses</h2>

            <button onClick={() => setShowForm(!showForm)}>
                {showForm ? 'Cancel' : 'Create New Course'}
            </button>

            {showForm && <CreateCourseForm onCreate={handleCreateCourse} />}

            <ul>
                {courses.map(course => (
                    <li key={course.id}>
                        <strong>{course.title}</strong><br />
                        <small>{course.description}</small>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default AllCoursesPage;

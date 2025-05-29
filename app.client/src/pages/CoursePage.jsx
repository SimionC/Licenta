import { useParams } from 'react-router-dom';
import { useEffect, useState } from 'react';
import axios from 'axios';
import CreateCourseWorkForm from '../components/CreateCourseWorkForm';

export default function CoursePage() {
    const { courseId } = useParams();
    const [course, setCourse] = useState(null);
    const [courseWorks, setCourseWorks] = useState([]);

    useEffect(() => {
        // Get course info
        axios.get(`/api/Course/${courseId}`)
            .then(res => setCourse(res.data))
            .catch(err => console.error("Error loading course", err));

        // Save courseId to local storage
        const userEmail = localStorage.getItem("userEmail"); // Or another identifier you're already storing
        if (!userEmail) return;
        const key = `recentCourses_${userEmail}`;
        const existing = JSON.parse(localStorage.getItem(key) || '[]');
        const updated = [courseId, ...existing.filter(id => id !== courseId)].slice(0, 5);
        localStorage.setItem(key, JSON.stringify(updated));

        // Get assignments
        axios.get(`/api/Course/${courseId}/courseworks`)
            .then(res => setCourseWorks(res.data))
            .catch(err => console.error("Error loading courseworks", err));

    }, [courseId]);

    if (!course) return <div className="text-center mt-5">Loading course...</div>;

    return (
        <div className="container mt-4">
            <h2 className="mb-4">📘 {course.title}</h2>

            {/* Assignment creation form */}
            <CreateCourseWorkForm courseId={courseId} onCreated={(cw) =>
                setCourseWorks(prev => [...prev, cw])
            } />

            {/* Assignment list */}
            <h4 className="mt-5">Assignments</h4>
            {courseWorks.length === 0 ? (
                <p className="text-muted">No assignments yet.</p>
            ) : (
                <ul className="list-group">
                    {courseWorks.map((cw) => (
                        <li key={cw.id} className="list-group-item">
                            <strong>{cw.title}</strong><br />
                            <small>{cw.description}</small><br />
                            <small className="text-muted">📅 Deadline: {cw.deadline?.split('T')[0]}</small>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}

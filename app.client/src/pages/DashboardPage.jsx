import React from 'react';
import { useEffect, useState } from 'react';
import axios from 'axios';
import Sidebar from '../components/Sidebar';

const DashboardPage = () => {
    const [recentCourses, setRecentCourses] = useState([]);

    useEffect(() => {
        const userEmail = localStorage.getItem("userEmail");
        if (!userEmail) return;

        const key = `recentCourses_${userEmail}`;
        const stored = JSON.parse(localStorage.getItem(key) || '[]');

        Promise.all(
            stored.map(id => axios.get(`/api/Course/${id}`).then(res => res.data))
        ).then(setRecentCourses)
            .catch(err => console.error("Error loading recent courses", err));
    }, []);

    return (
        <div style={{ display: 'flex' }}>
            <Sidebar />
            <div style={{ marginLeft: '250px', padding: '2rem', width: '100%' }}>
                <h1>Welcome to your Dashboard</h1>

                <h4 className="mt-4">Recently Visited Courses</h4>
                {recentCourses.length === 0 ? (
                    <p className="text-muted">No recent activity yet.</p>
                ) : (
                    <ul className="list-group">
                        {recentCourses.map(course => (
                            <li key={course.id} className="list-group-item">
                                <a href={`/courses/${course.id}`} className="text-decoration-none">
                                    <strong>{course.title}</strong><br />
                                    <small>{course.description}</small>
                                </a>
                            </li>
                        ))}
                    </ul>
                )}


            </div>
        </div>


    );
};

export default DashboardPage;

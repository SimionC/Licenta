import React, { useEffect, useState } from 'react';
import axios from 'axios';
import Sidebar from '../components/Sidebar';
import CourseCard from '../components/CourseCard';
import NoteCard from '../components/NoteCard';
import AssignmentCard from '../components/AssignmentCard';
import Schedule from '../components/Schedule';
import { Clock } from "react-feather";
import { BookOpen, FileText } from 'lucide-react';

/**
 * Purpose: Landing dashboard with recent courses, notes, and upcoming assignments snapshot.
 * API touched: GET /api/Course/{id} for locally tracked recent IDs.
 * Side effects: reads localStorage key recentCourses_{userEmail}.
 */

const DashboardPage = () => {
    const [recentCourses, setRecentCourses] = useState([]);

    const mockCourses = [
        { id: 1, title: 'React Fundamentals' },
        { id: 2, title: 'UI/UX Design' },
        { id: 3, title: 'Databases' }
    ];

    const mockNotes = [
        { id: 1, title: 'Component Patterns' },
        { id: 2, title: 'API Integration' },
        { id: 3, title: 'Design Systems' }
    ];

    const mockAssignments = [
        {
            id: 1,
            title: "Component Architecture Assignment",
            course: "Object-Oriented Programming",
            due: "Due: Today, 11:59 PM",
            status: "urgent", // red
        },
        {
            id: 2,
            title: "SQL Query Optimization",
            course: "Databases",
            due: "Due: Tomorrow, 5:00 PM",
            status: "urgent", // yellow
        },
        {
            id: 3,
            title: "Market Analysis Project",
            course: "Quantitative Microeconomics",
            due: "Due: Mar 25, 2024",
            status: "warning", // green
        },
        {
            id: 4,
            title: "Portfolio Management Quiz",
            course: "Finance",
            due: "Due: Mar 30, 2024",
            status: "ok",
        },
    ];

    // useEffect recent list: resolves stored course IDs into full course cards.
    useEffect(() => {
        setRecentCourses(mockCourses);
    }, []);

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
        <div style={{ display: 'flex', backgroundColor: '#FBF6E9', minHeight: '100vh' }}>
            <Sidebar />
            <div style={{ flex: 1, padding: '3rem 4rem' }}>
                <div className="dashboard-main">
                    <div className="dashboard-left">
                        {/* Dashboard Title */}
                        <h1 className="dashboard-title">Dashboard</h1>

                        {/* Recent Courses Header */}
                        <div className="section-header">
                            <BookOpen size={20} color="#118B50" />
                            <h4>Recent Courses</h4>
                        </div>

                        {/* Course Cards */}
                        <div className="card-grid">
                            {recentCourses.map(course => (
                                <CourseCard key={course.id} course={course} />
                            ))}
                        </div>

                        {/* Recent Notes Header */}
                        <div className="section-header" style={{ marginTop: '3rem' }}>
                            <FileText size={20} color="#118B50" />
                            <h4>Recent Notes</h4>
                        </div>

                        {/* Notes Cards */}
                        <div className="card-grid">
                            {mockNotes.map(note => (
                                <NoteCard key={note.id} note={note} />
                            ))}
                        </div>

                        {/* Schedule */}
                        <Schedule />
                    </div>

                    <div className="dashboard-right">
                        <div className="section-header">
                            <Clock size={20} color="#118B50" />
                            <h4>Upcoming Assignments</h4>
                        </div>
                        {mockAssignments.map(assign => (
                            <AssignmentCard key={assign.id} assignment={assign} />
                        ))}
                    </div>
                </div>

            </div>
        </div>
    );

};

export default DashboardPage;

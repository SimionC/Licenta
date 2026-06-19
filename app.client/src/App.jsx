import React from 'react';
import { Routes, Route } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap.min.css';
import { useAuth } from './hooks/useAuth';
// Import components
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import AllCoursesPage from './pages/AllCoursesPage';
import CoursePage from './pages/CoursePage';
import NotesPage from './pages/NotesPage';
import NoteEditorPage from './pages/NoteEditorPage';
import CollaborationDetailPage from './pages/CollaborationDetailPage';
import ProtectedRoute from './components/ProtectedRoute';

//IMPORT MARKDOWN EDITOR
import 'katex/dist/katex.min.css';       //npm install react-markdown remark-gfm remark-math rehype-katex katex
import 'highlight.js/styles/github.css'; //npm install rehype-highlight highlight.js

/**
 * Purpose: Central route table and top-level auth bootstrap for userType/email.
 * API touched: GET /api/Auth/Me (fetch + axios).
 * State contract: userType drives role-based pages (teacher/student) and localStorage mirrors identity.
 */

// Runs once to hydrate identity from cookie session and persist basic profile.

function App() {
    const { user } = useAuth();
    const userType = user?.userType || "";

    return (
        <Routes>
            {/* Public Routes */}
            <Route path="/" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/courses/all" element={<AllCoursesPage userType={userType} />} />
            <Route path="/courses/:courseId" element={<CoursePage />} />
            {/* Notes - protected only (create/edit routes are protected below) */}
            {/*<Route path="/notes/:noteGuid" element={<NoteEditorPage />} />*/}
            {/* protected note routes */}
            {/* Wrap components that require authentication with ProtectedRoute */}

            <Route path="/notes" element={
                <ProtectedRoute>
                    <NotesPage />
                </ProtectedRoute>
            } />

            {/* Route for creating a new regular note */}
            <Route path="/notes/new" element={
                <ProtectedRoute>
                    <NoteEditorPage />
                </ProtectedRoute>
            } />

            {/* Route for editing an existing regular note */}
            <Route path="/notes/:noteGuid" element={
                <ProtectedRoute>
                    <NoteEditorPage />
                </ProtectedRoute>
            } />

            {/* Route for viewing collaboration details (e.g., listing notes in a collaboration) */}
            <Route path="/collaborations/:collabId" element={
                <ProtectedRoute>
                    <CollaborationDetailPage />
                </ProtectedRoute>
            } />

            <Route path="/collaborations/:collabId/notes/new" element={
                <ProtectedRoute>
                    <NoteEditorPage />
                </ProtectedRoute>
            } />

            <Route path="/collaborations/:collabId/notes/:noteGuid" element={
                <ProtectedRoute>
                    <NoteEditorPage />
                </ProtectedRoute>
            } />

        </Routes>

    );
}

export default App;

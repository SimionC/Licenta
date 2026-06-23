import React from 'react';
import { Routes, Route } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css';
import { useAuth } from './hooks/useAuth';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import AllCoursesPage from './pages/AllCoursesPage';
import CoursePage from './pages/CoursePage';
import NotesPage from './pages/NotesPage';
import NoteEditorPage from './pages/NoteEditorPage';
import CollaborationDetailPage from './pages/CollaborationDetailPage';
import AccountsPage from './pages/AccountsPage';
import AccountPage from './pages/AccountPage';
import ChangePasswordPage from './pages/ChangePasswordPage';
import ProtectedRoute from './components/ProtectedRoute';
import PasswordChangeRoute from './components/PasswordChangeRoute';

//IMPORT MARKDOWN EDITOR
import 'katex/dist/katex.min.css';       //npm install react-markdown remark-gfm remark-math rehype-katex katex
import 'highlight.js/styles/github.css'; //npm install rehype-highlight highlight.js

/**
 * Purpose: Central route table and top-level auth bootstrap for userType/email.
 * API touched: GET /api/Auth/Me.
 * State contract: userType drives role-based pages and localStorage mirrors identity.
 */

function App() {
    const { user } = useAuth();
    const userType = user?.userType || "";

    React.useEffect(() => {
        const savedTheme = localStorage.getItem('theme') || 'light';
        document.documentElement.dataset.theme = savedTheme;
    }, []);

    return (
        <Routes>
            <Route path="/" element={<LoginPage />} />
            <Route path="/change-password" element={
                <PasswordChangeRoute>
                    <ChangePasswordPage />
                </PasswordChangeRoute>
            } />

            <Route path="/dashboard" element={
                <ProtectedRoute>
                    <DashboardPage />
                </ProtectedRoute>
            } />

            <Route path="/courses/all" element={
                <ProtectedRoute>
                    <AllCoursesPage userType={userType} />
                </ProtectedRoute>
            } />

            <Route path="/courses/:courseId" element={
                <ProtectedRoute>
                    <CoursePage />
                </ProtectedRoute>
            } />

            <Route path="/accounts" element={
                <ProtectedRoute>
                    <AccountsPage />
                </ProtectedRoute>
            } />

            <Route path="/account" element={
                <ProtectedRoute>
                    <AccountPage />
                </ProtectedRoute>
            } />

            <Route path="/notes" element={
                <ProtectedRoute>
                    <NotesPage />
                </ProtectedRoute>
            } />

            <Route path="/notes/new" element={
                <ProtectedRoute>
                    <NoteEditorPage />
                </ProtectedRoute>
            } />

            <Route path="/notes/:noteGuid" element={
                <ProtectedRoute>
                    <NoteEditorPage />
                </ProtectedRoute>
            } />

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

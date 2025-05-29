import { useState } from 'react';
import axios from 'axios';

export default function CreateCourseWorkForm({ courseId, onCreated }) {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [deadline, setDeadline] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            const res = await axios.post(`/api/Course/${courseId}/coursework`, {
                title,
                description,
                deadline,
            });
            onCreated(res.data); // notify parent
            setTitle('');
            setDescription('');
            setDeadline('');
        } catch (err) {
            console.error('Failed to create coursework:', err);
            alert('Something went wrong');
        }
    };

    return (
        <form onSubmit={handleSubmit} className="mb-4">

            <h4>Create Assignment</h4>
            <div className="mb-2">
                <input
                    type="text"
                    className="form-control"
                    placeholder="Title"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    required
                />
            </div>
            <div className="mb-2">
                <textarea
                    className="form-control"
                    placeholder="Description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                />
            </div>
            <div className="mb-3">
                <input
                    type="date"
                    className="form-control"
                    value={deadline}
                    onChange={(e) => setDeadline(e.target.value)}
                    required
                />
            </div>
            <button type="submit" className="btn btn-primary">Create</button>
        </form>
    );
}

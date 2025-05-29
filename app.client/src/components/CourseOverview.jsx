import { useState } from 'react';

export default function CourseOverview({ course, onUpdateCourse }) {
    const [editMode, setEditMode] = useState(false);
    const [title, setTitle] = useState(course.title);
    const [description, setDescription] = useState(course.description);

    const handleSave = () => {
        onUpdateCourse({ ...course, title, description });
        setEditMode(false);
    };

    return (
        <div className="card p-4 mb-4 shadow-sm">
            {editMode ? (
                <>
                    <div className="mb-3">
                        <label className="form-label">Course Title</label>
                        <input
                            type="text"
                            className="form-control"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                        />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Description</label>
                        <textarea
                            className="form-control"
                            rows="3"
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                        />
                    </div>
                    <button className="btn btn-success me-2" onClick={handleSave}>Save</button>
                    <button className="btn btn-secondary" onClick={() => setEditMode(false)}>Cancel</button>
                </>
            ) : (
                <>
                    <h2>{title}</h2>
                    <p>{description}</p>
                    <button className="btn btn-outline-primary" onClick={() => setEditMode(true)}>Edit Course Info</button>
                </>
            )}
        </div>
    );
}

import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';

/**
 * Purpose: Collaboration detail reader and note list navigator.
 * API touched: GET /api/Collaborations/{collabId}.
 * Output contract: renders collaboration metadata and links to collaboration-note editor route.
 */

export default function CollaborationDetailPage() {
    const { collabId } = useParams();
    const [collab, setCollab] = useState(null);

    useEffect(() => {
        Promise.all([
            fetch(`/api/Collaborations/${collabId}`, { credentials: 'include' }),
            fetch(`/api/Notes/collaboration/${collabId}`, { credentials: 'include' })
        ])
            .then(async ([collabRes, notesRes]) => {
                if (!collabRes.ok) throw new Error('Failed to load collaboration');

                const collabData = await collabRes.json();
                const notesData = notesRes.ok ? await notesRes.json() : [];

                setCollab({
                    ...collabData,
                    notes: Array.isArray(notesData) ? notesData : []
                });
            })
            .catch(() => setCollab(null));
    }, [collabId]);

    if (!collab) return <div>Loading…</div>;

    return (
        <div className="collaboration-detail">
            <h1>📒 {collab.name}</h1>
            <section>
                <h2>Notes</h2>
                <ul>
                    {collab.notes?.map(n => (
                        <li key={n.guid}>
                            <Link to={`/collaborations/${collabId}/notes/${n.guid}`}>
                                {n.title || 'Untitled'}
                            </Link>
                        </li>
                    ))}
                </ul>
            </section>
        </div>
    );
}

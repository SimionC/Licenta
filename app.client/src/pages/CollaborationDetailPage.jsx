import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';

export default function CollaborationDetailPage() {
    const { collabId } = useParams();
    const [collab, setCollab] = useState(null);

    useEffect(() => {
        fetch(`/api/Collaborations/${collabId}`, { credentials: 'include' })
            .then(r => r.json())
            .then(setCollab);
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

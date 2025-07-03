// src/components/ResourceCard.jsx
import React from 'react';
import './ResourceCard.css';
import { FileText } from 'lucide-react';

const ResourceCard = ({ title, type, size, uploadedAt }) => {
    return (
        <div className="resource-card">
            <div className="resource-left">
                <div className="resource-icon">
                    <FileText size={28} />
                </div>
                <div className="resource-info">
                    <h5 className="resource-title">{title}</h5>
                    <p className="resource-meta">
                        Type: {type} &nbsp;&bull;&nbsp; Size: {size} &nbsp;&bull;&nbsp; Uploaded: {uploadedAt}
                    </p>
                </div>
            </div>
            <button className="resource-download-btn">Download</button>
        </div>
    );
};

export default ResourceCard;

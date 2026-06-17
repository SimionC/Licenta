// src/components/ResourceCard.jsx
import React from 'react';
import './ResourceCard.css';
import { Download, FileText, Trash2 } from 'lucide-react';

const ResourceCard = ({ title, type, size, uploadedAt, canManage, onDownload, onDelete }) => {
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
            <div className="resource-actions">
                <button className="resource-download-btn" onClick={onDownload}>
                    <Download size={16} />
                    Download
                </button>
                {canManage && (
                    <button className="resource-delete-btn" onClick={onDelete} title="Remove resource">
                        <Trash2 size={16} />
                    </button>
                )}
            </div>
        </div>
    );
};

export default ResourceCard;

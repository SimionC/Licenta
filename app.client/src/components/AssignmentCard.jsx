// components/AssignmentCard.jsx
import { AlertCircle, Circle } from "react-feather";

const statusColor = {
    urgent: "#E74C3C",
    warning: "#F1C40F",
    ok: "#2ECC71"
};

const AssignmentCard = ({ assignment }) => {
    return (
        <div style={{
            backgroundColor: "#fff",
            borderRadius: "1rem",
            padding: "1rem",
            marginBottom: "1rem",
            boxShadow: "0 0 4px rgba(0,0,0,0.08)",
            border: `1px solid ${statusColor[assignment.status] || '#ccc'}`,
            position: "relative"
        }}>
            <div style={{ fontWeight: 'bold', color: '#18230F', fontSize: '0.95rem' }}>
                {assignment.title}
            </div>
            <div style={{ color: "#118B50", fontSize: "0.85rem", margin: '0.4rem 0' }}>
                {assignment.course}
            </div>
            <div style={{
                backgroundColor: `${assignment.status === 'urgent' ? '#FDEDEC' : assignment.status === 'warning' ? '#FCF3CF' : '#EAFAF1'}`,
                color: statusColor[assignment.status],
                padding: "0.2rem 0.6rem",
                borderRadius: "12px",
                fontSize: "0.75rem",
                display: "inline-block"
            }}>
                {assignment.due}
            </div>
            {assignment.status === 'urgent' && (
                <AlertCircle size={16} color={statusColor.urgent} style={{ position: "absolute", top: "1rem", right: "1rem" }} />
            )}
            <Circle
                size={8}
                fill={statusColor[assignment.status]}
                style={{ position: "absolute", bottom: "1rem", right: "1rem" }}
            />
        </div>
    );
};

export default AssignmentCard;

import React from 'react';
import './Schedule.css';
import { Folder } from 'lucide-react';

const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
const times = [
    '7:30', '9:00', '10:30', '12:00', '13:30',
    '15:00', '16:30', '18:00', '19:30'
];

const scheduleData = {
    'Monday-9:00': { course: 'Finance' },
    'Monday-10:30': { course: 'Microeconomics' },
    'Monday-12:00': { course: 'Databases' },
    'Tuesday-12:00': { course: 'React Fundamentals' },
    'Tuesday-13:30': { course: 'UI/UX Design' },
    'Thursday-15:00': { course: 'Advanced JavaScript' },
    'Thursday-16:30': { course: 'Management' },
    'Thursday-18:00': { course: 'Probabilities' },
    'Friday-12:00': { course: 'Data Structures' },
    'Friday-13:30': { course: 'Multimedia' },
};

const Schedule = () => {
    return (
        <div className="schedule-wrapper">
            <div className="schedule-header">
                <Folder size={20} className="header-icon" />
                <h4>Schedule</h4>
            </div>

            <div className="schedule-table">
                <div className="schedule-row header-row">
                    <div className="time-cell">Time</div>
                    {days.map(day => (
                        <div key={day} className="day-header">{day}</div>
                    ))}
                </div>

                {times.map(time => (
                    <div key={time} className="schedule-row">
                        <div className="time-cell">{time}</div>
                        {days.map(day => {
                            const key = `${day}-${time}`;
                            const slot = scheduleData[key];
                            return (
                                <div key={key} className={`schedule-cell ${slot ? 'filled' : ''}`}>
                                    {slot && (
                                        <>
                                            <span className="course-title">{slot.course}</span>
                                            <Folder size={20} className="course-icon" />
                                        </>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                ))}
            </div>
        </div>
    );
};

export default Schedule;

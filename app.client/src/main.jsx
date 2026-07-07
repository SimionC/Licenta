//wrap the app in a router - to navigate between pages
import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App.jsx';
import 'bootstrap/dist/css/bootstrap.min.css';

/**
 * Purpose: is the React starting point. It mounts the application into the HTML root element, 
 * loads Bootstrap styling, and wraps the app in BrowserRouter so the platform can navigate 
 * between pages without reloading the whole site
 */

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>    
        <BrowserRouter>
            <App />
        </BrowserRouter>
    </React.StrictMode>
);


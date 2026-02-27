//wrap the app in a router - to navigate between pages
import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App.jsx';
import 'bootstrap/dist/css/bootstrap.min.css';

/**
 * Purpose: Client entrypoint that mounts React app and enables router context.
 * API touched: none directly.
 * Core dependency: BrowserRouter wrapping App for route-based navigation.
 */

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>    
        <BrowserRouter>
            <App />
        </BrowserRouter>
    </React.StrictMode>
);


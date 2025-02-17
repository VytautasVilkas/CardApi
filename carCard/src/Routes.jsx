// src/Routes.js
import React from 'react';
import { Routes, Route } from 'react-router-dom';
import Login from './pages/Login';
import Menu from './pages/Menu';
import PrivateRoute from './components/PrivateRoute';
function AppRoutes() {
  return (
    <Routes>
      <Route path="/Prisijungti" element={<Login />} />
      <Route 
        path="/" 
        element={
          <PrivateRoute>
            <Menu />
          </PrivateRoute>
        } 
      />
    </Routes>
  );
}

export default AppRoutes;
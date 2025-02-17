import React from "react";
import { Navigate } from "react-router-dom";
import { useUser } from "../context/UserContext";
const PrivateRoute = ({ children }) => {
  const { isAuthenticated, isLoading } = useUser();
  if (isLoading) {
    return <div>Tvirtinama tapatybė...</div>; 
  }
  return isAuthenticated ? children : <Navigate to="/Prisijungti" />;
};
export default PrivateRoute;


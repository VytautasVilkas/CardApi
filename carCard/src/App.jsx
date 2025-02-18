import React from "react";
import AppRoutes from "./Routes";
import { BrowserRouter as Router } from "react-router-dom";
import "./App.css";
import Layout from "./components/Layout";
import { UserProvider } from "./context/UserContext";

function App() {
  return (
    <UserProvider>
      
      <Layout>
        <Router>
          <AppRoutes />
        </Router>
      </Layout>
      
    </UserProvider>
  );
}

export default App;
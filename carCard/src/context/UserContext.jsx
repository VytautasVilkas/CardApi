import React, { createContext, useContext, useState, useEffect, useRef } from "react";
import * as UserService from "../services/UserService"; // Namespace import for all named exports
import { setLogoutHandler } from "./authManager";

const UserContext = createContext();
export const useUser = () => useContext(UserContext);

export const UserProvider = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [cliId, setCliId] = useState("");
  const [cliName,setCliName] =useState("");
  const [role, setRole] = useState(null);
  const [Name, setName] = useState(null);
  const [Surname, setSurname] = useState(null);
  const [Username,setUsername] = useState(null);
  const [isLoading, setIsLoading] = useState(true);  
  
  const isMounted = useRef(false);

  const forceLogout = () => {
    setIsAuthenticated(false);
    setRole(null);
    setName(null);
    setSurname(null);
    setCliId("");
    setUsername("");
  };

  useEffect(() => {
    const initialize = async () => {
      if (isMounted.current) return;
      isMounted.current = true;
      try {
        const verifyResponse = await UserService.verifyToken();
        console.log("verifyResponse:", verifyResponse);
        if (verifyResponse && verifyResponse.isValid) {
          setIsAuthenticated(true);
          if (verifyResponse.role) {
            setRole(verifyResponse.role);
          }
          if (verifyResponse.name) {
            setName(verifyResponse.name);
          }
          if (verifyResponse.surname) {
            setSurname(verifyResponse.surname);
          }
          if (verifyResponse.clI_ID
          ) {
            setCliId(verifyResponse.clI_ID
            );
          }
          if (verifyResponse.username) {
            setUsername(verifyResponse.username);
          }
        } else {
          setIsAuthenticated(false);
          setRole(null);
          setName(null);
          setSurname(null);
          setCliId("");
          setCliName("");
          setUsername("");
        }
      } catch (verifyError) {
        console.error("Token verification failed:", verifyError);
        setIsAuthenticated(false);
        setRole(null);
        setName(null);
        setSurname(null);
        setCliId("");
        setCliName("");
        setUsername("");
      } finally {
        setIsLoading(false);  
      }
    };
    
    initialize();
    setLogoutHandler(async () => {
      forceLogout();
    });
  }, []);
  const logout = async () => {
    try {
      await UserService.logout();
      setIsAuthenticated(false);
      setRole(null);
      setName(null);
      setSurname(null);
      setCliId("");
      setCliName("");
      setUsername("");
    } catch (error) {
      throw error;
    }
  };
  const login = async (credentials) => {
    try {
      const loginResponse = await UserService.login(credentials);
      console.log("Login response:", loginResponse);
      if (loginResponse && loginResponse.isValid) {
        setIsAuthenticated(true);
        if (loginResponse.role) {
          setRole(loginResponse.role);
        }
        if (loginResponse.name) {
          setName(loginResponse.name);
        }
        if (loginResponse.surname) {
          setSurname(loginResponse.surname);
        }
        if (loginResponse.clI_ID) {
          setCliId(loginResponse.clI_ID);
        }
        if (loginResponse.username) {
          setUsername(loginResponse.username);
        }
      }
    } catch (error) {
      throw error;
    }
  };

  return (
    <UserContext.Provider value={{ Username, cliName, setCliName ,isAuthenticated, role, Name, Surname, isLoading ,cliId,setCliId, login, logout, forceLogout }}>
      {children}
    </UserContext.Provider>
  );
};

export default UserProvider;

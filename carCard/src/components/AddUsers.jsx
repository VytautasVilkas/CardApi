import React, { useState,useEffect } from "react";
import { addUser } from "../services/UserService"; // Ensure addUser exists
import UsersList from "./Users";
import { useUser } from "../context/UserContext";
const AddUsers = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [name, setName] = useState("");
  const [surname, setSurname] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [showUsers, setShowUsers] = useState(true);
  const [refreshUsers, setrefreshUsers] = useState(0);
  const {cliId} = useUser();

  
  
  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setMessage("");
    if (!username.trim() || !password.trim() || !name.trim() || !surname.trim()){
      setError("Visi laukai privalomi.");
      return;
    }
    setLoading(true);
    const newUser = {
      USERNAME: username,
      PASSWORD: password,
      NAME: name,
      SURNAME: surname,
      CLI_ID: cliId || ""
    };
    console.log(newUser);
    try {
      const response = await addUser(newUser);
      setMessage(response.message || "Vartotojas sėkmingai pridėtas!");
      setUsername("");
      setPassword("");
      setName("");
      setSurname("");
      setrefreshUsers((prev) => prev + 1);
    } catch(err) {
      setError(err.response?.data?.message || err.message);
    }
    setLoading(false);
  };

  return (
    <div className="flex flex-col items-center justify-center px-4 py-8 min-h-screen">
      {/* Add User Form Container */}
      <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl p-4 sm:p-6 border border-gray-300 rounded-lg bg-white shadow-md mb-8">
        <h2 className="text-xl font-bold mb-4 text-center">Pridėti vartotoją</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-900">Vartotojo vardas</label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              className="w-full border border-gray-300 p-2 rounded"
              placeholder="Įveskite vartotojo vardą"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-900">Slaptažodis</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              className="w-full border border-gray-300 p-2 rounded"
              placeholder="Įveskite slaptažodį"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-900">Vardas</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              className="w-full border border-gray-300 p-2 rounded"
              placeholder="Įveskite vardą"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-900">Pavardė</label>
            <input
              type="text"
              value={surname}
              onChange={(e) => setSurname(e.target.value)}
              required
              className="w-full border border-gray-300 p-2 rounded"
              placeholder="Įveskite pavardę"
            />
          </div>
          
          {error && <p className="text-red-600">{error}</p>}
          {message && <p className="text-green-600">{message}</p>}
          <button
            type="submit"
            disabled={loading}
            className="w-full bg-green-300 border border-gray-400 hover:text-black hover:border-gray-500 text-white py-2 rounded hover:bg-green-200 hover:scale-105 transition-transform duration-200"
          >
            {loading ? "Pateikiama..." : "Pridėti vartotoją"}
          </button>
        </form>
      </div>

      {/* Users List Section */}
      <div className="w-full max-w-7xl mx-auto mb-8 space-y-8">
        <div>
          <div className="flex items-center justify-between bg-gradient-to-r from-blue-300 to-blue-300 text-white px-8 py-2 rounded-t shadow-lg gap-x-4">
            <h3 className="text-xl font-semibold">Vartotojų duomenys</h3>
          <button
            onClick={() => setShowUsers((prev) => !prev)}
            className="bg-white text-purple-600 px-4 py-2 rounded shadow hover:bg-purple-100 transition-colors duration-200 text-xs"
          >
            {showUsers ? "Slėpti" : "Rodyti"}
          </button>
        </div>
        {showUsers && (
          <div className="bg-white p-8 rounded-b shadow-xl">
            <UsersList refreshUsers={refreshUsers}/>
          </div>
        )}
      </div>
      </div>
    </div>
  );
};

export default AddUsers;

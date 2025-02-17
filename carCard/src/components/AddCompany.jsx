import React, { useState } from "react";
import { AddCliCompany } from "../services/UserService"; // Make sure this path is correct

const AddCompany = () => {
  const [companyName, setCompanyName] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");

    if (!companyName.trim()) {
      setError("Įveskite įmonės pavadinimą.");
      return;
    }

    setLoading(true);
    
    try {
        const payload ={
            COMPANY_NAME: companyName  
        }
      const response = await AddCliCompany(payload);
      setSuccess(response.message || "Įmonė sėkmingai pridėta!");
      setCompanyName("");
    } catch (err) {
      setError(err.response?.data?.message || err.message);
    }
    setLoading(false);
  };

  return (
    <div className="max-w-md mx-auto bg-white p-6 rounded shadow">
      <h2 className="text-xl font-bold mb-4 text-center">Pridėti įmonę</h2>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-900">
            Įmonės pavadinimas
          </label>
          <input
            type="text"
            value={companyName}
            onChange={(e) => setCompanyName(e.target.value)}
            placeholder="Įveskite įmonės pavadinimą"
            required
            className="w-full border border-gray-300 p-2 rounded"
          />
        </div>
        {error && <p className="text-red-600">{error}</p>}
        {success && <p className="text-green-600">{success}</p>}
        <button
          type="submit"
          disabled={loading}
          className="w-full bg-green-300 text-white py-2 rounded hover:bg-green-200 hover:scale-105 transition-transform duration-200"
        >
          {loading ? "Pateikiama..." : "Pateikti"}
        </button>
      </form>
    </div>
  );
};

export default AddCompany;

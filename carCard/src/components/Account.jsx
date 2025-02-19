import React, { useState, useEffect } from "react";
import { useUser } from "../context/UserContext";
import { downloadExcelFileAll } from "../service/Service";

const Account = () => {
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const [loading, setLoading] = useState(false);
  const { cliId } = useUser();

  useEffect(() => {
    // any additional logic on mount
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);
    try {
      await downloadExcelFileAll();
      setSuccess("Atsiųsta sėkmingai");
    } catch (error) {
      setError("Klaida: " + error);
    } finally { 
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col items-center justify-center px-4 py-8 min-h-screen">
      <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl p-4 sm:p-6 border border-gray-300 rounded-lg bg-white shadow-md mb-8">
        <h2 className="text-xl font-bold mb-4">Gauti ataskaitą</h2>
        {/* Display error and success messages */}
        {error && <p className="mb-4 text-red-600">{error}</p>}
        {success && <p className="mb-4 text-green-600">{success}</p>}
        
        <form onSubmit={handleSubmit}>
          <button
            type="submit"
            disabled={loading}
            className="w-full bg-green-300 border border-gray-400 hover:text-black hover:border-gray-500 text-white py-2 rounded hover:bg-green-200 hover:scale-105"
          >
            {loading ? "Gaunama..." : "Gauti"}
          </button>
        </form>
      </div>
      {/* cars List Section */}
    </div>
  );
};

export default Account;

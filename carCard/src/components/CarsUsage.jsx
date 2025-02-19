import React, { useState, useEffect } from "react";
import { getCarsUsage, updateCarsUsage,getCarsUsageAdmin,updateCarsUsageAdmin   } from "../services/TripService";
import { useUser } from "../context/UserContext";
const CarsUsage = ({ refreshUsage, selectedUserId }) => {
  const [carsUsage, setCarsUsage] = useState([]);
  const [error, setError] = useState("");
  const {role} = useUser();
  const [searchFrom, setSearchFrom] = useState("");
  const [searchTo, setSearchTo] = useState("");

  const fetchCarsUsage = async () => {
    try {
      const response = await getCarsUsage(searchFrom,searchTo);
      if (response) {
        setCarsUsage(response);
        setError("");
      }
    } catch (err) {
      if (err.response && err.response.data && err.response.data.message) {
        setError(err.response.data.message);
      } else {
        setError(err.message);
      }
    }
  };
  const fetchCarsUsageAdmin = async () => {
    try {
      const response = await getCarsUsageAdmin(searchFrom,searchTo,selectedUserId);
      if (response) {
        setCarsUsage(response);
        setError("");
      }
    } catch (err) {
      if (err.response && err.response.data && err.response.data.message) {
        setError(err.response.data.message);
      } else {
        setError(err.message);
      }
    }
  };
  useEffect(() => {
    if(role == "2"){
    fetchCarsUsage();
    }
    if(role == "1" && selectedUserId){
    fetchCarsUsageAdmin();
    }
  }, [refreshUsage, selectedUserId]);

  const handleFieldChange = (id, fieldName, value) => {
    setCarsUsage((prev) =>
      prev.map((record) =>
        record.CAU_ID === id ? { ...record, [fieldName]: value } : record
      )
    );
  };

  const handleUpdate = async (record) => {
    
    if(role == "2"){
    try {
      const response = await updateCarsUsage(record);
      alert(response.message);
    } catch (err) {
      alert("Error updating record: " + (err.response?.data?.message || err.message));
    }
    }
    if(role == "1" &&  selectedUserId){
      try {
        const response = await updateCarsUsageAdmin(record, selectedUserId);
        alert(response.message);
      } catch (err) {
        alert("Error updating record: " + (err.response?.data?.message || err.message));
      }
      }
  };
  const handleSearch = async () => {
    if(role == "2"){
      fetchCarsUsage();
      }
      if(role == "1" && selectedUserId){
      fetchCarsUsageAdmin();
      }
    };

  const formatDate = (dateString) => {
    if (!dateString) return "";
    return dateString.split("T")[0];
  };

  return (
    <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl max-w-[400px] mx-auto overflow-x-auto">
    <div className="flex flex-col sm:flex-row items-center gap-2 mb-4">
      <input
        type="date"
        value={searchFrom}
        onChange={(e) => setSearchFrom(e.target.value)}
        className="w-full sm:w-1/4 border border-gray-300 p-2 rounded"
      />
      <input
        type="date"
        value={searchTo}
        onChange={(e) => setSearchTo(e.target.value)}
        className="w-full sm:w-1/4 border border-gray-300 p-2 rounded"
      />
      <button
        onClick={handleSearch}
        className="ml-4 bg-blue-300 border border-gray-400 hover:text-black hover:border-gray-500 text-white px-3 py-1 rounded hover:bg-blue-200 hover:scale-101 transition-all duration-200"      
      >
        Ieškoti
      </button>
    </div>
      {error && <p className="text-red-600">{error}</p>}
      {carsUsage.length > 0 ? (
        <table className="w-full border-collapse border border-gray-300 text-xs sm:text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Data</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Rida nuo</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Rida iki</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Kiekis nuo</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Kiekis iki</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Veiksmas</th>
            </tr>
          </thead>
          <tbody>
            {carsUsage.map((record, index) => (
              <tr
                key={record.CAU_ID}
                className={index % 2 === 0 ? "bg-white" : "bg-gray-50"}
              >
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  {formatDate(record.CAU_DATE)}
                </td>
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <input
                    type="number"
                    value={record.CAU_ODO_FROM}
                    readOnly
                    onChange={(e) =>
                      handleFieldChange(record.CAU_ID, "CAU_ODO_FROM", e.target.value)
                    }
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <input
                    type="number"
                    value={record.CAU_ODO_TO}
                    onChange={(e) =>
                      handleFieldChange(record.CAU_ID, "CAU_ODO_TO", e.target.value)
                    }
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <input
                    type="number"
                    value={record.CAU_QTU_FROM}
                    onChange={(e) =>
                      handleFieldChange(record.CAU_ID, "CAU_QTU_FROM", e.target.value)
                    }
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <input
                    type="number"
                    value={record.CAU_QTY_TO}
                    onChange={(e) =>
                      handleFieldChange(record.CAU_ID, "CAU_QTY_TO", e.target.value)
                    }
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <button
                    onClick={() => handleUpdate(record)}
                    className="transform transition duration-200 hover:scale-110 p-2 rounded"
                  >
                    <img
                      src="https://img.icons8.com/ios/50/installing-updates--v1.png"
                      alt="Update"
                      className="w-6 h-6"
                    />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <p className="text-sm text-gray-600"></p>
      )}
    </div>
  );
};

export default CarsUsage;

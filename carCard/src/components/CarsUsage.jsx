import React, { useState, useEffect } from "react";
import { getCarsUsage, updateCarsUsage } from "../services/TripService";

const CarsUsage = ({ refreshUsage }) => {
  const [carsUsage, setCarsUsage] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    const fetchCarsUsage = async () => {
      try {
        const response = await getCarsUsage();
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

    fetchCarsUsage();
  }, [refreshUsage]);

  const handleFieldChange = (id, fieldName, value) => {
    setCarsUsage((prev) =>
      prev.map((record) =>
        record.CAU_ID === id ? { ...record, [fieldName]: value } : record
      )
    );
  };

  const handleUpdate = async (record) => {
    try {
      const response = await updateCarsUsage(record);
      alert(response.message);
    } catch (err) {
      alert("Error updating record: " + (err.response?.data?.message || err.message));
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return "";
    return dateString.split("T")[0];
  };

  return (
    // Outer container: fixed maximum width, centered, and horizontal scroll enabled.
    <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl max-w-[400px] mx-auto overflow-x-auto">
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

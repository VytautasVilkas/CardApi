import React, { useState, useEffect } from "react";
import { getCardsUsage} from "../services/TripService";
const CardsUsage = () => {
  const [cardsUsage, setCardsUsage] = useState([]);
  const [error, setError] = useState("");

  // Search criteria state variables:
  const [searchQuery, setSearchQuery] = useState("");
  const [searchFrom, setSearchFrom] = useState(""); 
  const [searchTo, setSearchTo] = useState("");     

  useEffect(() => {
    const fetchInitialData = async () => {
      try {
        const response = await getCardsUsage("", "", "");
        if (response) {
          setCardsUsage(response);
          setError("");
        }
      } catch (err) {
        setError(err.response?.data?.message || err.message);
      }
    };

    fetchInitialData();
  }, []);

  // This function is triggered by the search button.
  const handleSearch = async () => {
    try {
      const response = await getCardsUsage(searchQuery, searchFrom, searchTo);
      if (response) {
        setCardsUsage(response);
        setError("");
      }
    } catch (err) {
      setError(err.response?.data?.message || err.message);
    }
  };

  // Format date for display (only the date part)
  const formatDate = (dateString) => {
    if (!dateString) return "";
    return dateString.split("T")[0];
  };

  return (
    <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl mx-auto overflow-x-auto">
      {/* Search Strip */}
      <div className="flex flex-col sm:flex-row items-center gap-2 mb-4">
        <input
          type="text"
          placeholder="Ieškoti pagal vietą..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full sm:w-1/3 border border-gray-300 p-2 rounded"
        />
        <input
          type="date"
          value={searchFrom}
          onChange={(e) => setSearchFrom(e.target.value)}
          className="w-full sm:w-1/3 border border-gray-300 p-2 rounded"
        />
        <input
          type="date"
          value={searchTo}
          onChange={(e) => setSearchTo(e.target.value)}
          className="w-full sm:w-1/3 border border-gray-300 p-2 rounded"
        />
        <button
          onClick={handleSearch}
          className="bg-blue-500 text-white px-4 py-2 rounded"
        >
          Ieškoti
        </button>
      </div>

      {error && <p className="text-red-600">{error}</p>}
      {cardsUsage.length > 0 ? (
        <table className="w-full border-collapse border border-gray-300 text-xs sm:text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="border border-gray-300 p-2">Data</th>
              <th className="border border-gray-300 p-2">Kuro tipas</th>
              <th className="border border-gray-300 p-2">Kiekis</th>
              <th className="border border-gray-300 p-2">Čekio numeris</th>
              <th className="border border-gray-300 p-2">Vieta</th>
              <th className="border border-gray-300 p-2">Šalis</th>
              <th className="border border-gray-300 p-2">Suma</th>
            </tr>
          </thead>
          <tbody>
            {cardsUsage.map((record, index) => (
              <tr
                key={index}
                className={index % 2 === 0 ? "bg-white" : "bg-gray-50"}
              >
                <td className="border border-gray-300 p-2">{formatDate(record.FCU_DATE)}</td>
                <td className="border border-gray-300 p-2">{record.FUEL_NAME}</td>
                <td className="border border-gray-300 p-2">{record.FCU_QTY}</td>
                <td className="border border-gray-300 p-2">{record.FCU_CHECK_NUMBER}</td>
                <td className="border border-gray-300 p-2">{record.FCU_FUELING_PLACE_NAME}</td>
                <td className="border border-gray-300 p-2">{record.FCU_COUNTRY}</td>
                <td className="border border-gray-300 p-2">{record.FCU_TOTAL_AMOUNT}</td>
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

export default CardsUsage;

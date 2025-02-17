import React, { useState, useEffect } from "react";
import { addCard } from "../services/CardService";
import { getFuelTypes } from "../services/FuelTypeService";
import { useUser } from "../context/UserContext";
import CardList from "./Cards";
const AddCard = () => {
  const [cardNumber, setCardNumber] = useState("");
  const [fuelType, setFuelType] = useState(""); 
  const [expirationDate, setExpirationDate] = useState("");
  const [additionalInfo, setAdditionalInfo] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const [fuelTypes, setFuelTypes] = useState([]);



  const [showUsers, setShowUsers] = useState(true);
  const [refreshCards, setrefreshCards] = useState(0);


  const {cliId} = useUser();

  useEffect(() => {
    const fetchFuelTypes = async () => {
      try {
        const data = await getFuelTypes();
        setFuelTypes(data);
      } catch (err) {
        if (err.response && err.response.data && err.response.data.message) {
          setError(err.response.data.message);
        } else {
          setError("Klaida");
        }
      }
    };
    fetchFuelTypes();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSuccess(null);

    if (!cardNumber.trim() || !fuelType || !expirationDate.trim()) {
      setError("Kortelės numeris, degalų tipas ir galiojimo data yra privalomi.");
      return;
    }
  
    setLoading(true);
    setError(null);
    const parsedFuelType = parseInt(fuelType, 10);
    const formData = {
      cardNumber,
      fuelType: parsedFuelType,
      expirationDate,
      additionalInfo,
      CLI_ID: cliId
    };
  
    try {
      const result = await addCard(formData);
      setCardNumber("");
      setFuelType("");
      setExpirationDate("");
      setAdditionalInfo("");
      setSuccess("Kortelė sėkmingai pridėta!");
      setrefreshCards((prev) => prev + 1);
    } catch (err) {
      if (err.response && err.response.data && err.response.data.message) {
        setError(err.response.data.message);
      } else {
        setError("Klaida");
      }
    } finally {
      setLoading(false);
    }
  };
  

  return (
    <div className="flex flex-col items-center justify-center px-4 py-8 min-h-screen">
      {/* Add User Form Container */}
      <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl p-4 sm:p-6 border border-gray-300 rounded-lg bg-white shadow-md mb-8">
      <h2 className="text-xl font-bold mb-4">Pridėti kortelę</h2>
      <form onSubmit={handleSubmit}>
        {/* Card Number */}
        <div className="mb-4">
            <label htmlFor="cardNumber" className="block mb-1 font-medium">
              Kortelės numeris
            </label>
            <input
              id="cardNumber"
              type="text"
              value={cardNumber}
              onChange={(e) => setCardNumber(e.target.value)}
              placeholder="Įveskite kortelės numerį"
              className="w-full border border-gray-300 p-2 rounded"
              maxLength={18}
              pattern="\d{18}"
              title="Kortelės numeris turi būti lygiai 18 skaitmenų"
              required
            />
          </div>

        {/* Fuel Type (dynamically loaded from the backend) */}
        <div className="mb-4">
          <label htmlFor="fuelType" className="block mb-1 font-medium">
            Degalų tipas
          </label>
          <select
                id="fuelType"
                value={fuelType}
                onChange={(e) => setFuelType(e.target.value)}
                className="w-full border border-gray-300 p-2 rounded"
                >
                <option value="">Pasirinkite degalų tipą</option>
                {fuelTypes.map((ft) => (
                    <option key={ft.fuelId} value={ft.fuelId}>
                    {ft.fuelName}
                    </option>
                ))}
                </select>
        </div>
        {/* Expiration Date */}
        <div className="mb-4">
          <label htmlFor="expirationDate" className="block mb-1 font-medium">
            Galiojimo data
          </label>
          <input
            id="expirationDate"
            type="date"
            value={expirationDate}
            onChange={(e) => setExpirationDate(e.target.value)}
            className="w-full border border-gray-300 p-2 rounded"
          />
        </div>
        {/* Additional Info (optional) */}
        <div className="mb-4">
          <label htmlFor="additionalInfo" className="block mb-1 font-medium">
            Papildoma info
          </label>
          <textarea
            id="additionalInfo"
            value={additionalInfo}
            onChange={(e) => setAdditionalInfo(e.target.value)}
            placeholder="Įveskite papildomą informaciją"
            className="w-full border border-gray-300 p-2 rounded"
          ></textarea>
        </div>
        {/* Display error and success messages */}
        {error && <p className="mb-4 text-red-600">{error}</p>}
        {success && <p className="mb-4 text-green-600">{success}</p>}
        <button
          type="submit"
          className="w-full bg-green-300 border border-gray-400 hover:text-black hover:border-gray-500 text-white py-2 rounded hover:bg-green-200 hover:scale-105"
          disabled={loading}
        >
          {loading ? "Pateikiama..." : "Pateikti"}
        </button>
      </form>
    </div>
    {/* cards List Section */}
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
            <CardList  refreshCards={refreshCards} fuelTypes={fuelTypes}  />
          </div>
        )}
      </div>
      </div>
    </div>
  );
};

export default AddCard;

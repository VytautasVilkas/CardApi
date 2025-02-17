import React, { useState, useEffect } from "react";
import { addCar } from "../services/CarService"; 
import { getUsersNotConnected } from "../services/UserService"; 
import { getNotConnectedCards } from "../services/CardService";  
import { useUser } from "../context/UserContext";
import CarList from "./Cars";
const AddCar = () => {
  const [carPlateNumber, setCarPlateNumber] = useState("");
  const [initialOdo, setInitialOdo] = useState("");
  const [selectedUser, setSelectedUser] = useState("");
  const [selectedCard, setSelectedCard] = useState("");
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const [loading, setLoading] = useState(false);
  const [users, setUsers] = useState([]);
  const [cards, setCards] = useState([]);
  const [ShowCars, setShowCars] = useState(true);
  const [refreshCars, setrefreshCars] = useState(0);
  const { cliId } = useUser();

  const fetchUsers = async () => {
    try {
      const data = await getUsersNotConnected(cliId);
      setUsers(data);
    } catch (err) {
      setError(err.response?.data?.message || "Nepavyko užkrauti vartotojų. Bandykite dar kartą.");
    }
  };

  const fetchCards = async () => {
    try {
      const data = await getNotConnectedCards(cliId);
      setCards(data);
    } catch (err) {
      setError(err.response?.data?.message || "Nepavyko užkrauti kortelių. Bandykite dar kartą.");
    }
  };

  useEffect(() => {
    if (cliId) {
      fetchUsers();
      fetchCards();
    }
  }, [cliId]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSuccess(null);
    if (!carPlateNumber.trim() || !initialOdo.trim()) {
      setError("Automobilio numeris ir pradinis rida yra privalomi.");
      return;
    }
    setLoading(true);
    setError(null);
    const formData = {
      carPlateNumber,
      initialOdo,
      userId: selectedUser.trim() ? selectedUser : null,
      cardId: selectedCard.trim() ? parseInt(selectedCard, 10) : null,
      CLI_ID: cliId
    };
    try {
      const result = await addCar(formData);
      setCarPlateNumber("");
      setInitialOdo("");
      setSelectedUser("");
      setSelectedCard("");
      setSuccess("Automobilis sėkmingai pridėtas!");
      setrefreshCars((prev) => prev + 1);
    } catch (err) {
      setError(err.response?.data?.message || "Nepavyko pridėti automobilio. Bandykite dar kartą.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col items-center justify-center px-4 py-8 min-h-screen">
      <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl p-4 sm:p-6 border border-gray-300 rounded-lg bg-white shadow-md mb-8">
      <h2 className="text-xl font-bold mb-4">Pridėti Automobilį</h2>
      <form onSubmit={handleSubmit}>
        {/* Car Plate Number */}
        <div className="mb-4">
          <label htmlFor="carPlateNumber" className="block mb-1 font-medium">
            Automobilio numeris
          </label>
          <input
            id="carPlateNumber"
            type="text"
            value={carPlateNumber}
            onChange={(e) => setCarPlateNumber(e.target.value)}
            placeholder="Įveskite automobilio numerį"
            className="w-full border border-gray-300 p-2 rounded"
          />
        </div>

        {/* Initial Odometer */}
        <div className="mb-4">
          <label htmlFor="initialOdo" className="block mb-1 font-medium">
            Pradinė rida
          </label>
          <input
            id="initialOdo"
            type="number"
            value={initialOdo}
            onChange={(e) => setInitialOdo(e.target.value)}
            placeholder="Įveskite pradinę ridą"
            className="w-full border border-gray-300 p-2 rounded"
          />
        </div>

        {/* Optional User Dropdown */}
        <div className="mb-4">
          <label htmlFor="selectedUser" className="block mb-1 font-medium">
            Pasirinkite vartotoją (nebūtina)
          </label>
          <select
            id="selectedUser"
            value={selectedUser}
            onChange={(e) => setSelectedUser(e.target.value)}
            className="w-full border border-gray-300 p-2 rounded"
          >
            <option value="">Nepriskirti</option>
            {users.map((user) => (
              <option key={user.userid} value={user.userid}>
                {user.username}
              </option>
            ))}
          </select>
        </div>

        {/* Optional Card Dropdown */}
        <div className="mb-4">
          <label htmlFor="selectedCard" className="block mb-1 font-medium">
            Pasirinkite kortelę (nebūtina)
          </label>
          <select
            id="selectedCard"
            value={selectedCard}
            onChange={(e) => setSelectedCard(e.target.value)}
            className="w-full border border-gray-300 p-2 rounded"
          >
            <option value="">Nepriskirti</option>
            {cards.map((card) => (
              <option key={card.id} value={card.id}>
                {card.number}
              </option>
            ))}
          </select>
        </div>

        {/* Display error and success messages */}
        {error && <p className="mb-4 text-red-600">{error}</p>}
        {success && <p className="mb-4 text-green-600">{success}</p>}

        <button
          type="submit"
          disabled={loading}
          className="w-full bg-green-300 border border-gray-400 hover:text-black hover:border-gray-500 text-white py-2 rounded hover:bg-green-200 hover:scale-105"
        >
          {loading ? "Pateikiama..." : "Pateikti"}
        </button>
      </form>
      </div>
      {/* cars List Section */}
      <div className="w-full max-w-7xl mx-auto mb-8 space-y-8">
        <div>
          <div className="flex items-center justify-between bg-gradient-to-r from-blue-300 to-blue-300 text-white px-8 py-2 rounded-t shadow-lg gap-x-4">
            <h3 className="text-xl font-semibold">Mašinos</h3>
          <button
            onClick={() => setShowCars((prev) => !prev)}
            className="bg-white text-purple-600 px-4 py-2 rounded shadow hover:bg-purple-100 transition-colors duration-200 text-xs"
          >
            {ShowCars ? "Slėpti" : "Rodyti"}
          </button>
        </div>
        {ShowCars && (
          <div className="bg-white p-8 rounded-b shadow-xl">
            <CarList refreshCars={refreshCars}/>
          </div>
        )}
      </div>
      </div>
    </div>
  );
};

export default AddCar;

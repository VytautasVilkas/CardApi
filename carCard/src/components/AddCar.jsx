import React, { useState, useEffect } from "react";
import { addCar } from "../services/CarService"; // Service to add a new car
import { getUsers } from "../services/UserService"; // Service to fetch users
import { getNotConnectedCards } from "../services/CardService";  // Service to fetch cards
import { useUser } from "../context/UserContext";

const AddCar = () => {
  const [carPlateNumber, setCarPlateNumber] = useState("");
  const [initialOdo, setInitialOdo] = useState("");
  // Optional dropdowns: if nothing is selected, they remain empty strings
  const [selectedUser, setSelectedUser] = useState("");
  const [selectedCard, setSelectedCard] = useState("");
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);
  const [loading, setLoading] = useState(false);
  const [users, setUsers] = useState([]);
  const [cards, setCards] = useState([]);
  const { cliId } = useUser();

  const fetchUsers = async () => {
    try {
      const data = await getUsers(cliId);
      setUsers(data);
    } catch (err) {
      setError(err.response?.data?.message || "Nepavyko užkrauti vartotojų. Bandykite dar kartą.");
    }
  };

  const fetchCards = async () => {
    try {
      console.log("Fetched cards: " + cliId);
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
    // Only require carPlateNumber and initialOdo.
    if (!carPlateNumber.trim() || !initialOdo.trim()) {
      setError("Automobilio numeris ir pradinis rida yra privalomi.");
      return;
    }
    setLoading(true);
    setError(null);
    const formData = {
      carPlateNumber,
      initialOdo,
      // If the dropdown is empty string, pass null
      userId: selectedUser.trim() ? selectedUser : null,
      cardId: selectedCard.trim() ? selectedCard : null,
      CLI_ID: cliId
    };
    try {
      const result = await addCar(formData);
      console.log("Car added successfully:", result);
      setCarPlateNumber("");
      setInitialOdo("");
      setSelectedUser("");
      setSelectedCard("");
      setSuccess("Automobilis sėkmingai pridėtas!");
    } catch (err) {
      setError(err.response?.data?.message || "Nepavyko pridėti automobilio. Bandykite dar kartą.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-md mx-auto bg-white p-6 rounded shadow">
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
  );
};

export default AddCar;

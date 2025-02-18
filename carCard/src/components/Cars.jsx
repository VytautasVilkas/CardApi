import React, { useState, useEffect } from "react";
import { getCarsAll, updateCar, deleteCar } from "../services/CarService";
import { useUser } from "../context/UserContext";
import { getUsers } from "../services/UserService"; 
import { getCards } from "../services/CardService";
const Cars = ({refreshCars}) => {
  const [cars, setCars] = useState([]);
  const [error, setError] = useState("");
  const [users, setUsers] = useState([]);
  const [cards, setCards] = useState([]);
  const { cliId } = useUser();
   // Search criteria state variables:
    const [search, setSearchQuery] = useState(""); 
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
          const response = await getCards(cliId);
          if (response) {
            console.log(response);
            setCards(response);
            setError("");
          }
        } catch (err) {
          setError(err.response?.data?.message || err.message);
        }
    };
  const fetchCars = async () => {
    try {
      const response = await getCarsAll(cliId,"");
      if (response) {
        console.log("Fetched cars:", response);
        setCars(response);
        setError("");
      }
    } catch (err) {
      setError(err.response?.data?.message || err.message);
    }
  };


  
  useEffect(() => {
    fetchCars();
    fetchUsers();
    fetchCards();
  }, [refreshCars, cliId]);

  const handleSearch = async () => {
      try {
        const response = await getCarsAll(cliId,search);
        if (response) {
          setCars(response);
          setError("");
        }
      } catch (err) {
        setError(err.response?.data?.message || err.message);
      }
  };

  const handleFieldChange = (carId, fieldName, value) => {
    setCars((prevCars) =>
      prevCars.map((car) =>
        car.CAR_ID === carId ? { ...car, [fieldName]: value } : car
      )
    );
  };
  const handleUpdate = async (car) => {
    try {
      const payload = {
        CAR_ID: car.CAR_ID,
        CAR_PLATE_NUMBER: car.CAR_PLATE_NUMBER,
        CAR_USER: typeof car.CAR_USER === 'string' ? car.CAR_USER.trim() || null : null,
        CAR_FCA_ID: (typeof car.CAR_FCA_ID === "number" && car.CAR_FCA_ID !== 0) ? car.CAR_FCA_ID : null,
      };
      console.log(payload);
      const response = await updateCar(payload);
      alert(response.message || "Automobilis atnaujintas sėkmingai!");
    } catch (err) {
      alert("Klaida atnaujinant automobilį: " + (err.response?.data?.message || err.message));
    }
  };
  const handleDelete = async (carId) => {
    if (!window.confirm("Ar tikrai norite ištrinti šį automobilį?")) return;
    try {
      const payload = { CAR_ID: carId };
      const response = await deleteCar(payload);
      alert(response.message || "Automobilis ištrintas sėkmingai!");
      setCars((prevCars) => prevCars.filter((car) => car.CAR_ID !== carId));
    } catch (err) {
      alert("Klaida trinant automobilį: " + (err.response?.data?.message || err.message));
    }
  };
  const formatDate = (dateString) => {
    if (!dateString) return "";
    return dateString.split("T")[0];
  };

  return (
    <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl mx-auto overflow-x-auto">
        {/* Search Strip Outside the Scrollable Container */}
    <div className="mb-2">
      <input
        type="text"
        placeholder="Ieškoti pagal numerį..."
        value={search}
        onChange={(e) => setSearchQuery(e.target.value)}
        className=" border border-gray-300 p-2 rounded"
      />
      <button
        onClick={handleSearch}
        className="ml-4 bg-blue-300 border border-gray-400 hover:text-black hover:border-gray-500 text-white px-3 py-1 rounded hover:bg-blue-200 hover:scale-101 transition-all duration-200"      
      >
        Ieškoti
      </button>
    </div>
      {error && <p className="text-red-600">{error}</p>}
      {cars.length > 0 ? (
        <table className="w-full border-collapse border border-gray-300 text-xs sm:text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="border border-gray-300 p-2 whitespace-nowrap w-15">Mašinos numeris</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Vartotojas</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap min-w-[200px]">Kortelės numeris</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap w-12">Rida</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Pradėta naudoti</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Atnaujinti</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">Panaikinti</th>
            </tr>
          </thead>
          <tbody>
            {cars.map((car, index) => (
              <tr key={car.CAR_ID} className={index % 2 === 0 ? "bg-white" : "bg-gray-50"}>
                {/* Mašinos numeris */}
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <input
                    type="text"
                    value={car.CAR_PLATE_NUMBER}
                    onChange={(e) =>
                      handleFieldChange(car.CAR_ID, "CAR_PLATE_NUMBER", e.target.value)
                    }
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                {/* Vartotojas */}
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <select
                    value={car.CAR_USER || ""}
                    onChange={(e) =>
                      handleFieldChange(car.CAR_ID, "CAR_USER", e.target.value)
                    }
                    className="w-full border border-gray-200 p-1 rounded"
                  >
                    <option value="">Nepriskirtas</option>
                    {users && users.map((user) => (
                    <option key={user.userid} value={user.userid}>
                        {user.username}
                    </option>
                    ))}
                  </select>
                </td>
                {/* Kortelės numeris */}
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                <select
                  value={
                    car.CAR_FCA_ID !== null && car.CAR_FCA_ID !== undefined
                      ? car.CAR_FCA_ID.toString()
                      : ""
                  }
                  onChange={(e) => {
                    const newValue = e.target.value === "" ? null : parseInt(e.target.value, 10);
                    handleFieldChange(car.CAR_ID, "CAR_FCA_ID", newValue);
                  }}
                  className="w-full border border-gray-200 p-1 rounded"
                >
                  <option value="">Nepasirinkta</option>
                  {cards &&
                    cards.map((card) => (
                      <option key={card.FCA_ID} value={card.FCA_ID.toString()}>
                        {card.FCA_NUMBER}
                      </option>
                    ))}
                </select>
              </td>

                {/* Pradinė rida (read-only, using formatted date if applicable) */}
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                <span>{car.CurrentOdo}</span>
                </td>
                {/* Pradėta naudoti */}
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                <span>{car.CAR_USAGE_START_DATE ? formatDate(car.CAR_USAGE_START_DATE) : ""}</span>
                </td>
                {/* Atnaujinti */}
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <button
                    onClick={() => handleUpdate(car)}
                    className="transform transition duration-200 hover:scale-110 p-2 rounded"
                  >
                    <img
                      src="https://img.icons8.com/ios/50/installing-updates--v1.png"
                      alt="Atnaujinti"
                      className="w-6 h-6"
                    />
                  </button>
                </td>
                {/* Panaikinti */}
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <button
                    onClick={() => handleDelete(car.CAR_ID)}
                    className="transform transition duration-200 hover:scale-110 p-2 rounded"
                  >
                    <img
                      src="https://img.icons8.com/ios/100/delete-trash.png"
                      alt="Ištrinti"
                      className="w-6 h-6"
                    />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <p className="text-sm text-gray-600">Nėra automobilių</p>
      )}
    </div>
  );
};

export default Cars;

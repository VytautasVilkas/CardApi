import React, { useState, useEffect } from "react";
import { getCards, updateCard, deleteCard } from "../services/CardService";
import { useUser } from "../context/UserContext";

const Cards = ({ refreshCards, fuelTypes }) => {
  const [cards, setCards] = useState([]);
  const [error, setError] = useState("");
  const { cliId } = useUser();
  useEffect(() => {
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
    fetchCards();
  }, [refreshCards, cliId]);
  const handleFieldChange = (cardId, fieldName, value) => {
    setCards((prevCards) =>
      prevCards.map((card) =>
        card.FCA_ID === cardId ? { ...card, [fieldName]: value } : card
      )
    );
  };
  const handleUpdate = async (card) => {
    if (!window.confirm("Ar tikrai norite atnaujinti kortelę?")) return;
    const cardNumberRegex = /^\d{18}$/;
    if (!cardNumberRegex.test(card.FCA_NUMBER)) {
      alert("Kortelės numeris turi būti lygiai 18 skaitmenų");
      return;
    }
    try {
      console.log(card);
      const response = await updateCard(card);
      alert(response.message || "Kortelė atnaujinta sėkmingai!");
    } catch (err) {
      alert(
        "Klaida atnaujinant kortelę: " +
          (err.response?.data?.message || err.message)
      );
    }
  };
  
  const handleDelete = async (cardId) => {
    if (!window.confirm("Ar tikrai norite ištrinti šią kortelę?")) return;
    try {
      const payload = {
          FCA_ID: cardId
      }
      const response = await deleteCard(payload);
      alert(response.message || "Kortelė ištrinta sėkmingai!");
      setCards((prevCards) =>
        prevCards.filter((card) => card.FCA_ID !== cardId)
      );
    } catch (err) {
      alert(
        "Klaida trinant kortelę: " +
          (err.response?.data?.message || err.message)
      );
    }
  };
  const formatDate = (dateString) => {
    if (!dateString) return "";
    return dateString.split("T")[0];
  };
  return (
    <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl mx-auto overflow-x-auto">
      {error && <p className="text-red-600">{error}</p>}
      {cards.length > 0 ? (
        <table className="w-full border-collapse border border-gray-300 text-xs sm:text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="border border-gray-300 p-2 whitespace-nowrap">
                Kortelės numeris
              </th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">
                Papildoma info
              </th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">
                Galiojimo data
              </th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">
                Degalų tipas
              </th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">
                Atnaujinti
              </th>
              <th className="border border-gray-300 p-2 whitespace-nowrap">
                Panaikinti
              </th>
            </tr>
          </thead>
          <tbody>
            {cards.map((card, index) => (
              <tr
                key={card.FCA_ID}
                className={index % 2 === 0 ? "bg-white" : "bg-gray-50"}
              >
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <input
                    type="text"
                    value={card.FCA_NUMBER}
                    onChange={(e) =>
                      handleFieldChange(card.FCA_ID, "FCA_NUMBER", e.target.value)
                    }
                    maxLength={18}
                    pattern="\d{18}"
                    inputMode="numeric"
                    title="Kortelės numeris turi būti lygiai 18 skaitmenų"
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>

                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <input
                    type="text"
                    value={card.FAC_ADDITIONALINFO || ""}
                    onChange={(e) =>
                      handleFieldChange(
                        card.FCA_ID,
                        "FAC_ADDITIONALINFO",
                        e.target.value
                      )
                    }
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <span>{formatDate(card.FCA_VALID_UNTIL)}</span>
                </td>
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <select
                    value={card.FCA_FUEL_TYPE || ""}
                    onChange={(e) =>
                      handleFieldChange(
                        card.FCA_ID,
                        "FCA_FUEL_TYPE",
                        parseInt(e.target.value, 10)
                      )
                    }
                    className="w-full border border-gray-200 p-1 rounded"
                  >
                    <option value="">Pasirinkite degalų tipą</option>
                    {fuelTypes &&
                      fuelTypes.map((ft) => (
                        <option key={ft.fuelId} value={ft.fuelId}>
                          {ft.fuelName}
                        </option>
                      ))}
                  </select>
                </td>
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <button
                    onClick={() => handleUpdate(card)}
                    className="transform transition duration-200 hover:scale-110 p-2 rounded"
                  >
                    <img
                      src="https://img.icons8.com/ios/50/installing-updates--v1.png"
                      alt="Atnaujinti"
                      className="w-6 h-6"
                    />
                  </button>
                  </td>
                  <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <button
                    onClick={() => handleDelete(card.FCA_ID)}
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
        <p className="text-sm text-gray-600">Nėra kortelių</p>
      )}
    </div>
  );
};

export default Cards;

import React, { useState, useEffect } from "react";
import { getUsersAll, updateUser } from "../services/UserService";
import { getCars } from "../services/CarService";
import { useUser } from "../context/UserContext";
const Users = ({ refreshUsers }) => {
  const [users, setUsers] = useState([]);
  const [cars, setCars] = useState([]);
  const [error, setError] = useState("");
  const {cliId} = useUser();
  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const response = await getUsersAll(cliId);
        if (response) {
          setUsers(response);
          setError("");
        }
      } catch (err) {
        setError(err.response?.data?.message || err.message);
      }
    };
    fetchUsers();
  }, [refreshUsers,cliId]);



  useEffect(() => {
    const fetchCars = async () => {
      try {
        const response = await getCars(cliId);
        if (response) {
          setCars(response);
          setError("");
        }
      } catch (err) {
        setError(err.response?.data?.message || err.message);
      }
    };

    if (users.length > 0) {
      fetchCars();
    }
  }, [users]);

  // Update the corresponding field value for a given user.
  const handleFieldChange = (userId, fieldName, value) => {
    setUsers((prevUsers) =>
      prevUsers.map((user) =>
        user.userid === userId ? { ...user, [fieldName]: value } : user
      )
    );
  };

  const handleUpdate = async (user) => {
    try {
      const selectedCar = cars.find(
        (car) => car.CAR_PLATE_NUMBER === user.caR_NUMBER
      );
      const payload = {
        USERID: user.userid,
        USERNAME: user.username,
        ROLE: user.role,
        NAME: user.name,
        SURNAME: user.surname,
        CAR_ID: selectedCar ? selectedCar.CAR_ID : null
      };
      const response = await updateUser(payload);
      handleFieldChange(user.userid, "carD_NUMBER", "");
      if (response.newFcaNumber) {
        handleFieldChange(user.userid, "carD_NUMBER", response.newFcaNumber);
      }

      alert(response.message || "Vartotojas atnaujintas sėkmingai!");
    } catch (err) {
      const errorData = err.response?.data;
      if (errorData) {
        if (errorData.currentCarPlate && errorData.currentFcaNumber) {
          handleFieldChange(user.userid, "caR_NUMBER", errorData.currentCarPlate);
          handleFieldChange(user.userid, "carD_NUMBER", errorData.currentFcaNumber);
        }
        alert("Klaida: " + (errorData.message || err.message));
      } else {
        alert("Klaida: " + err.message);
      }
    }
  };
  return (
    <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl mx-auto overflow-x-auto">
      {error && <p className="text-red-600">{error}</p>}
      {users.length > 0 ? (
        <table className="w-full border-collapse border border-gray-300 text-xs sm:text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="border border-gray-300 p-2 whitespace-nowrap w-16">Vartotojo vardas</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap w-16">Rolė</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap w-30">Vardas</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap w-30">Pavardė</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap w-30">Mašina</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap ">Kortelė</th>
              <th className="border border-gray-300 p-2 whitespace-nowrap ">Atnaujinti</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user, index) => (
              <tr
                key={`${user.userid}-${index}`}
                className={index % 2 === 0 ? "bg-white" : "bg-gray-50"}
              >
                <td className="border border-gray-300 p-2 whitespace-nowrap">
                  <input
                    type="text"
                    value={user.username}
                    onChange={(e) => handleFieldChange(user.userid, "username", e.target.value)}
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                <td className="border border-gray-300 p-2">
                  <input
                    type="text"
                    value={user.role}
                    onChange={(e) => handleFieldChange(user.userid, "role", e.target.value)}
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                <td className="border border-gray-300 p-2">
                  <input
                    type="text"
                    value={user.name}
                    onChange={(e) => handleFieldChange(user.userid, "name", e.target.value)}
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                <td className="border border-gray-300 p-2">
                  <input
                    type="text"
                    value={user.surname}
                    onChange={(e) => handleFieldChange(user.userid, "surname", e.target.value)}
                    className="w-full border border-gray-200 p-1 rounded"
                  />
                </td>
                {/* Car combobox instead of input */}
                <td className="border border-gray-300 p-2">
                  <select
                    value={user.caR_NUMBER || ""}
                    onChange={(e) => handleFieldChange(user.userid, "caR_NUMBER", e.target.value)}
                    className="w-full border border-gray-200 p-1 rounded"
                  >
                    <option value="">Pasirinkite mašiną</option>
                    {cars.map((car) => (
                      <option key={car.CAR_ID} value={car.CAR_PLATE_NUMBER}>
                        {car.CAR_PLATE_NUMBER}
                      </option>
                    ))}
                  </select>
                </td>

                <td className="border border-gray-300 p-2">
                  <span>
                  {user.carD_NUMBER}
                  </span>
                </td>
                <td className="border border-gray-300 p-2">
                  <button
                    onClick={() => handleUpdate(user,cars)}
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
        <p className="text-sm text-gray-600">Nėra vartotojų</p>
      )}
    </div>
  );
};

export default Users;

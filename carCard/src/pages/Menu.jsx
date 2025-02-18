import React, { useState, useEffect } from "react";
import { Bars3Icon, XMarkIcon } from "@heroicons/react/24/outline";
import UploadExcel from "../components/UploadExcel";
import AddCard from "../components/AddCard";
import AddCar from "../components/AddCar";
import StartTrip from "../components/StartTrip";
import AddUsers from "../components/AddUsers";
import AddCompany from "../components/AddCompany";
import { useUser } from "../context/UserContext";
import { getCliOptions } from "../services/UserService"; 

const Menu = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [activeComponent, setActiveComponent] = useState("");
  const [cliOptions, setCliOptions] = useState([]);
  const {role, Name, Surname, logout, cliId, setCliId, cliName, setCliName } = useUser();

  const handleLogout = async () => {
    try {
      await logout();
    } catch (error) {
      console.error("Logout failed:", error);
    }
  };

  useEffect(() => {
    if (role === "0") {
      setActiveComponent("three");
    } else if (role === "2") {
      setActiveComponent("six");
    } else if (role === "1") {
      setActiveComponent("three");
    }
  }, [role]);

  useEffect(() => {
    const fetchCliOptions = async () => {
      
        try {
          const response = await getCliOptions();
          if (response && response.length > 0) {
            setCliOptions(response);
            if (cliId == "Admin") {
              setCliId(response[0].CLI_ID);
              setCliName(response[0].CLI_NAME);
            }
          }
        } catch (err) {
          console.error("Error fetching CLI options:", err);
        }
    };
    fetchCliOptions();
  }, [role, cliId, setCliId]);

  useEffect(() => {
    if (cliId && cliOptions.length > 0) {
      const selectedOption = cliOptions.find(
        (option) => option.CLI_ID === cliId
      );
      if (selectedOption) {
        setCliName(selectedOption.CLI_NAME);
      }
    }
  }, [cliId, cliOptions, setCliName]);

  const renderComponent = () => {
    switch (activeComponent) {
      case "three":
        return <UploadExcel />;
      case "four":
        return <AddCard />;
      case "five":
        return <AddCar />;
      case "six":
        return <StartTrip />;
      case "seven":
        return <AddUsers />;
      case "one":
        return <AddCompany />;
      default:
        return <div>Sveiki prisijungę</div>;
    }
  };

  return (
    <div className="flex min-h-screen">

      {/* Sidebar */}
      <aside
        className={`fixed inset-y-0 left-0 z-20 transform ${
          isSidebarOpen ? "translate-x-0" : "-translate-x-full"
        } w-64 border-r border-gray-300 bg-white transition-transform duration-300 ease-in-out`}
      >
        <div className="p-6 border-b border-gray-300 flex items-center justify-between">
          <h2 className="text-xl font-bold flex items-center">
            <img
              src="https://img.icons8.com/ios/100/user--v1.png"
              alt="user--v1"
              className="w-6 h-6 mr-2"
            />
            {Name} {Surname}
          </h2>
          <button
            onClick={() => setIsSidebarOpen(false)}
            className="text-gray-600 hover:text-gray-900 focus:outline-none"
          >
            {/* Optionally, remove inner toggle icon if not needed */}
          </button>
        </div>
        <nav className="flex-grow p-4 space-y-4">
          {/* CLI select for company admin */}
          {(role === "1" || role === "0") && (
            <div className="max-w-4xl mx-auto mb-4">
              <label className="block text-sm font-medium text-gray-900">
                Pasirinkti įmonę
              </label>
              <select
                value={cliId || ""}
                onChange={(e) => {
                  const newCliId = e.target.value;
                  const selectedIndex = e.target.selectedIndex;
                  const newCliName = e.target.options[selectedIndex].text;
                  console.log("Selected CLI_ID:", newCliId, "CLI_NAME:", newCliName);
                  setCliId(newCliId);
                  setCliName(newCliName);
                }}
                className="w-full border border-gray-300 p-2 rounded"
              >
                {cliOptions.map((option) => (
                  <option key={option.CLI_ID} value={option.CLI_ID}>
                    {option.CLI_NAME}
                  </option>
                ))}
              </select>
            </div>
          )}
          {role === "0" && (
            <>
              <button
                onClick={() => setActiveComponent("one")}
                className="hover:scale-105 w-full flex items-center justify-center rounded-md border border-gray-300 py-2 px-4 text-sm font-semibold text-gray-900 hover:bg-gray-100 focus:outline-none"
              >
                <img
                  src="https://img.icons8.com/glyph-neue/64/group-foreground-selected.png"
                  alt="group-foreground-selected"
                  className="w-6 h-6 mr-2"
                />
                Pridėti Imonę
              </button>
            </>
          )}
          {(role === "1" || role === "0") && (
            <>
              <button
                onClick={() => setActiveComponent("three")}
                className="hover:scale-105 w-full flex items-center justify-center rounded-md border border-gray-300 py-2 px-4 text-sm font-semibold text-gray-900 hover:bg-gray-100 focus:outline-none"
              >
                <img
                  src="https://img.icons8.com/ios/100/ms-excel.png"
                  alt="ms-excel"
                  className="w-6 h-6 mr-2"
                />
                Įkelti Excel duomenis
              </button>
              <button
                onClick={() => setActiveComponent("four")}
                className="hover:scale-105 w-full flex items-center justify-center rounded-md border border-gray-300 py-2 px-4 text-sm font-semibold text-gray-900 hover:bg-gray-100 focus:outline-none"
              >
                <img
                  src="https://img.icons8.com/ios/100/bank-card-back-side--v1.png"
                  alt="bank-card-back-side--v1"
                  className="w-6 h-6 mr-2"
                />
                Pridėti Kortelę
              </button>
              <button
                onClick={() => setActiveComponent("five")}
                className="hover:scale-105 w-full flex items-center justify-center rounded-md border border-gray-300 py-2 px-4 text-sm font-semibold text-gray-900 hover:bg-gray-100 focus:outline-none"
              >
                <img
                  src="https://img.icons8.com/ios/100/car--v1.png"
                  alt="car--v1"
                  className="w-6 h-6 mr-2"
                />
                Pridėti Automobilį
              </button>
              <button
                onClick={() => setActiveComponent("seven")}
                className="hover:scale-105 w-full flex items-center justify-center rounded-md border border-gray-300 py-2 px-4 text-sm font-semibold text-gray-900 hover:bg-gray-100 focus:outline-none"
              >
                <img
                  src="https://img.icons8.com/ios/100/add-user-male.png"
                  alt="add-user-male"
                  className="w-6 h-6 mr-2"
                />
                Pridėti Vartotoją
              </button>
            </>
          )}
          {(role === "2" || role === "1") && (
            <button
              onClick={() => setActiveComponent("six")}
              className="hover:scale-105 w-full flex items-center justify-center rounded-md border border-gray-300 py-2 px-4 text-sm font-semibold text-gray-900 hover:bg-gray-100 focus:outline-none"
            >
              <img
                src="https://img.icons8.com/ios/100/odometer.png"
                alt="odometer"
                className="w-6 h-6 mr-2"
              />
              Odometro duomenys
            </button>
          )}
          <button
            onClick={handleLogout}
            className="hover:scale-105 w-full flex items-center justify-center rounded-md border border-red-300 py-2 px-4 text-sm font-semibold text-red-900 hover:bg-red-100 focus:outline-none"
          >
            <img
              src="https://img.icons8.com/ios/100/open-pane.png"
              alt="open-pane"
              className="w-6 h-6 mr-2"
            />
            Atsijungti
          </button>
        </nav>
      </aside>

      {/* Main Content */}
      <main
        className={`flex-grow p-6 transition-all duration-300 ${
          isSidebarOpen ? "ml-64" : "ml-0"
        }`}
      >
        {/* Fixed toggle button */}
        <button
          onClick={() => setIsSidebarOpen(!isSidebarOpen)}
          style={{
            left: isSidebarOpen ? "calc(16rem + 1rem)" : "1rem",
            transition: "left 0.2s ease-in-out",
          }}
          className="fixed top-4 rounded-md bg-gray-100 p-2 shadow-md focus:outline-none z-50"
        >
          <div className="relative w-6 h-6">
            <Bars3Icon
              className={`absolute top-0 left-0 transition-opacity duration-200 ${
                isSidebarOpen ? "opacity-0" : "opacity-100"
              }`}
            />
            <XMarkIcon
              className={`absolute top-0 left-0 transition-opacity duration-200 ${
                isSidebarOpen ? "opacity-100" : "opacity-0"
              }`}
            />
          </div>
        </button>

        <div className="max-w-4xl mx-auto">
        <header className="w-full bg-white shadow-md py-4 px-6">
        <h1 className="text-2xl font-bold text-gray-800">
          {cliName ? cliName : "Jūsų įmonės pavadinimas"}
        </h1>
      </header>{renderComponent()}</div>
      </main>
    </div>
  );
};

export default Menu;

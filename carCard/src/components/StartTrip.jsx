import React, { useState, useEffect } from "react";
import { getLastOdo, startTrip, getCarPlate } from "../services/TripService";
import CardsUsage from "./CardsUsage";
import CarsUsage from "./CarsUsage";

const StartTrip = () => {
  const [odoFrom, setOdoFrom] = useState("");
  const [odoTo, setOdoTo] = useState("");
  const [qtyFrom, setQtyFrom] = useState("");
  const [qtyTo, setQtyTo] = useState("");
  const [tripDate, setTripDate] = useState("");
  const [carPlate, setCarPlate] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [refreshUsage, setRefreshUsage] = useState(0);
  const [showCards, setShowCards] = useState(true);
  const [showCars, setShowCars] = useState(true);

  const fetchLastOdo = async () => {
    try {
      const response = await getLastOdo();
      if (response && response.lastOdo) {
        setOdoFrom(response.lastOdo);
        setError("");
      } else if (response && response.message) {
        setError(response.message);
      } else {
        setError("Paskutinė rida nerasta.");
      }
    } catch (err) {
      setError(err.response?.data?.message || err.message);
    }
  };

  const fetchCarPlate = async () => {
    try {
      const response = await getCarPlate();
      if (response && response.carPlate) {
        setCarPlate(response.carPlate);
        setError("");
      } else if (response && response.message) {
        setError(response.message);
      } else {
        setError("Automobilio numeris nerastas.");
      }
    } catch (err) {
      setError(err.response?.data?.message || err.message);
    }
  };

  // Fetch initial data on mount.
  useEffect(() => {
    fetchLastOdo();
    fetchCarPlate();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");
    setLoading(true);
    const lastOdoNumber = Number(odoFrom);
    const currentOdoNumber = Number(odoTo);
    // if ( currentOdoNumber <= lastOdoNumber) {
    //   setError("Blogai įvestas odometro duomenys: Rida 'iki' turi būti didesnė už 'nuo'.");
    //   setLoading(false);
    //   return;
    // }

    const tripData = {
      OdoTo: odoTo === "" ? 0 : Number(odoTo),
      QtyFrom: qtyFrom === "" ? 0 : Number(qtyFrom),
      QtyTo: qtyTo === "" ? 0 : Number(qtyTo),
    };
    console.log(tripData);
    try {
      const response = await startTrip(tripData);
      setSuccess(response.message);
      setOdoTo("");
      setQtyFrom("");
      setQtyTo("");
      fetchLastOdo();
      setRefreshUsage((prev) => prev + 1);
    } catch (err) {
      setError(err.response?.data?.message || err.message);
    }
    setLoading(false);
  };

  return (
    // Outer container for responsive design
      <div className="flex flex-col items-center justify-center px-4 py-8 min-h-screen">
    {/* Form Container */}
      <div className="max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl p-4 sm:p-6 border border-gray-300 rounded-lg bg-white shadow-md mb-8">
        <h2 className="text-xl font-bold mb-4 text-center">Pridėti Ridą</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Car Plate (read-only) */}
          <div>
            <label className="block text-sm font-medium text-gray-900">Automobilio numeris</label>
            <input
              type="text"
              value={carPlate}
              readOnly
              className="w-full border border-black p-2 rounded bg-green-100"
            />
          </div>
          {/* Odo From (read-only) */}
          <div>
            <label className="block text-sm font-medium text-gray-900">Rida nuo (automatiškai)</label>
            <input
              type="number"
              value={odoFrom}
              readOnly
              className="w-full border border-gray-300 p-2 rounded bg-gray-100"
            />
          </div>
          {/* Odo To */}
          <div>
            <label className="block text-sm font-medium text-gray-900">Rida iki</label>
            <input
              type="number"
              value={odoTo}
              onChange={(e) => setOdoTo(e.target.value)}
              // required
              className="w-full border border-gray-300 p-2 rounded"
            />
          </div>
          {/* Qty From */}
          <div>
            <label className="block text-sm font-medium text-gray-900">Kiekis nuo</label>
            <input
              type="number"
              value={qtyFrom}
              onChange={(e) => setQtyFrom(e.target.value)}
              // required
              className="w-full border border-gray-300 p-2 rounded"
            />
          </div>
          {/* Qty To */}
          <div>
            <label className="block text-sm font-medium text-gray-900">Kiekis iki</label>
            <input
              type="number"
              value={qtyTo}
              onChange={(e) => setQtyTo(e.target.value)}
              // required
              className="w-full border border-gray-300 p-2 rounded"
            />
          </div>
          {error && <p className="text-red-600">{error}</p>}
          {success && <p className="text-green-600">{success}</p>}
          <button
            type="submit"
            disabled={loading}
            className="w-full bg-green-300 text-white py-2 rounded hover:bg-green-200 hover:scale-105 transition-transform duration-200"
          >
            {loading ? "Pateikiama..." : "Pradėti kelionę"}
          </button>
        </form>
      </div>
      <div className="w-full max-w-7xl mx-auto mb-8 space-y-8">
        <div>
          <div className="flex items-center justify-between bg-gradient-to-r from-blue-300 to-blue-300 text-white px-8 py-2 rounded-t shadow-lg gap-x-4">
            <h3 className="text-xl font-semibold">Kortelių duomenys</h3>
            <button
              onClick={() => setShowCards((prev) => !prev)}
              className="bg-white text-blue-600 px-4 py-2 rounded shadow hover:bg-blue-100 transition-colors duration-200 text-xs"
            >
              {showCards ? "Slėpti" : "Rodyti"}
            </button>
          </div>
          {showCards && (
            <div className="bg-white p-8 rounded-b shadow-xl">
              <CardsUsage refreshUsage={refreshUsage} />
            </div>
          )}
        </div>
        <div>
          <div className="flex items-center justify-between bg-gradient-to-r from-green-300 to-green-300 text-white px-8 py-2 rounded-t shadow-lg gap-x-4">
            <h3 className="text-xl font-semibold">Odometro duomenys</h3>
            <button
              onClick={() => setShowCars((prev) => !prev)}
              className="bg-white text-green-600 px-4 py-2 rounded shadow hover:bg-green-100 transition-colors duration-200 text-xs"
            >
              {showCars ? "Slėpti" : "Rodyti"}
            </button>
          </div>
          {showCars && (
            <div className="bg-white p-8 rounded-b shadow-xl">
              <CarsUsage refreshUsage={refreshUsage} />
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
export default StartTrip;

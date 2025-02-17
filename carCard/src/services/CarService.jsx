import apiClient from "../utils/ApiClient";

/**
 * 
 * @param {Object} carData 
 * @returns {Promise<Object>} 
 */
export const addCar = async (carData) => {
  try {
    const response = await apiClient.post("/Car/addcar", carData);
    return response.data;
  } catch (error) {
    console.error("Error adding car:", error);
    throw error;
  }
};
export const getCars = async (cliId,search = "") => {
  try {
    const response = await apiClient.get("/Car/getCars",{
      params: { CLI_ID: cliId,  search: search }
    });
    return response.data;
  } catch (error) {
    console.error("Error adding car:", error);
    throw error;
  }
};
export const getCarsAll = async (cliId,carId = "") => {
  try {
    const response = await apiClient.get("/Car/getCarsAll",{
      params: { CLI_ID: cliId, search: carId}
    });
    return response.data;
  } catch (error) {
    console.error("Error adding car:", error);
    throw error;
  }
};
export const updateCar = async (car) => {
  try {
    const response = await apiClient.post("/Car/updateCar",car);
    return response.data;
  } catch (error) {
    console.error("Error adding car:", error);
    throw error;
  }
};
export const deleteCar = async (car) => {
  try {
    const response = await apiClient.post("/Car/deleteCar",car);
    return response.data;
  } catch (error) {
    console.error("Error adding car:", error);
    throw error;
  }
};
const CarService = {
  addCar,
  getCars,
  getCarsAll,
  updateCar,
  deleteCar
};

export default CarService;
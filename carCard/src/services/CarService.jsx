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
export const getCars = async (cliId) => {
  try {
    const response = await apiClient.get("/Car/getCars",{
      params: { CLI_ID: cliId }
    });
    return response.data;
  } catch (error) {
    console.error("Error adding car:", error);
    throw error;
  }
};
const CarService = {
  addCar,
  getCars
};

export default CarService;
import apiClient from "../utils/ApiClient";



export const getLastOdo = async () => {
  try {
    const response = await apiClient.get("/StartTrip/lastOdo");
    return response.data; 
  } catch (error) {
    console.error("Error fetching last odo:", error);
    throw error;
  }
};

export const startTrip = async (tripData) => {
  try {
    const response = await apiClient.post("/StartTrip/addTrip", tripData);
    return response.data;
  } catch (error) {
    console.error("Error starting trip:", error);
    throw error;
  }
};

export const getCarPlate = async () => {
  try {
    const response = await apiClient.get("/StartTrip/getCarPlates");
    return response.data;
  } catch (error) {
    console.error("Error starting trip:", error);
    throw error;
  }
};
export const getCardsUsage = async (search = "", startDate = "", endDate = "") => {
  try {
    const response = await apiClient.get("/StartTrip/getCardsUsage", {
      params: { search, startDate, endDate },
    });
    return response.data;
  } catch (error) {
    console.error("Error fetching cards usage:", error);
    throw error;
  }
};
export const getCarsUsage  = async () => {
  try {
    const response = await apiClient.get("/StartTrip/getCarsUsage ");
    return response.data;
  } catch (error) {
    console.error("Error starting trip:", error);
    throw error;
  }
};
export const updateCarsUsage = async (updatedRecord) => {
  try {
    const response = await apiClient.post("/StartTrip/updateCarsUsage", updatedRecord);
    return response.data;
  } catch (error) {
    console.error("Error updating cars usage:", error);
    throw error;
  }
};
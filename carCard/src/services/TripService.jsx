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
export const getLastOdoAdmin = async (userId) => {
  try {
    const response = await apiClient.get("/StartTrip/lastOdoAdmin", {
      params: { userId: userId },
    });
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

export const startTripAdmin = async (tripData) => {
  try {
    const response = await apiClient.post("/StartTrip/addTripAdmin", tripData);
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
export const getCarPlateAdmin = async (userId) => {
  try {
    const response = await apiClient.get("/StartTrip/getCarPlatesAdmin", {
      params: { userId:userId },
    });
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
export const getCardsUsageAdmin = async (search = "", startDate = "", endDate = "", selectedUserId) => {
  try {
    const response = await apiClient.get("/StartTrip/getCardsUsageAdmin", {
      params: { search, startDate, endDate, selectedUserId},
    });
    return response.data;
  } catch (error) {
    console.error("Error fetching cards usage:", error);
    throw error;
  }
};
export const getCarsUsage  = async (searchFrom = "",searchTo = "") => {
  try {
    const response = await apiClient.get("/StartTrip/getCarsUsage", {
      params: { searchFrom, searchTo},
    });
    return response.data;
  } catch (error) {
    console.error("Error starting trip:", error);
    throw error;
  }
};
export const getCarsUsageAdmin = async (searchFrom = "", searchTo = "", selectedUserId = "") => {
  try {
    const response = await apiClient.get("/StartTrip/getCarsUsageAdmin", {
      params: { searchFrom, searchTo, selectedUserId },
    });
    return response.data;
  } catch (error) {
    console.error("Error fetching cars usage (admin):", error);
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
export const updateCarsUsageAdmin = async (updatedRecord, selectedUserId = "") => {
  try {
    const response = await apiClient.post("/StartTrip/updateCarsUsageAdmin", updatedRecord, {
      params: { selectedUserId },
    })
    return response.data;
  } catch (error) {
    console.error("Error updating cars usage:", error);
    throw error;
  }
};

import apiClient from "../utils/ApiClient";
/**
 * Retrieves the list of fuel types.
 * @returns {Promise<Object[]>} An array of fuel type objects.
 */
export const getFuelTypes = async () => {
  try {
    const response = await apiClient.get("/FuelType");
    return response.data;
  } catch (error) {
    console.error("Error fetching fuel types:", error);
    throw error;
  }
};

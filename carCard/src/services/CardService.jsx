import apiClient from "../utils/ApiClient";
/**
 * Sends a POST request to add a new card.
 * @param {Object} cardData - The card data to be added.
 * @returns {Promise<Object>} The response data from the server.
 */
export const addCard = async (cardData) => {
  try {
    const response = await apiClient.post(`/Card/addcard`, cardData);
    return response.data;
  } catch (error) {
    console.error("Error adding card:", error);
    throw error;
  }
};

/**
 * Retrieves the list of cards.
 * @returns {Promise<Object[]>} An array of card objects.
 */
export const getNotConnectedCards = async (cliId) => {
  try {
    const response = await apiClient.get(`/Card/getNotConnectedCards`, {
      params: { CLI_ID: cliId },
    });
    return response.data;
  } catch (error) {
    console.error("Error fetching cards:", error);
    throw error;
  }
};

export const getCards = async (cliId) => {
  try {
    const response = await apiClient.get(`/Card/getCards`, {
      params: { CLI_ID: cliId },
    });
    return response.data;
  } catch (error) {
    console.error("Error fetching cards:", error);
    throw error;
  }
};
export const updateCard = async (data) => {
  try {
    const response = await apiClient.post(`/Card/updateCard`,data);
    return response.data;
  } catch (error) {
    console.error("Error Updating card:", error);
    throw error;
  }
};
export const deleteCard = async (cliId) => {
  try {
    const response = await apiClient.post(`/Card/deleteCard`, cliId
    );
    return response.data;
  } catch (error) {
    console.error("Error deleting card:", error);
    throw error;
  }
};
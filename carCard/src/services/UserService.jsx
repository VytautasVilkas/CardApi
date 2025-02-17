
import apiClient from "../utils/ApiClient";
/**
 * Retrieves the list of users.
 * @returns {Promise<Object[]>} An array of user objects.
 */
export const getUsers = async (cliId) => {
  try {
    const response = await apiClient.get("/User/getUsers",{
      params: { CLI_ID: cliId }
    });
    return response.data;
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};
export const getUsersNotConnected = async (cliId) => {
  try {
    const response = await apiClient.get("/User/getUsersNotConnected",{
      params: { CLI_ID: cliId }
    });
    return response.data;
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};
export const deleteUser = async (USERID) => {
  try {
    const response = await apiClient.post("/User/DeleteUser",USERID
      );
    return response.data;
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};

export const getUsersAll = async (cliId, search = "") => {
  try {
    const response = await apiClient.get("/User/getUsersAll", {
      params: { CLI_ID: cliId, search: search }
    });
    return response.data;
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};
export const updateUser = async (user) => {
  try {
    const response = await apiClient.post("/User/UpdateUser",user);
    return response.data;
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};
export const UpdateUserCarToNull = async (user) => {
  try {
    const response = await apiClient.post("/User/UpdateUserCarToNull",user);
    return response.data;
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};
export const AddCliCompany = async (client) => {
  try {
    const response = await apiClient.post("/User/addCompany",client);
    return response.data;
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};
export const getCliOptions = async () => {
  try {
    const response = await apiClient.get("/User/getCli");
    return response.data;
  } catch (error) {
    console.error("Error fetching users:", error);
    throw error;
  }
};

/**
 * Logs in a user.
 * @param {Object} credentials - The login credentials.
 */
export const login = async (credentials) => {
  try {
    const response = await apiClient.post("/User/login", credentials);
    return response.data; // Return the response data so that loginResponse isn't undefined.
  } catch (error) {
    throw new Error(
      error.response?.data?.message || "Įvyko klaida prisijungiant."
    );
  }
};

/**
 * Verifies the current token.
 * @returns {Promise<Object>} The token verification result.
 */
export const verifyToken = async () => {
  try {
    const response = await apiClient.get("/User/verifyToken");
    return response.data;
  } catch (error) {
    throw new Error(
      error.response?.data?.message || "Įvyko klaida prisijungiant."
    );
  }
};
export const logout = async () => {
  try {
    await apiClient.post("/User/logout");
  } catch (error) {
    throw new Error(
      error.response?.data?.message || "Nepavyko atsijungti."
    );
  }
};
/**
 * Adds a new user.
 */
export const addUser = async (newUser) => {
  try {
    const response = await apiClient.post("/User/addUser", newUser);
    return response.data;
  } catch (error) {
    console.error("Error adding user:", error);
    throw error;
  }
};

const UserService = {
  getUsers,
  login,
  verifyToken,
  logout,
  addUser,
  updateUser
};

export default UserService;

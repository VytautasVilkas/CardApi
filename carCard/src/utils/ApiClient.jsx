import axios from "axios";
import { doLogout } from "../context/authManager"; 
const API_URL = "https://localhost:7279/api";
const apiClient = axios.create({
  baseURL: API_URL,
  withCredentials: true,
});
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (
      error.response &&
      error.response.status === 401 &&
      !originalRequest._retry &&
      !originalRequest.url.includes("/login") &&
      !originalRequest.url.includes("/refreshToken")
    ) {
      originalRequest._retry = true;
      try {
        await axios.post(`${API_URL}/User/refreshToken`, {}, { withCredentials: true });
        return apiClient(originalRequest);
      } catch (refreshError) {
        doLogout();
        return Promise.reject(refreshError);
      }
    }
    return Promise.reject(error);
  }
);
export default apiClient;

import apiClient from "../utils/ApiClient";

export const uploadExcelFile = async (file) => {
  const formData = new FormData();
  formData.append("excelFile", file);
  try {
    const response = await apiClient.post(`/File/file-upload`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return response.data; // Axios automatically parses JSON responses
  } catch (error) {
    console.error("Error uploading excel file:", error);
    throw error;
  }
};
export const uploadExcelFileSkipMissing = async (file) => {
  const formData = new FormData();
  formData.append("excelFile", file);
  try {
    const response = await apiClient.post(`/File/file-upload-skip`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return response.data; // Axios automatically parses JSON responses
  } catch (error) {
    console.error("Error uploading excel file:", error);
    throw error;
  }
};
export const uploadExcelFileAll = async (file) => {
  const formData = new FormData();
  formData.append("excelFile", file);
  try {
    const response = await apiClient.post(`/File/file-upload-all`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return response.data; // Axios automatically parses JSON responses
  } catch (error) {
    console.error("Error uploading excel file:", error);
    throw error;
  }
};


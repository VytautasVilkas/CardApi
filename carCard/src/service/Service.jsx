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
export const downloadExcelFileAll = async () => {
  try {
    const response = await apiClient.get(`/File/GetExcelFile`, {
      responseType: 'blob'
    });
    const blob = new Blob([response.data], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', 'CarUsage.xlsx'); 
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  } catch (error) {
    console.error("Error downloading excel file:", error);
    throw error;
  }
};


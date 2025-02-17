import React, { useState } from "react";
import readXlsxFile from "read-excel-file";
import { uploadExcelFile, uploadExcelFileSkipMissing,uploadExcelFileAll  } from "../service/Service";


const UploadExcel = () => {
  const [data, setData] = useState([]);
  const [fileName, setFileName] = useState("");
  const [selectedFile, setSelectedFile] = useState(null);
  const [importOption, setImportOption] = useState("abort"); // "abort" is the default option


  const handleFileChange = async (e) => {
    const file = e.target.files[0];
    if (file) {
      setFileName(file.name);
      setSelectedFile(file);
      try {
        const rows = await readXlsxFile(file);
        setData(rows);
      } catch (error) {
        console.error("Error reading Excel file:", error);
      }
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedFile) {
      alert("Pasirinkite dokumentą");
      return;
    }

    try {
      let response;
      // Decide which service to call based on the selected import option.
      if (importOption === "abort") {
        response = await uploadExcelFile(selectedFile);
      } else if (importOption === "skip") {
        response = await uploadExcelFileSkipMissing(selectedFile);
      } else if (importOption === "importAll") {
        // pakeisti i ikelti visus
        // alert("Neveikia");
        // response = await uploadExcelFileAll(selectedFile);
      }
      alert(JSON.stringify(response));
    } catch (error) {
      if (err.response && err.response.data && err.response.data.message) {
        setError(err.response.data.message);
      } else {
        setError(err.message);
      }
    }
  };

  return (
    <div className="flex flex-col items-center justify-center px-4 py-8 min-h-screen">
      <div className="w-full max-w-xs sm:max-w-lg md:max-w-2xl lg:max-w-4xl p-4 sm:p-6 border border-gray-300 rounded-lg bg-white shadow-md">
        <h2 className="text-lg sm:text-2xl font-bold text-gray-900 mb-4 sm:mb-6 text-center">
          Pridėti dokumentą
        </h2>
        <form className="space-y-4 sm:space-y-6" onSubmit={handleSubmit}>
          {/* File Input */}
          <div className="flex flex-col sm:flex-row sm:items-center sm:space-x-4">
            <label
              htmlFor="excelFile"
              className="block text-sm sm:text-base font-medium text-gray-700 sm:w-1/3"
            >
              Pasirinkite dokumentą
            </label>
            <input
              id="excelFile"
              type="file"
              accept=".xls, .xlsx"
              onChange={handleFileChange}
              className="block w-full rounded-md border border-gray-300 p-2 sm:p-3 focus:outline-none focus:ring-2 focus:ring-indigo-500 sm:w-2/3"
            />
          </div>
          {/* File Name */}
          {fileName && (
            <p className="text-xs sm:text-sm text-gray-600 text-center">
              Pasirinktas dokumentas: <strong>{fileName}</strong>
            </p>
          )}
          {/* Import Option Radio Buttons */}
          <div className="flex flex-col space-y-2">
            <label className="text-sm font-medium text-gray-700">
              Importo parinktis:
            </label>
            <div>
              <input
                type="radio"
                id="abort"
                name="importOption"
                value="abort"
                checked={importOption === "abort"}
                onChange={(e) => setImportOption(e.target.value)}
              />
              <label htmlFor="abort" className="ml-2">
                Nutraukti, jei trūksta kortelių numerių
              </label>
            </div>
            <div>
              <input
                type="radio"
                id="skip"
                name="importOption"
                value="skip"
                checked={importOption === "skip"}
                onChange={(e) => setImportOption(e.target.value)}
              />
              <label htmlFor="skip" className="ml-2">
                Praleisti trūkstamus
              </label>
            </div>
            {/* <div>
              <input
                type="radio"
                id="importAll"
                name="importOption"
                value="importAll"
                checked={importOption === "importAll"}
                onChange={(e) => setImportOption(e.target.value)}
              />
              <label htmlFor="importAll" className="ml-2">
                Importuoti visus
              </label>
            </div> */}
          </div>
          {/* Submit Button */}
          <div className="flex justify-center">
            <button
              type="submit"
              className="w-full bg-green-300  border border-gray-400 hover:text-black hover:border-gray-500 text-white py-2 rounded hover:bg-green-200 hover:scale-105"
            >
              Įkelti
            </button>
          </div>
        </form>
        {/* Table */}
        {data.length > 0 && (
          <div className="mt-6 sm:mt-8 max-h-80 overflow-y-auto">
            <h3 className="text-base sm:text-lg font-semibold text-gray-900 mb-2 sm:mb-4 text-center">
              Duomenys:
            </h3>
            <table className="w-full border-collapse border border-gray-300 text-xs sm:text-sm">
              <thead className="bg-gray-50">
                <tr>
                  {data[0].map((col, index) => (
                    <th
                      key={index}
                      className="border border-gray-300 p-2 sm:p-3 text-left font-medium text-gray-700"
                    >
                      {col}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {data.slice(1).map((row, rowIndex) => (
                  <tr
                    key={rowIndex}
                    className={rowIndex % 2 === 0 ? "bg-white" : "bg-gray-50"}
                  >
                    {row.map((cell, cellIndex) => (
                      <td
                        key={cellIndex}
                        className="border border-gray-300 p-2 sm:p-3 text-gray-900 truncate"
                      >
                        {cell}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
        {/* cia bus visi rezultatai */}










    </div>
  );
};

export default UploadExcel;

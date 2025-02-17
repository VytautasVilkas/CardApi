// FuelTypeService.cs

using Microsoft.Data.SqlClient;


namespace carCard.Services
{
    // The model representing a fuel type.
    public class FuelType
    {
        public int FuelId { get; set; }      // value to be saved (FUEAL_ID)
        public string FuelName { get; set; } // display member (FUEAL_NAME)
    }

    // Interface for dependency injection and unit testing.
    public interface IFuelTypeService
    {
        Task<List<FuelType>> GetFuelTypesAsync();
    }

    public class FuelTypeService : IFuelTypeService
    {
        private readonly ConnectionProvider _connectionProvider;
            private readonly DataTableService _dataTableService;
                private ExceptionLogger exceptionLogger;
        public FuelTypeService(ConnectionProvider connectionProvider, DataTableService dataTableService)
        {
        _connectionProvider = connectionProvider;
        _dataTableService = dataTableService;
        exceptionLogger = new ExceptionLogger(_connectionProvider);
        }

        public async Task<List<FuelType>> GetFuelTypesAsync()
        {
            try{
            var fuelTypes = new List<FuelType>();

            using (var connection = _connectionProvider.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM FUEL_TYPE";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fuelTypes.Add(new FuelType
                            {
                                FuelId = reader.GetInt32(reader.GetOrdinal("FUEL_ID")),
                                FuelName = reader.GetString(reader.GetOrdinal("FUEL_NAME"))
                            });
                        }
                        return fuelTypes;
                    }
                }
            }
            }catch(Exception ex){
                exceptionLogger.LogException(
                    source: "UploadExcelFile",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return null;
            }

            
        }
    }
}

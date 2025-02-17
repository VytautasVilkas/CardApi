using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Data;
namespace carCard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarController : ControllerBase
    {   
         private readonly DataTableService _dataTableService;
        private readonly IAdminService _adminService;
        private readonly ConnectionProvider _connectionProvider;
        private readonly ExceptionLogger _exceptionLogger;

        public CarController(ConnectionProvider connectionProvider,IAdminService adminService,DataTableService dataTableService)
        {
             _adminService = adminService;
            _connectionProvider = connectionProvider;
            _dataTableService = dataTableService;
            _exceptionLogger = new ExceptionLogger(_connectionProvider);
        }
        // POST: api/car/addcar
        [HttpPost("addcar")]
        [Authorize]
        public IActionResult AddCar([FromBody] AddCarRequest car)
        {
            var currentUserId = User?.FindFirst("USERID")?.Value;
            if (!_adminService.IsAdmin( currentUserId ))
            {
            return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
            }
            try
            {
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    var CheckIfCArEgsist = @"

                        SELECT COUNT(*) 
                        FROM CARS 
                        WHERE CAR_PLATE_NUMBER COLLATE Latin1_General_CI_AI = @CarPlateNumber;
                    ";
                        using (var command = new SqlCommand(CheckIfCArEgsist, connection))
                        {
                            command.Parameters.AddWithValue("@CarPlateNumber", car.carPlateNumber);
                            int count = Convert.ToInt32(command.ExecuteScalar());
                            if (count > 0)
                            {
                                return BadRequest(new { message = "Toks automobilis jau įvestas į sistemą" });
                            }
                        }

                    
                    using (var command = new SqlCommand(CheckIfCArEgsist, connection))
                    {
                        command.Parameters.AddWithValue("@CarPlateNumber", car.carPlateNumber);
                        var Count = command.ExecuteNonQuery();
                        if(Count > 0){
                                return BadRequest(new { message = "Toks automobilis jau ivestas į sistemą" });
                        }
                    }
                    var query = @"
                        INSERT INTO CARS (CAR_PLATE_NUMBER, CAR_INITIAL_ODO, CAR_USER, CAR_FCA_ID,CAR_USAGE_START_DATE,CAR_CLI_ID)
                        VALUES (@CarPlateNumber, @InitialOdo, @UserId, @CardId,@Date,@CAR_CLI_ID)";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CarPlateNumber", car.carPlateNumber);
                        command.Parameters.AddWithValue("@InitialOdo", car.initialOdo);
                        command.Parameters.AddWithValue("@UserId", car.userId);
                        command.Parameters.AddWithValue("@CardId", car.cardId);
                        command.Parameters.AddWithValue("@CAR_CLI_ID", car.CLI_ID);
                        command.Parameters.AddWithValue("@Date", DateTime.UtcNow);
                        command.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "Automobilis sėkmingai pridėtas!" });
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "AddCar",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Nepavyko pridėti automobilio. Bandykite dar kartą." });
            }
        }
        [HttpGet("getCars")]
        [Authorize]
        public IActionResult GetCarsUsage([FromQuery] string CLI_ID)
        {
            var UserID = User?.FindFirst("USERID")?.Value;
            if (!_adminService.IsAdmin(UserID))
            {
                return Unauthorized(new { message = "Neturite administratoriaus teisių." });
            }    
            
            try
            {
            var dataTable = new DataTable();
            using (var connection = _connectionProvider.GetConnection())
            {
                connection.Open();
                var query = "SELECT CAR_ID, CAR_PLATE_NUMBER FROM CARS WHERE CAR_CLI_ID = @CLI_ID";
                using (var command = new SqlCommand(query, connection))
                {
                    if (Guid.TryParse(CLI_ID, out Guid parsedCliId))
                            {
                            command.Parameters.AddWithValue("@CLI_ID", parsedCliId);
                            }
                          
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            var jsonResult = _dataTableService.ConvertToJson(dataTable);
            return Ok(jsonResult);
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "geCars",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko užkrauti Componento" });
            }
        }
       

    public class AddCarRequest
    {
        public string carPlateNumber { get; set; }
        public string initialOdo { get; set; }
        public Guid userId { get; set; }
        public int cardId { get; set; }
        public string CLI_ID { get; set; }

    }
    }
}

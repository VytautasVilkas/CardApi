using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Components.Forms;
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
            var currentUserId = User.FindFirst("USERID")?.Value;
            if (!_adminService.IsAdmin(currentUserId))
            {
                return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
            }
            try
            {
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();

                    // Check if car with this plate already exists.
                    var checkQuery = @"
                        SELECT COUNT(*) 
                        FROM CARS 
                        WHERE CAR_PLATE_NUMBER COLLATE Latin1_General_CI_AI = @CarPlateNumber;
                    ";
                    using (var command = new SqlCommand(checkQuery, connection))
                    {
                        command.Parameters.AddWithValue("@CarPlateNumber", car.carPlateNumber);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        if (count > 0)
                        {
                            return BadRequest(new { message = "Toks automobilis jau įvestas į sistemą" });
                        }
                    }

                    var query = @"
                        INSERT INTO CARS (CAR_PLATE_NUMBER, CAR_INITIAL_ODO, CAR_USER, CAR_FCA_ID, CAR_USAGE_START_DATE, CAR_CLI_ID,CAR_SANDELIS,CAR_TIKSLAS,CAR_TYPE,CAR_PADALINYS)
                        VALUES (@CarPlateNumber, @InitialOdo, @UserId, @CardId, @Date, @CAR_CLI_ID,@CAR_SANDELIS,@CAR_TIKSLAS,@CAR_TYPE,@CAR_PADALINYS)
                    ";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CarPlateNumber", car.carPlateNumber);
                        command.Parameters.AddWithValue("@InitialOdo", car.initialOdo);
                        command.Parameters.AddWithValue("@UserId", 
                        !car.userId.HasValue || car.userId.Value == Guid.Empty ? (object)DBNull.Value : car.userId.Value);
                        command.Parameters.AddWithValue("@CardId", 
                        car.cardId.HasValue ? (object)car.cardId.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@CAR_CLI_ID", car.CLI_ID);
                        command.Parameters.AddWithValue("@Date", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@CAR_SANDELIS", car.sandelis);
                        command.Parameters.AddWithValue("@CAR_TIKSLAS", car.tikslas);
                        command.Parameters.AddWithValue("@CAR_TYPE", car.CAR_TYPE);
                        command.Parameters.AddWithValue("@CAR_PADALINYS", car.CAR_PADALINYS);
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
        [HttpPost("DeleteCar")]
        [Authorize]
        public IActionResult DeleteCar([FromBody] DeleteCarRequest car)
        {
                var currentUserId = User.FindFirst("USERID")?.Value;
            if (!_adminService.IsAdmin(currentUserId))
            {
                return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
            }
            try
            {
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    

                    var query = @"
                        DELETE CARS WHERE CAR_ID = @car
                    ";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@car", car.CAR_ID );
                        command.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "Automobilis sėkmingai Ištrintas" });
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "DeleteCar",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko pridėti automobilio. Bandykite dar kartą." });
            }




        }
        [HttpGet("getCars")]
        [Authorize]
        public IActionResult GetCarsUsage([FromQuery] string CLI_ID, [FromQuery] string search = "")
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
                    if (!string.IsNullOrEmpty(search))
                    {
                        query += " AND CAR_PLATE_NUMBER LIKE '%' + @search + '%'";
                    }
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (Guid.TryParse(CLI_ID, out Guid parsedCliId))
                        {
                            command.Parameters.AddWithValue("@CLI_ID", parsedCliId);
                        }
                        // Add search parameter if applicable.
                        if (!string.IsNullOrEmpty(search))
                        {
                            command.Parameters.AddWithValue("@search", search);
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

        
        [HttpGet("getCarType")]
        [Authorize]
        
        public IActionResult getCarTypes()
        {
            try{
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    var dt = new DataTable();
                    var query = "SELECT TYPE_ID,TYPE_NAME,TYPE_CODE FROM CAR_TYPE";
                    using (var command = new SqlCommand(query,connection))
                    {
                        using (var adapter = new SqlDataAdapter(command))
                        {   
                                adapter.Fill(dt);
                                if (dt.Rows.Count>0){

                                     return Ok(_dataTableService.ConvertToJson(dt));                                      
                                }else
                                {
                                        return BadRequest(new {message = "Nerasta"});
                                }
                        }
                    }
                }
            }catch(Exception ex)
            {
                    return StatusCode(StatusCodes.Status500InternalServerError, new {message = "Klaida" });
            }
        }
        
        
        
        [HttpGet("getCarsAll")]
        [Authorize]
        public IActionResult GetCarsAll([FromQuery] string CLI_ID, [FromQuery] string search = "")
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
                    // Build the base query.
                    var query = @"
                        SELECT 
                            c.CAR_ID, 
                            c.CAR_PLATE_NUMBER, 
                            c.CAR_USER, 
                            COALESCE(
                                (SELECT MAX(cu.CAU_ODO_TO) FROM CARS_USAGE cu WHERE cu.CAU_CAR_ID = c.CAR_ID),
                                c.CAR_INITIAL_ODO
                            ) AS CurrentOdo,
                            c.CAR_USAGE_START_DATE,
                            c.CAR_FCA_ID,
                            c.CAR_SANDELIS,
                            c.CAR_TIKSLAS,
                            c.CAR_TYPE,
                            c.CAR_PADALINYS
                        FROM CARS c
                        WHERE c.CAR_CLI_ID = @CLI_ID
                    ";

                    if (!string.IsNullOrEmpty(search))
                    {
                        query += " AND c.CAR_PLATE_NUMBER LIKE '%' + @search + '%'";
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (Guid.TryParse(CLI_ID, out Guid parsedCliId))
                        {
                            command.Parameters.AddWithValue("@CLI_ID", parsedCliId);
                        }
                        if (!string.IsNullOrEmpty(search))
                        {
                            command.Parameters.AddWithValue("@search", search);
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
                    source: "geCarsAll",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko užkrauti Componento" });
            }
        }
        [HttpPost("updateCar")]
        [Authorize]
        public IActionResult updateCard([FromBody] UpdateCarRequest car)
        {
        var currentUserId = User?.FindFirst("USERID")?.Value;
        if (!_adminService.IsAdmin(currentUserId))
        {
            return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
        }
        
        try
        {
            using (var connection = _connectionProvider.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var checkQuery = "SELECT COUNT(*) FROM CARS WHERE CAR_PLATE_NUMBER = @NewNumber AND CAR_ID <> @CarId";
                    using (var checkCommand = new SqlCommand(checkQuery, connection, transaction))
                    {
                        checkCommand.Parameters.AddWithValue("@NewNumber", car.CAR_PLATE_NUMBER);
                        checkCommand.Parameters.AddWithValue("@CarId", car.CAR_ID);
                        int count = (int)checkCommand.ExecuteScalar();
                        if (count > 0)
                        {
                            transaction.Rollback();
                            return BadRequest(new { message = "Mašina tokiu numeriu jau sukurta." });
                        }
                    }   
                    var updateQuery = @"
                    UPDATE CARS
                    SET CAR_PLATE_NUMBER = @NewNumber,
                        CAR_USER = @User,
                        CAR_FCA_ID = @CAR_FCA_ID,
                        CAR_SANDELIS = @CAR_SANDELIS,
                        CAR_TIKSLAS = @CAR_TIKSLAS,
                        CAR_TYPE = @CAR_TYPE,
                        CAR_PADALINYS = @CAR_PADALINYS
                    WHERE CAR_ID = @CarId";
                using (var updateCommand = new SqlCommand(updateQuery, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@NewNumber", car.CAR_PLATE_NUMBER);
                    updateCommand.Parameters.AddWithValue("@CAR_SANDELIS", car.CAR_SANDELIS);
                    updateCommand.Parameters.AddWithValue("@CAR_TIKSLAS", car.CAR_TIKSLAS);
                    updateCommand.Parameters.AddWithValue("@CAR_TYPE",
                     car.CAR_TYPE.HasValue?(object)car.CAR_TYPE.Value : DBNull.Value);
                     updateCommand.Parameters.AddWithValue("@CAR_PADALINYS",
                     car.CAR_PADALINYS.HasValue?(object)car.CAR_PADALINYS.Value : DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@CarId", car.CAR_ID);
                    updateCommand.Parameters.AddWithValue("@User", 
                        !car.CAR_USER.HasValue || car.CAR_USER.Value == Guid.Empty 
                            ? (object)DBNull.Value 
                            : car.CAR_USER.Value);
                    updateCommand.Parameters.AddWithValue("@CAR_FCA_ID", 
                        car.CAR_FCA_ID.HasValue ? (object)car.CAR_FCA_ID.Value : DBNull.Value);

                    updateCommand.ExecuteNonQuery();
                } 
                    transaction.Commit();
                }
            }
            return Ok(new { message = "Kortelė sėkmingai atnaujinta" });
        }
        catch (Exception ex)
        {
            _exceptionLogger.LogException(
                source: "updateCar",
                message: ex.Message,
                stackTrace: ex.StackTrace
            );
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Nepavyko atnaujinti kortelės. Bandykite dar kartą." });
        }
        }





    public class AddCarRequest
    {
        public string carPlateNumber { get; set; }
        public string initialOdo { get; set; }
        public Guid? userId { get; set; }
        public int? cardId { get; set; }
        public string CLI_ID { get; set; }
        public string sandelis { get; set; }
        public string tikslas { get; set; }
        public int CAR_TYPE { get; set; }
        public int? CAR_PADALINYS { get; set; }

    }
    public class DeleteCarRequest
    {
        public int CAR_ID { get; set; }
    }
    public class UpdateCarRequest
    {
        public int CAR_ID { get; set; }
        public string CAR_PLATE_NUMBER { get; set; }
        public Guid? CAR_USER { get; set; }
        public int? CAR_FCA_ID { get; set; }
        public string CAR_SANDELIS { get; set; }
        public string CAR_TIKSLAS { get; set; }
        public int? CAR_TYPE { get; set; }
        public int? CAR_PADALINYS { get; set; }
    }
    }
}

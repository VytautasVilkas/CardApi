using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Security.Cryptography;

namespace carCard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StartTripController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ConnectionProvider _connectionProvider;
        private readonly ExceptionLogger _exceptionLogger;
        private readonly DataTableService _dataTableService;

    public StartTripController(ConnectionProvider connectionProvider,DataTableService dataTableService,IAdminService adminService)
    {   
    _adminService = adminService;
    _dataTableService = dataTableService;
    _connectionProvider = connectionProvider;
    _exceptionLogger = new ExceptionLogger(_connectionProvider);
    }
    [HttpPost("addTrip")]
    [Authorize]
    public IActionResult AddTrip([FromBody] AddTripRequest trip)
            {
                try
                {
                    using (var connection = _connectionProvider.GetConnection())
                    {
                        connection.Open();
                        string userId = User.FindFirst("USERID")?.Value;
                        if (string.IsNullOrEmpty(userId))
                        {
                            return Unauthorized(new { message = "Nerastas tokenas" });
                        }
                        string carId = null;
                        int carInitialOdo = 0;
                        var carQuery = @"
                            SELECT CAR_ID, CAR_INITIAL_ODO 
                            FROM CARS
                            WHERE CAR_USER = @UserId
                        ";
                        using (var carCommand = new SqlCommand(carQuery, connection))
                        {
                            carCommand.Parameters.AddWithValue("@UserId", userId);
                            using (var reader = carCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    carId = reader["CAR_ID"].ToString();
                                    carInitialOdo = (int)reader["CAR_INITIAL_ODO"];
                                }
                                else
                                {
                                    return NotFound(new { message = "Jum nebuvo priskirta mašina" });
                                }
                            }
                        }
                        int lastOdoFrom = 0;
                        var usageQuery = @"
                            SELECT MAX(CAU_ODO_TO) AS LastOdo
                            FROM CARS_USAGE
                            WHERE CAU_CAR_ID = @CarId
                        ";
                        using (var usageCommand = new SqlCommand(usageQuery, connection))
                        {
                            usageCommand.Parameters.AddWithValue("@CarId", carId);
                            var result = usageCommand.ExecuteScalar();
                            if (result != DBNull.Value && result != null )
                            {
                                lastOdoFrom = (int)result;
                            }
                            else 
                            {
                                lastOdoFrom = carInitialOdo;
                            }
                        }

                        DateTime currentDate = DateTime.UtcNow;

                        if (currentDate.Day < 10 || currentDate.Day > 30)
                        {
                            return BadRequest(new { message = "Įrašai gali būti pateikiami tik nuo 10 iki 15 dienos mėnesyje." });
                        }


                        string checkQuery = @"
                            SELECT COUNT(*) 
                            FROM CARS_USAGE 
                            WHERE CAU_CAR_ID = @CarId 
                            AND YEAR(CAU_DATE) = @Year 
                            AND MONTH(CAU_DATE) = @Month;
                        ";
                        using (var checkCommand = new SqlCommand(checkQuery, connection))
                        {
                            checkCommand.Parameters.AddWithValue("@CarId", carId);
                            checkCommand.Parameters.AddWithValue("@Year", currentDate.Year);
                            checkCommand.Parameters.AddWithValue("@Month", currentDate.Month);
                            int count = (int)checkCommand.ExecuteScalar();
                            if (count > 0)
                            {
                                return BadRequest(new { message = "Ataskaita už šį mėnesį buvo pateikta." });
                            }
                        }

                        // Validate odometer values
                        
                        // if (!decimal.TryParse(trip.OdoTo, out decimal odoToValue))
                        // {
                        //     return BadRequest(new { message = "Nepavyko nustatyti ridos reikšmės" });
                        // }
                        // if (odoFromValue >= odoToValue)
                        // {
                        //     return BadRequest(new { message = "Blogai įvestas odometro kiekis" });
                        // }

                        // Insert the new trip record into CARS_USAGE


                        int? odo = 0;
                        if(trip.OdoTo == 0){
                           odo = lastOdoFrom;
                        }else
                        {
                            odo = trip.OdoTo;
                        }

                        var insertQuery = @"
                            INSERT INTO CARS_USAGE (CAU_CAR_ID, CAU_ODO_FROM, CAU_ODO_TO, CAU_QTU_FROM, CAU_QTY_TO, CAU_DATE)
                            VALUES (@CarId, @OdoFrom, @OdoTo, @QtyFrom, @QtyTo, @Date)
                        ";
                        using (var insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@CarId", carId);
                            insertCommand.Parameters.AddWithValue("@OdoFrom", lastOdoFrom);
                            insertCommand.Parameters.AddWithValue("@OdoTo", odo);
                            insertCommand.Parameters.AddWithValue("@QtyFrom", trip.QtyFrom);
                            insertCommand.Parameters.AddWithValue("@QtyTo", trip.QtyTo);
                            // Use currentDate or DateTime.UtcNow as needed
                            insertCommand.Parameters.AddWithValue("@Date", currentDate);
                            insertCommand.ExecuteNonQuery();
                        }
                    }

                    return Ok(new { message = "Rida sėkmingai pridėta!" });
                }
                catch (Exception ex)
                {
                    _exceptionLogger.LogException(
                        source: "AddTrip",
                        message: ex.Message,
                        stackTrace: ex.StackTrace
                    );
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Nepavyko pridėti kelionės. Bandykite dar kartą." });
                }
            }
    
    [HttpPost("addTripAdmin")]
    [Authorize]
    public IActionResult AddTripAdmin([FromBody] AddTripAdminRequest trip)
            {
                var AdminUserId = User?.FindFirst("USERID")?.Value;
                if (!_adminService.IsAdmin( AdminUserId ))
                {
                    return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
                }
                try
                {
                    using (var connection = _connectionProvider.GetConnection())
                    {
                        connection.Open();
                        
                        string carId = null;
                        int carInitialOdo = 0;
                        var carQuery = @"
                            SELECT CAR_ID, CAR_INITIAL_ODO 
                            FROM CARS
                            WHERE CAR_USER = @UserId
                        ";
                        using (var carCommand = new SqlCommand(carQuery, connection))
                        {
                            carCommand.Parameters.AddWithValue("@UserId", trip.User);
                            using (var reader = carCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    carId = reader["CAR_ID"].ToString();
                                    carInitialOdo = (int)reader["CAR_INITIAL_ODO"];
                                }
                                else
                                {
                                    return NotFound(new { message = "Jum nebuvo priskirta mašina" });
                                }
                            }
                        }
                        int lastOdoFrom = 0;
                        var usageQuery = @"
                            SELECT MAX(CAU_ODO_TO) AS LastOdo
                            FROM CARS_USAGE
                            WHERE CAU_CAR_ID = @CarId
                        ";
                        using (var usageCommand = new SqlCommand(usageQuery, connection))
                        {
                            usageCommand.Parameters.AddWithValue("@CarId", carId);
                            var result = usageCommand.ExecuteScalar();
                            if (result != DBNull.Value && result != null )
                            {
                                lastOdoFrom = (int)result;
                            }
                            else 
                            {
                                lastOdoFrom = carInitialOdo;
                            }
                        }

                        DateTime currentDate = DateTime.UtcNow;

                        if (currentDate.Day < 10 || currentDate.Day > 30)
                        {
                            return BadRequest(new { message = "Įrašai gali būti pateikiami tik nuo 10 iki 15 dienos mėnesyje." });
                        }


                        string checkQuery = @"
                            SELECT COUNT(*) 
                            FROM CARS_USAGE 
                            WHERE CAU_CAR_ID = @CarId 
                            AND YEAR(CAU_DATE) = @Year 
                            AND MONTH(CAU_DATE) = @Month;
                        ";
                        using (var checkCommand = new SqlCommand(checkQuery, connection))
                        {
                            checkCommand.Parameters.AddWithValue("@CarId", carId);
                            checkCommand.Parameters.AddWithValue("@Year", currentDate.Year);
                            checkCommand.Parameters.AddWithValue("@Month", currentDate.Month);
                            int count = (int)checkCommand.ExecuteScalar();
                            if (count > 0)
                            {
                                return BadRequest(new { message = "Ataskaita už šį mėnesį buvo pateikta." });
                            }
                        }

                        // Validate odometer values
                        
                        // if (!decimal.TryParse(trip.OdoTo, out decimal odoToValue))
                        // {
                        //     return BadRequest(new { message = "Nepavyko nustatyti ridos reikšmės" });
                        // }
                        // if (odoFromValue >= odoToValue)
                        // {
                        //     return BadRequest(new { message = "Blogai įvestas odometro kiekis" });
                        // }

                        // Insert the new trip record into CARS_USAGE


                        int? odo = 0;
                        if(trip.OdoTo == 0){
                           odo = lastOdoFrom;
                        }else
                        {
                            odo = trip.OdoTo;
                        }

                        var insertQuery = @"
                            INSERT INTO CARS_USAGE (CAU_CAR_ID, CAU_ODO_FROM, CAU_ODO_TO, CAU_QTU_FROM, CAU_QTY_TO, CAU_DATE)
                            VALUES (@CarId, @OdoFrom, @OdoTo, @QtyFrom, @QtyTo, @Date)
                        ";
                        using (var insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@CarId", carId);
                            insertCommand.Parameters.AddWithValue("@OdoFrom", lastOdoFrom);
                            insertCommand.Parameters.AddWithValue("@OdoTo", odo);
                            insertCommand.Parameters.AddWithValue("@QtyFrom", trip.QtyFrom);
                            insertCommand.Parameters.AddWithValue("@QtyTo", trip.QtyTo);
                            // Use currentDate or DateTime.UtcNow as needed
                            insertCommand.Parameters.AddWithValue("@Date", currentDate);
                            insertCommand.ExecuteNonQuery();
                        }
                    }

                    return Ok(new { message = "Rida sėkmingai pridėta!" });
                }
                catch (Exception ex)
                {
                    _exceptionLogger.LogException(
                        source: "AddTrip",
                        message: ex.Message,
                        stackTrace: ex.StackTrace
                    );
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Nepavyko pridėti kelionės. Bandykite dar kartą." });
                }
            }
    [HttpGet("getCarPlates")]
    [Authorize]
    public IActionResult GetCarPlates()
            {
                try
                {
                    string userId = User.FindFirst("USERID")?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Unauthorized(new { message = "User ID not found in token" });
                    }

                    using (var connection = _connectionProvider.GetConnection())
                    {
                        connection.Open();
                        var query = @"
                            SELECT CAR_PLATE_NUMBER 
                            FROM CARS 
                            WHERE CAR_USER = @userId;
                        ";
                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@userId", userId);
                            var result = command.ExecuteScalar();

                            // Check if the result is null or empty
                            if (result == null || string.IsNullOrEmpty(result.ToString()))
                            {
                                return NotFound(new { message = "Jums nebuvo priskirta mašina" });
                            }

                            return Ok(new { carPlate = result });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _exceptionLogger.LogException(
                        source: "GetCarPlates",
                        message: ex.Message,
                        stackTrace: ex.StackTrace
                    );
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { message = "Klaida gaunant automobilio numerį" });
                }
            }
    [HttpGet("getCarPlatesAdmin")]
    [Authorize]
    public IActionResult GetCarPlatesAdmin([FromQuery]string userId)
    {
        try
        {
        var AdminUserId = User?.FindFirst("USERID")?.Value;
        if (!_adminService.IsAdmin( AdminUserId ))
        {
            return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
        }

        using (var connection = _connectionProvider.GetConnection())
        {
            connection.Open();
            var query = @"
                SELECT CAR_PLATE_NUMBER 
                FROM CARS 
                WHERE CAR_USER = @userId;
            ";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                var result = command.ExecuteScalar();

                // Check if the result is null or empty
                if (result == null || string.IsNullOrEmpty(result.ToString()))
                {
                    return NotFound(new { message = "Jums nebuvo priskirta mašina" });
                }

                return Ok(new { carPlate = result });
            }
        }
        }
        catch (Exception ex)
        {
            _exceptionLogger.LogException(
                source: "GetCarPlates",
                message: ex.Message,
                stackTrace: ex.StackTrace
            );
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Klaida gaunant automobilio numerį" });
        }
    }

    [HttpGet("lastOdo")]
    [Authorize]
    public IActionResult GetLastOdo()
    {
    try
    {
        string userId = User.FindFirst("USERID")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }
        using (var connection = _connectionProvider.GetConnection())
        {
            connection.Open();

            // 2. Query the CARS table to get the car ID and initial odometer reading for this user.
            string carId = null;
            string carInitialOdo = null;
            var carQuery = @"
                SELECT CAR_ID, CAR_INITIAL_ODO 
                FROM CARS
                WHERE CAR_USER = @UserId
            ";
            using (var carCommand = new SqlCommand(carQuery, connection))
            {
                carCommand.Parameters.AddWithValue("@UserId", userId);
                using (var reader = carCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        carId = reader["CAR_ID"].ToString();
                        carInitialOdo = reader["CAR_INITIAL_ODO"].ToString();
                    }
                    else
                    {
                        return NotFound(new { message = "Jum nebuvo priskirta mašina" });
                    }
                }
            }

            // 3. Query the CARS_USAGE table to get the last odometer reading for that car.
            string lastOdo = null;
            var usageQuery = @"
                SELECT MAX(CAU_ODO_TO) AS LastOdo
                FROM CARS_USAGE
                WHERE CAU_CAR_ID = @CarId
            ";
            using (var usageCommand = new SqlCommand(usageQuery, connection))
            {
                usageCommand.Parameters.AddWithValue("@CarId", carId);
                var result = usageCommand.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                {
                    lastOdo = result.ToString();
                }
                else
                {
                    lastOdo = carInitialOdo;
                }
            }

            return Ok(new { lastOdo });
        }
    }
    catch (Exception ex)
    {
        _exceptionLogger.LogException(
            source: "GetLastOdo",
            message: ex.Message,
            stackTrace: ex.StackTrace
        );
        return StatusCode(StatusCodes.Status500InternalServerError, 
            new { message = "Klaida gaunant paskutinę ridą" });
    }
    }

    [HttpGet("lastOdoAdmin")]
    [Authorize]
    public IActionResult GetLastOdoAdmin([FromQuery]string userId)
    {
    try
    {
        var AdminUserId = User?.FindFirst("USERID")?.Value;
        if (!_adminService.IsAdmin( AdminUserId ))
        {
            return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
        }
        using (var connection = _connectionProvider.GetConnection())
        {
            connection.Open();

            string carId = null;
            string carInitialOdo = null;
            var carQuery = @"
                SELECT CAR_ID, CAR_INITIAL_ODO 
                FROM CARS
                WHERE CAR_USER = @UserId
            ";
            using (var carCommand = new SqlCommand(carQuery, connection))
            {
                carCommand.Parameters.AddWithValue("@UserId", userId);
                using (var reader = carCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        carId = reader["CAR_ID"].ToString();
                        carInitialOdo = reader["CAR_INITIAL_ODO"].ToString();
                    }
                    else
                    {
                        return NotFound(new { message = "Jum nebuvo priskirta mašina" });
                    }
                }
            }
            string lastOdo = null;
            var usageQuery = @"
                SELECT MAX(CAU_ODO_TO) AS LastOdo
                FROM CARS_USAGE
                WHERE CAU_CAR_ID = @CarId
            ";
            using (var usageCommand = new SqlCommand(usageQuery, connection))
            {
                usageCommand.Parameters.AddWithValue("@CarId", carId);
                var result = usageCommand.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                {
                    lastOdo = result.ToString();
                }
                else
                {
                    lastOdo = carInitialOdo;
                }
            }

            return Ok(new { lastOdo });
        }
    }
    catch (Exception ex)
    {
        _exceptionLogger.LogException(
            source: "GetLastOdo",
            message: ex.Message,
            stackTrace: ex.StackTrace
        );
        return StatusCode(StatusCodes.Status500InternalServerError, 
            new { message = "Klaida gaunant paskutinę ridą" });
    }
    }
    [HttpGet("getCardsUsage")]
    [Authorize]
    public IActionResult GetCardsUsage(
            [FromQuery] string search = "",
            [FromQuery] string startDate = "",
            [FromQuery] string endDate  = "")
        {
            string userId = User.FindFirst("USERID")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }
            
            string FCA_ID = "";
            try
            {
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    var queryCarId = "SELECT CAR_FCA_ID FROM CARS WHERE CAR_USER = @UserId;";
                    using (var command = new SqlCommand(queryCarId, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            FCA_ID = result.ToString();
                        }
                        else
                        {
                            return NotFound(new { message = "Nepavyko gauti kortelės duomenų" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "GetCardsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko gauti kortelės duomenų" });
            }

            try
            {
                bool noFilters = string.IsNullOrEmpty(search) && string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate);
                string query = "";
                if (noFilters)
                {
                    query = @"
                        SELECT TOP 100 
                            c.*, 
                            f.FUEL_NAME 
                        FROM CARDS_USAGE c
                        INNER JOIN FUEL_TYPE f ON c.FCU_FUEL_TYPE = f.FUEL_ID
                        WHERE c.FCU_FCA_ID = @FCA_ID
                        ORDER BY c.FCU_DATE DESC;";

                }
                else
                {
                    query = @"
                        SELECT 
                            c.*, 
                            f.FUEL_NAME 
                        FROM CARDS_USAGE c
                        INNER JOIN FUEL_TYPE f ON c.FCU_FUEL_TYPE = f.FUEL_ID
                        WHERE c.FCU_FCA_ID = @FCA_ID
                            AND (@vieta = '' OR c.FCU_FUELING_PLACE_NAME COLLATE Latin1_General_CI_AI LIKE '%' + @vieta + '%')
                            AND (@dateFrom = '' OR c.FCU_DATE >= @dateFrom)
                            AND (@dateTo = '' OR c.FCU_DATE < DATEADD(day, 1, @dateTo))
                        ORDER BY c.FCU_DATE DESC;";
                }

                var dataTable = new DataTable();
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FCA_ID", FCA_ID);
                        command.Parameters.AddWithValue("@vieta", string.IsNullOrEmpty(search) ? "" : search);
                        command.Parameters.AddWithValue("@dateFrom", string.IsNullOrEmpty(startDate) ? "" : startDate);
                        command.Parameters.AddWithValue("@dateTo", string.IsNullOrEmpty(endDate) ? "" : endDate);

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
                    source: "GetCardsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko užkrauti Componento" });
            }
        }



    [HttpGet("getCardsUsageAdmin")]
    [Authorize]
    public IActionResult GetCardsUsageAdmin(
            [FromQuery] string search = "",
            [FromQuery] string startDate = "",
            [FromQuery] string endDate  = "",
            [FromQuery] string selectedUserId = "")
            
        {
            var currentUserId = User?.FindFirst("USERID")?.Value;
            if (!_adminService.IsAdmin(currentUserId))
            {
                return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
            }
            if (string.IsNullOrEmpty(selectedUserId))
            {
                return BadRequest(new { message = "Negautas vartotojo id" });
            }
            string FCA_ID = "";
            try
            {
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    var queryCarId = "SELECT CAR_FCA_ID FROM CARS WHERE CAR_USER = @UserId;";
                    using (var command = new SqlCommand(queryCarId, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", selectedUserId );
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            FCA_ID = result.ToString();
                        }
                        else
                        {
                            return NotFound(new { message = "Nepavyko gauti kortelės duomenų" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "GetCardsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko gauti kortelės duomenų" });
            }

            try
            {
                bool noFilters = string.IsNullOrEmpty(search) && string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate);
                string query = "";
                if (noFilters)
                {
                    query = @"
                        SELECT TOP 100 
                            c.*, 
                            f.FUEL_NAME 
                        FROM CARDS_USAGE c
                        INNER JOIN FUEL_TYPE f ON c.FCU_FUEL_TYPE = f.FUEL_ID
                        WHERE c.FCU_FCA_ID = @FCA_ID
                        ORDER BY c.FCU_DATE DESC;";

                }
                else
                {
                    query = @"
                        SELECT 
                            c.*, 
                            f.FUEL_NAME 
                        FROM CARDS_USAGE c
                        INNER JOIN FUEL_TYPE f ON c.FCU_FUEL_TYPE = f.FUEL_ID
                        WHERE c.FCU_FCA_ID = @FCA_ID
                            AND (@vieta = '' OR c.FCU_FUELING_PLACE_NAME COLLATE Latin1_General_CI_AI LIKE '%' + @vieta + '%')
                            AND (@dateFrom = '' OR c.FCU_DATE >= @dateFrom)
                            AND (@dateTo = '' OR c.FCU_DATE < DATEADD(day, 1, @dateTo))
                        ORDER BY c.FCU_DATE DESC;";
                }

                var dataTable = new DataTable();
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FCA_ID", FCA_ID);
                        command.Parameters.AddWithValue("@vieta", string.IsNullOrEmpty(search) ? "" : search);
                        command.Parameters.AddWithValue("@dateFrom", string.IsNullOrEmpty(startDate) ? "" : startDate);
                        command.Parameters.AddWithValue("@dateTo", string.IsNullOrEmpty(endDate) ? "" : endDate);

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
                    source: "GetCardsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko užkrauti Componento" });
            }
        }

        [HttpGet("getCarsUsage")]
        [Authorize]
        public IActionResult GetCarsUsage(
            [FromQuery] string searchFrom = "",
            [FromQuery] string searchTo = "")
        {
            string userId = User.FindFirst("USERID")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }
            string carId = "";
            try
            {
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    var queryCarId = "SELECT CAR_ID FROM CARS WHERE CAR_USER = @UserId;";
                    using (var command = new SqlCommand(queryCarId, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            carId = result.ToString();
                        }
                        else
                        {
                            return NotFound(new { message = "Nepavyko gauti mašinos id" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "GetCarsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko gauti mašinos" });
            }

            try
            {
                var dataTable = new DataTable();
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    // Start with the base query.
                    var query = @"
                        SELECT * FROM CARS_USAGE 
                        WHERE CAU_CAR_ID = @CarId";
                    
                    // If search dates are provided, add conditions.
                    if (!string.IsNullOrEmpty(searchFrom) && !string.IsNullOrEmpty(searchTo))
                    {
                        query += " AND CAU_DATE BETWEEN @searchFrom AND @searchTo";
                    }
                    else if (!string.IsNullOrEmpty(searchFrom))
                    {
                        query += " AND CAU_DATE >= @searchFrom";
                    }
                    else if (!string.IsNullOrEmpty(searchTo))
                    {
                        query += " AND CAU_DATE <= @searchTo";
                    }
                    
                    // Order by descending date.
                    query += " ORDER BY CAU_DATE DESC;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CarId", carId);
                        if (!string.IsNullOrEmpty(searchFrom))
                        {
                            // Assuming searchFrom is in a valid date format (e.g., "2025-02-01")
                            command.Parameters.AddWithValue("@searchFrom", DateTime.Parse(searchFrom));
                        }
                        if (!string.IsNullOrEmpty(searchTo))
                        {
                            // Same assumption for searchTo.
                            command.Parameters.AddWithValue("@searchTo", DateTime.Parse(searchTo));
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
                    source: "GetCarsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko užkrauti Componento" });
            }
        }

    [HttpGet("getCarsUsageAdmin")]
    [Authorize]
    public IActionResult GetCarsUsageAdmin([FromQuery] string searchFrom  = "",
            [FromQuery] string searchTo  = "",[FromQuery] string selectedUserId = "")
    {
        var currentUserId = User?.FindFirst("USERID")?.Value;
        if (!_adminService.IsAdmin(currentUserId))
        {
            return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
        }
        if (string.IsNullOrEmpty(selectedUserId))
        {
            return BadRequest(new { message = "Negautas vartotojo id" });
        }
        string carId = "";
            try
            {
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    var queryCarId = "SELECT CAR_ID FROM CARS WHERE CAR_USER = @UserId;";
                    using (var command = new SqlCommand(queryCarId, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", selectedUserId );
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            carId = result.ToString();
                        }
                        else
                        {
                            return NotFound(new { message = "Nepavyko gauti mašinos id" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "GetCarsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko gauti mašinos" });
            }

            try
            {
                var dataTable = new DataTable();
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    // Start with the base query.
                    var query = @"
                        SELECT * FROM CARS_USAGE 
                        WHERE CAU_CAR_ID = @CarId";
                    
                    // If search dates are provided, add conditions.
                    if (!string.IsNullOrEmpty(searchFrom) && !string.IsNullOrEmpty(searchTo))
                    {
                        query += " AND CAU_DATE BETWEEN @searchFrom AND @searchTo";
                    }
                    else if (!string.IsNullOrEmpty(searchFrom))
                    {
                        query += " AND CAU_DATE >= @searchFrom";
                    }
                    else if (!string.IsNullOrEmpty(searchTo))
                    {
                        query += " AND CAU_DATE <= @searchTo";
                    }
                    
                    // Order by descending date.
                    query += " ORDER BY CAU_DATE DESC;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CarId", carId);
                        if (!string.IsNullOrEmpty(searchFrom))
                        {
                            
                            command.Parameters.AddWithValue("@searchFrom", DateTime.Parse(searchFrom));
                        }
                        if (!string.IsNullOrEmpty(searchTo))
                        {
                            
                            command.Parameters.AddWithValue("@searchTo", DateTime.Parse(searchTo));
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
                    source: "GetCarsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Nepavyko užkrauti Componento" });
            }
        }
    [HttpPost("updateCarsUsage")]
    [Authorize]
    public IActionResult UpdateCarsUsage([FromBody] CarsUsageUpdateRequest request)
        {
            try
            {
                // Get the user id from the token.
                string userId = User.FindFirst("USERID")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    var query = @"
                        UPDATE CARS_USAGE
                        SET 
                            CAU_DATE = @Date,
                            CAU_ODO_FROM = @OdoFrom,
                            CAU_ODO_TO = @OdoTo,
                            CAU_QTU_FROM = @QtyFrom,
                            CAU_QTY_TO = @QtyTo
                        WHERE CAU_ID = @Id
                        AND CAU_CAR_ID IN (SELECT CAR_ID FROM CARS WHERE CAR_USER = @UserId);
                    ";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Date", request.CAU_DATE);
                        command.Parameters.AddWithValue("@OdoFrom", request.CAU_ODO_FROM);
                        command.Parameters.AddWithValue("@OdoTo", request.CAU_ODO_TO);
                        command.Parameters.AddWithValue("@QtyFrom", request.CAU_QTU_FROM);
                        command.Parameters.AddWithValue("@QtyTo", request.CAU_QTY_TO);
                        command.Parameters.AddWithValue("@Id", request.CAU_ID);
                        command.Parameters.AddWithValue("@UserId", userId);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Ok(new { message = "Sėkmingai pataisyta" });
                        }
                        else
                        {
                            return NotFound(new { message = "Įrašas nerastas" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "UpdateCarsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Klaida darant atnaujinimą", details = ex.Message });
            }
        }
    [HttpPost("updateCarsUsageAdmin")]
    [Authorize]
    public IActionResult UpdateCarsUsageAdmin([FromBody] CarsUsageAdminUpdateRequest request,[FromQuery] string selectedUserId)
        {
            
            try
            {
                var currentUserId = User?.FindFirst("USERID")?.Value;
                if (!_adminService.IsAdmin(currentUserId))
                {
                    return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
                }
                if (string.IsNullOrEmpty(selectedUserId))
                {
                    return BadRequest(new { message = "Negautas vartotojo id" });
                }
                using (var connection = _connectionProvider.GetConnection())
                {
                    connection.Open();
                    var query = @"
                        UPDATE CARS_USAGE
                        SET 
                            CAU_DATE = @Date,
                            CAU_ODO_FROM = @OdoFrom,
                            CAU_ODO_TO = @OdoTo,
                            CAU_QTU_FROM = @QtyFrom,
                            CAU_QTY_TO = @QtyTo
                        WHERE CAU_ID = @Id
                        AND CAU_CAR_ID IN (SELECT CAR_ID FROM CARS WHERE CAR_USER = @UserId);
                    ";
                    

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Date", request.CAU_DATE);
                        command.Parameters.AddWithValue("@OdoFrom", request.CAU_ODO_FROM);
                        command.Parameters.AddWithValue("@OdoTo", request.CAU_ODO_TO);
                        command.Parameters.AddWithValue("@QtyFrom", request.CAU_QTU_FROM);
                        command.Parameters.AddWithValue("@QtyTo", request.CAU_QTY_TO);
                        command.Parameters.AddWithValue("@Id", request.CAU_ID);
                        command.Parameters.AddWithValue("@UserId", selectedUserId);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Ok(new { message = "Sėkmingai pataisyta" });
                        }
                        else
                        {
                            return NotFound(new { message = "Įrašas nerastas" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionLogger.LogException(
                    source: "UpdateCarsUsage",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Klaida darant atnaujinimą", details = ex.Message });
            }
        }
    }
    public class AddTripAdminRequest
        {
            public string? User { get; set; }
            public int? OdoTo { get; set; }
            public int? QtyFrom { get; set; }
            public int? QtyTo { get; set; }
        }
        public class AddTripRequest
        {
            public int? OdoTo { get; set; }
            public int? QtyFrom { get; set; }
            public int? QtyTo { get; set; }
        }
    public class CarsUsageUpdateRequest
    {
        public int CAU_ID { get; set; }
        public DateTime CAU_DATE { get; set; }
        public decimal CAU_ODO_FROM { get; set; }
        public decimal CAU_ODO_TO { get; set; }
        public decimal CAU_QTU_FROM { get; set; }
        public decimal CAU_QTY_TO { get; set; }
    }
    public class CarsUsageAdminUpdateRequest
    {        
        public int CAU_ID { get; set; }
        public DateTime CAU_DATE { get; set; }
        public decimal CAU_ODO_FROM { get; set; }
        public decimal CAU_ODO_TO { get; set; }
        public decimal CAU_QTU_FROM { get; set; }
        public decimal CAU_QTY_TO { get; set; }
    }
}


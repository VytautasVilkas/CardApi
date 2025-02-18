using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
// using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
// using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;
// using MimeKit;
// using Newtonsoft.Json;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Query.Internal;
using OfficeOpenXml.Drawing.Chart;
using System.Net.WebSockets;


namespace carCard.Controllers
{
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{   
    private readonly IAdminService _adminService;
    private readonly ConnectionProvider _connectionProvider;
    private readonly DataTableService _dataTableService;
    private ExceptionLogger exceptionLogger;
    public UserController(ConnectionProvider connectionProvider, DataTableService dataTableService,IAdminService adminService)
    {
        _adminService = adminService;
        _connectionProvider = connectionProvider;
        _dataTableService = dataTableService;
        exceptionLogger = new ExceptionLogger(_connectionProvider);
    }

                [HttpPost("logout")]
                [Authorize]
                public IActionResult Logout()
                    {
                    try
                    {
                        var UserID = User?.FindFirst("USERID")?.Value;
                        if (!string.IsNullOrEmpty(UserID))
                        {
                        
                        deleteOldRefreshTokens(UserID);
                        }
                        Response.Cookies.Delete("ACCESS_TOKEN");
                        Response.Cookies.Delete("REFRESH_TOKEN");                
                        return Ok(new { message = "Atsijungta sekmingai" });
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "Logout",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(500, new { message = "Klaida Bandant atsijungti" });
                    }
                    }

                [HttpPost("login")]
                [AllowAnonymous]
                public IActionResult Login([FromBody] LoginRequest request)
                {
                    try
                    {   
                        var user = AuthenticateUser(request.USERNAME, request.PASSWORD);
                        if (user == null)
                        {
                            return Unauthorized(new { message = "Neteisingas Vartotojo vardas arba slaptažodis" });
                        }
                        deleteOldRefreshTokens(user.USERID);
                        return GenerateSessionAndTokens(user);
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "login",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Klaida jungiantis" });
                    }
                }

                private IActionResult GenerateSessionAndTokens(AppUser user)
                {
                    try
                    {
                        var accessToken = GenerateJwtToken(user.USERID, user.Role,user.CLI_ID);
                        var refreshToken = GenerateRefreshToken();
                        SaveRefreshToken(user.USERID, refreshToken);

                        Response.Cookies.Append("ACCESS_TOKEN", accessToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddMinutes(15)
                        });

                        Response.Cookies.Append("REFRESH_TOKEN", refreshToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddDays(7)
                        });

                        // Explicitly name the role property
                        return Ok(new { message = "Sekmingai prisijungta", isValid = true, role = user.Role,user.USERNAME,user.NAME, user.SURNAME,user.CLI_ID });
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "GenerateSessionAndTokens",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Klaida Bandant prisijungti" });
                    }
                }

                [HttpPost("addUser")]
                [Authorize]
                public IActionResult AddUser([FromBody] DTOAddUser addUserRequest)
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
                            var query = "SELECT count(*) FROM [USER] WHERE USERNAME = @USERNAME";
                            using (var command = new SqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@USERNAME", addUserRequest.USERNAME);
                                var count = (int)command.ExecuteScalar(); 
                                if (count > 0)
                                {
                                    return BadRequest(new { message = $"Vartotojas su vardu {addUserRequest.USERNAME} jau egzistuoja." });
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "AddUser",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(500, new { message = "Klaida bandant patikrinti vartotojo vardą" });
                    }
                    var newUserId = Guid.NewGuid().ToString();
                    var defaultRole = "2";
                    try
                    {
                        using (var connection = _connectionProvider.GetConnection())
                        {
                            connection.Open();
                            var query = @"
                                INSERT INTO [USER] (USERID, USERNAME, PASSWORD, NAME, SURNAME, ROLE, CLI_ID, DATE)
                                VALUES (@USERID, @USERNAME, @PASSWORD, @NAME, @SURNAME, @ROLE,@CLI_ID,@Date)
                            ";
                            using (var command = new SqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@USERID", newUserId);
                                command.Parameters.AddWithValue("@USERNAME", addUserRequest.USERNAME);
                                command.Parameters.AddWithValue("@PASSWORD", HashPassword(addUserRequest.PASSWORD));
                                command.Parameters.AddWithValue("@NAME", addUserRequest.NAME);
                                command.Parameters.AddWithValue("@SURNAME", addUserRequest.SURNAME);
                                command.Parameters.AddWithValue("@ROLE", defaultRole);
                                command.Parameters.AddWithValue("@CLI_ID", addUserRequest.CLI_ID);
                                command.Parameters.AddWithValue("@Date", DateTime.UtcNow);
                                command.ExecuteNonQuery();
                            }
                            return Ok(new { message = "Vartotojas sėkmingai pridėtas", userId = newUserId, role = defaultRole });
                        }
                    }
                    catch(Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "AddUser",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(500, new { message = "Klaida bandant sukurti vartotoją" });
                    }
                }
                
                [HttpPost("DeleteUser")]
                [Authorize]
                public IActionResult DeleteUser([FromBody] DTODeleteUser user)
                {
                    try
                    {
                        using (var connection = _connectionProvider.GetConnection())
                        {
                            connection.Open();
                            using (var transaction = connection.BeginTransaction())
                            {
                                string query = "DELETE FROM [USER] WHERE USERID = @USER_ID";
                                using (var command = new SqlCommand(query, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@USER_ID", user.USER_ID);
                                    int count = command.ExecuteNonQuery();
                                    if (count <= 0)
                                    {
                                        transaction.Rollback();
                                        return BadRequest(new { message = "Klaida: Nepavyko ištrinti vartotojo" });
                                    }
                                }

                                string queryCar = "UPDATE CARS SET CAR_USER = NULL WHERE CAR_USER = @USER_ID";
                                using (var command = new SqlCommand(queryCar, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@USER_ID", user.USER_ID);
                                    int count = command.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                return Ok(new { message = "Vartotojas sėkmingai ištrintas" });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "DeleteUser",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Klaida" });
                    }
                }

  
                [HttpGet("getUsersNotConnected")]
                [Authorize]
                public IActionResult GetUsersNotConnected([FromQuery]string CLI_ID)
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
                            var query = @"
                                SELECT u.USERID, u.USERNAME, u.ROLE
                                FROM [USER] u
                                WHERE 
                                u.CLI_ID = @CLI_ID
                                AND NOT EXISTS (
                                    SELECT 1 
                                    FROM CARS c 
                                    WHERE c.CAR_USER = u.USERID
                                );
                            ";

                            var command = new SqlCommand(query, connection);
                            var users = new List<AppUser>();
                            if (Guid.TryParse(CLI_ID, out Guid parsedCliId))
                            {
                                command.Parameters.AddWithValue("@CLI_ID", parsedCliId);
                            }
                            using (var reader = command.ExecuteReader())
                            {                          
                                while (reader.Read())
                                {
                                    var user = new AppUser
                                    {
                                        USERID = reader["USERID"].ToString(),
                                        USERNAME = reader["USERNAME"].ToString(),
                                        Role = reader["ROLE"].ToString()
                                    };
                                    users.Add(user);
                                }
                            }
                            return Ok(users);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "getUsersNotConnected",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Klaida" });
                    }
                    
                }
                
                [HttpGet("getUsers")]
                [Authorize]
                public IActionResult GetUsers([FromQuery]string CLI_ID)
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
                            var query = @"
                                SELECT u.USERID, u.USERNAME, u.ROLE
                                FROM [USER] u
                                WHERE 
                                u.CLI_ID = @CLI_ID
                            ";

                            var command = new SqlCommand(query, connection);
                            var users = new List<AppUser>();
                            if (Guid.TryParse(CLI_ID, out Guid parsedCliId))
                            {
                                command.Parameters.AddWithValue("@CLI_ID", parsedCliId);
                            }
                            using (var reader = command.ExecuteReader())
                            {                          
                                while (reader.Read())
                                {
                                    var user = new AppUser
                                    {
                                        USERID = reader["USERID"].ToString(),
                                        USERNAME = reader["USERNAME"].ToString(),
                                        Role = reader["ROLE"].ToString()
                                    };
                                    users.Add(user);
                                }
                            }
                            return Ok(users);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "GetUsers",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Klaida" });
                    }
                    
                }

                [HttpGet("getUsersAll")]
                [Authorize]
                public IActionResult GetUsersFULL([FromQuery] string CLI_ID, [FromQuery] string search = "")
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
                            var query = @"
                                SELECT 
                                    u.USERID, 
                                    u.USERNAME, 
                                    u.ROLE, 
                                    u.NAME, 
                                    u.SURNAME,
                                    MIN(c.CAR_PLATE_NUMBER) AS CAR_PLATE_NUMBER, 
                                    MIN(d.FCA_NUMBER) AS FCA_NUMBER
                                FROM [USER] u
                                LEFT JOIN CARS c ON c.CAR_USER = u.USERID
                                LEFT JOIN CARDS d ON d.FCA_ID = c.CAR_FCA_ID
                                WHERE u.CLI_ID = @CLI_ID
                            ";
                            if (!string.IsNullOrEmpty(search))
                            {
                                query += @"
                                    AND (
                                        u.USERNAME LIKE '%' + @search + '%' OR 
                                        u.NAME LIKE '%' + @search + '%' OR 
                                        u.SURNAME LIKE '%' + @search + '%'
                                    )
                                ";
                            }

                            query += " GROUP BY u.USERID, u.USERNAME, u.ROLE, u.NAME, u.SURNAME;";

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

                                var users = new List<AppUserFULL>();
                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var user = new AppUserFULL
                                        {
                                            USERID = reader["USERID"].ToString(),
                                            USERNAME = reader["USERNAME"].ToString(),
                                            ROLE = reader["ROLE"].ToString(),
                                            NAME = reader["NAME"].ToString(),
                                            SURNAME = reader["SURNAME"].ToString(),
                                            CAR_NUMBER = reader["CAR_PLATE_NUMBER"].ToString(),
                                            CARD_NUMBER = reader["FCA_NUMBER"].ToString(),
                                        };
                                        users.Add(user);
                                    }
                                }
                                return Ok(users);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "getUsersAll",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(StatusCodes.Status500InternalServerError,
                            new { message = "Error retrieving users." });
                    }
                }

                [HttpPost("UpdateUser")]
                [Authorize]
                public IActionResult UpdateUser([FromBody] UpdateUserRequest request)
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
                                // Update user details.
                                var queryUser = @"
                                    UPDATE [USER]
                                    SET 
                                        USERNAME = @USERNAME,
                                        ROLE = @ROLE,
                                        NAME = @NAME,
                                        SURNAME = @SURNAME
                                    WHERE USERID = @USERID;
                                ";
                                using (var commandUser = new SqlCommand(queryUser, connection, transaction))
                                {
                                    commandUser.Parameters.AddWithValue("@USERID", request.USERID);
                                    commandUser.Parameters.AddWithValue("@USERNAME", request.USERNAME);
                                    commandUser.Parameters.AddWithValue("@ROLE", request.ROLE);
                                    commandUser.Parameters.AddWithValue("@NAME", request.NAME);
                                    commandUser.Parameters.AddWithValue("@SURNAME", request.SURNAME);
                                    
                                    int userRowsAffected = commandUser.ExecuteNonQuery();
                                    if (userRowsAffected <= 0)
                                    {
                                        transaction.Rollback();
                                        return NotFound(new { message = "Vartotojas nerastas" });
                                    }
                                }
                                if (request.CAR_ID == null )
                                {
                                    try
                                    {
                                            var deleteQuery = @"
                                                UPDATE CARS 
                                                SET CAR_USER = NULL
                                                WHERE CAR_USER = @USERID;
                                            ";
                                            using (var deleteCommand = new SqlCommand(deleteQuery, connection,transaction))
                                            {
                                                deleteCommand.Parameters.AddWithValue("@USERID", request.USERID);
                                                int rowsAffected = deleteCommand.ExecuteNonQuery(); 
                                                transaction.Commit();
                                                return Ok(new { message = "Sekmingai pataisyta" ,currentFcaNumber = ""});
                                            }
                                    }
                                    catch (Exception ex)
                                    {
                                        exceptionLogger.LogException("DeleteFromUser", ex.Message, ex.StackTrace);
                                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Klaida atnaujinant vartotoją." });
                                    }
                                }

                                if (request.CAR_ID != null)
                                {
                                    var checkQuery = "SELECT CAR_USER FROM CARS WHERE CAR_ID = @CAR_ID;";
                                    using (var checkCommand = new SqlCommand(checkQuery, connection, transaction))
                                    {
                                        checkCommand.Parameters.AddWithValue("@CAR_ID", request.CAR_ID);
                                        var assignedObj = checkCommand.ExecuteScalar();
                                        if (assignedObj != null && assignedObj != DBNull.Value)
                                        {
                                            var assignedUserId = assignedObj.ToString();
                                            if (!assignedUserId.Equals(request.USERID, StringComparison.OrdinalIgnoreCase))
                                            {
                                                string currentCarPlate = "";
                                                string currentFcaNumber = "";
                                                var getCurrentCarQuery = @"
                                                    SELECT c.CAR_PLATE_NUMBER, d.FCA_NUMBER
                                                    FROM CARS c
                                                    INNER JOIN CARDS d ON c.CAR_FCA_ID = d.FCA_ID
                                                    WHERE c.CAR_USER = @USERID;
                                                ";
                                                using (var currentCarCommand = new SqlCommand(getCurrentCarQuery, connection, transaction))
                                                {
                                                    currentCarCommand.Parameters.AddWithValue("@USERID", request.USERID);
                                                    using (var reader = currentCarCommand.ExecuteReader())
                                                    {
                                                        if (reader.Read())
                                                        {
                                                            currentCarPlate = reader["CAR_PLATE_NUMBER"].ToString();
                                                            currentFcaNumber = reader["FCA_NUMBER"].ToString();
                                                        }
                                                    }
                                                }
                                                transaction.Rollback();
                                                
                                                return BadRequest(new
                                                {
                                                    message = "Mašina jau yra priskirta kitam vartotojui.",
                                                    currentCarPlate,
                                                    currentFcaNumber
                                                });
                                            }
                                        }
                                    }


                                    var deleteQuery = @"
                                            UPDATE CARS 
                                            SET CAR_USER = NULL
                                            WHERE CAR_USER = @USERID;
                                        ";
                                        using (var deleteCommand = new SqlCommand(deleteQuery, connection, transaction))
                                        {
                                            deleteCommand.Parameters.AddWithValue("@USERID", request.USERID);
                                            deleteCommand.ExecuteNonQuery();
                                        }
                                    var queryCar = @"
                                        UPDATE CARS
                                        SET CAR_USER = @USERID
                                        WHERE CAR_ID = @CAR_ID;
                                    ";
                                    using (var commandCar = new SqlCommand(queryCar, connection, transaction))
                                    {
                                        commandCar.Parameters.AddWithValue("@USERID", request.USERID);
                                        commandCar.Parameters.AddWithValue("@CAR_ID", request.CAR_ID);
                                        int carRowsAffected = commandCar.ExecuteNonQuery();
                                        if (carRowsAffected <= 0)
                                        {
                                            transaction.Rollback();
                                            return NotFound(new { message = "Mašina nerasta" });
                                        }
                                    }
                                    string newFcaNumber = "";
                                    var getNewCarQuery = @"
                                        SELECT d.FCA_NUMBER
                                        FROM CARS c
                                        INNER JOIN CARDS d ON c.CAR_FCA_ID = d.FCA_ID
                                        WHERE c.CAR_ID = @CAR_ID;
                                    ";
                                    using (var getNewCarCommand = new SqlCommand(getNewCarQuery, connection, transaction))
                                    {
                                        getNewCarCommand.Parameters.AddWithValue("@CAR_ID", request.CAR_ID);
                                        var result = getNewCarCommand.ExecuteScalar();
                                        if (result != null && result != DBNull.Value)
                                        {
                                            newFcaNumber = result.ToString();
                                        }
                                    }
                                    // Optionally, you could add newFcaNumber to the success response.
                                    transaction.Commit();
                                    return Ok(new { message = "Sekmingai pataisyta", newFcaNumber });
                                }
                                else
                                {
                                    transaction.Commit();
                                    return Ok(new { message = "Sekmingai pataisyta" });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException("UpdateUser", ex.Message, ex.StackTrace);
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Klaida atnaujinant vartotoją." });
                    }
                }
                [HttpPost("UpdateUserCarToNull")]
                [Authorize]
                public IActionResult UpdateUserCarToNull([FromBody] UpdateUserCarToNullRequest   request)
                {
                    var currentUserId = User?.FindFirst("USERID")?.Value;
                    if (!_adminService.IsAdmin(currentUserId))
                    {
                        return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
                    }
                    try{
                    using (var connection = _connectionProvider.GetConnection()){
                    connection.Open();
                    var deleteQuery = @"
                                            UPDATE CARS 
                                            SET CAR_USER = NULL
                                            WHERE CAR_USER = @USERID;
                                        ";
                                        using (var deleteCommand = new SqlCommand(deleteQuery, connection ))
                                        {
                                            deleteCommand.Parameters.AddWithValue("@USERID", request.USERID);
                                            deleteCommand.ExecuteNonQuery();
                                        }
                                        return Ok(new { message = "Sekmingai panaikinta" });
                    }
                    }catch(Exception ex){
                             exceptionLogger.LogException("DeleteFromUser", ex.Message, ex.StackTrace);
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Klaida atnaujinant vartotoją." });
                    }



                }
                [HttpGet("getAdministrators")]
                [Authorize]
                public IActionResult GetAdministrators()
                {
                    try
                    {
                        using (var connection = _connectionProvider.GetConnection())
                        {
                            connection.Open();
                            var query = @"
                                SELECT USERID, USERNAME, ROLE
                                FROM [USER] WHERE ROLE = 1;
                            ";

                            var command = new SqlCommand(query, connection);
                            var users = new List<AppUser>();

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var user = new AppUser
                                    {
                                        USERID = reader["USERID"].ToString(),
                                        USERNAME = reader["USERNAME"].ToString(),
                                        Role = reader["ROLE"].ToString()
                                    };
                                    users.Add(user);
                                }
                            }
                            return Ok(users);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionLogger.LogException(
                            source: "GetUsers",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                        );
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving users." });
                    }
                }
                [HttpPost("refreshToken")]
                [AllowAnonymous]
                public IActionResult RefreshToken()
                {
                    try
                    {
                        var refreshToken = Request.Cookies["REFRESH_TOKEN"];
                        if (string.IsNullOrEmpty(refreshToken))
                        {
                            return Unauthorized(new { message = "Atnaujinimo tokenas nebegalioja" });
                        }
                        var USERID = GetUSERFromRefreshToken(refreshToken);
                        if (USERID  == null)
                        {   
                            deleteRefreshToken(refreshToken);
                            return Unauthorized(new { message = "Sesija nebegalioja" });
                        }
                        
                        var user = GetUserById(USERID);
                        var newAccessToken = GenerateJwtToken(user.USERID,user.Role,user.CLI_ID);
                        var newRefreshToken = GenerateRefreshToken();
                        SaveRefreshToken(user.USERID, newRefreshToken);
                        Response.Cookies.Append("ACCESS_TOKEN", newAccessToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddMinutes(15)
                        });

                        Response.Cookies.Append("REFRESH_TOKEN", newRefreshToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddDays(7)
                        });

                        return Ok(new { message = "Sekmingai atnaujinta ", isValid = true, user.Role, user.NAME, user.SURNAME, user.CLI_ID});
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error refreshing token: {ex.Message}");
                        exceptionLogger.LogException(
                                    source: "refreshToken",
                                    message: ex.Message,
                                    stackTrace: ex.StackTrace
                        );
                        return StatusCode(500, new { message = "Klaida atnaujinant tokena" });
                    }
                }
                private string GetUSERFromRefreshToken(string refreshToken)
                    {
                        try
                        {
                            using (var connection = _connectionProvider.GetConnection())
                            {
                                connection.Open();
                                var command = new SqlCommand(
                                    @"
                                    SELECT USERID
                                    FROM RefreshTokens rt
                                    WHERE rt.Token = @Token AND rt.ExpiryDate > @Now
                                    ",
                                    connection
                                );
                                command.Parameters.AddWithValue("@Token", refreshToken);
                                command.Parameters.AddWithValue("@Now", DateTime.UtcNow);

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        return reader["USERID"].ToString();
                                    }
                                    else
                                    {
                                        return null;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error retrieving refreshToken ID: {ex.Message}");
                            throw;
                        }
                    }
                private AppUser GetUserById(string userId)
                    {   
                        if(_adminService.IsTheAdmin(userId))   
                        {
                            Console.WriteLine("Admin user");
                            return new AppUser{
                            USERID = AdminConfig.AdminGuid.ToString(),
                            USERNAME = "Admin",
                            NAME = "Adminas",
                            Role = "0",
                            CLI_ID = ""
                        };
                        }
                        try
                        {
                            using (var connection = _connectionProvider.GetConnection())
                            {
                                connection.Open();
                                var query = @"
                                    SELECT USERID, USERNAME, ROLE, NAME, SURNAME,CLI_ID
                                    FROM [USER]
                                    WHERE USERID = @UserId
                                ";

                                var command = new SqlCommand(query, connection);
                                command.Parameters.AddWithValue("@UserId", userId);

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        return new AppUser
                                        {
                                            USERID = reader["USERID"].ToString(),
                                            USERNAME = reader["USERNAME"].ToString(),
                                            Role = reader["ROLE"].ToString(),
                                            NAME = reader["NAME"].ToString(),
                                            SURNAME = reader["SURNAME"].ToString(),
                                            CLI_ID = reader["CLI_ID"].ToString()
                                        };
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptionLogger.LogException(
                                source: "GetUserById",
                                message: ex.Message,
                                stackTrace: ex.StackTrace
                            );
                            return null;
                        }
                        return null;
                    }
                [HttpGet("verifyToken")]
                [Authorize]
                public IActionResult VerifyToken()
                    {
                        try
                        {
                                var UserID = User?.FindFirst("USERID")?.Value;

                                if (string.IsNullOrEmpty(UserID))
                                {
                                    return Unauthorized(new { message = "Netinkamas sesijos id arba token" });
                                }
                                var user = GetUserById(UserID);
                                var newAccessToken = GenerateJwtToken(user.USERID,user.Role,user.CLI_ID);
                                var newRefreshToken = GenerateRefreshToken();
                                deleteOldRefreshTokens(user.USERID);
                                SaveRefreshToken(user.USERID, newRefreshToken);


                                

                                Response.Cookies.Append("ACCESS_TOKEN", newAccessToken, new CookieOptions
                                {
                                        HttpOnly = true,
                                        Secure = true,
                                        SameSite = SameSiteMode.Strict,
                                        Expires = DateTime.UtcNow.AddMinutes(15)
                                });
                                Response.Cookies.Append("REFRESH_TOKEN", newRefreshToken, new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Strict,
                                    Expires = DateTime.UtcNow.AddDays(7)
                                });
                                    return Ok(new { message = "Tokenas validus", isValid = true, role = user.Role, user.USERNAME,user.NAME, user.SURNAME, user.CLI_ID}); 
                                }
                            
                        
                        catch (Exception ex)
                        {
                            return StatusCode(500, new { message = "Nepavyko patvirtinti Tokeno", details = ex.Message });
                        }
                    }
                
                [HttpGet("getCli")]
                [Authorize]
                public IActionResult GetCli()
                {
                    var currentUserId = User?.FindFirst("USERID")?.Value;        
                    try 
                    {
                    using(var connection = _connectionProvider.GetConnection()){


                        connection.Open();
                        DataTable dt = new DataTable(); 
                        string query = "SELECT * FROM COMPANYS";
                        using(var command = new SqlCommand(query ,connection)){

                                using (var adapter = new SqlDataAdapter(command))
                                {
                                adapter.Fill(dt);
                                }
                                var jsonResult = _dataTableService.ConvertToJson(dt);
                                return Ok(jsonResult);
                        }
                    }
                    }catch(Exception ex ){
                    exceptionLogger.LogException(
                        source: "geCli",
                        message: ex.Message,
                        stackTrace: ex.StackTrace
                    );
                            return StatusCode(StatusCodes.Status500InternalServerError,
                            new { message = "Nepavyko užkrauti Componento" });
                    }
                }
                               
                [HttpPost("addCompany")]
                [Authorize]
                public IActionResult AddCompany([FromBody] AddCompanyRequest addCompanyRequest)
                {
                    var currentUserId = User?.FindFirst("USERID")?.Value;
                        if (!_adminService.IsTheAdmin(currentUserId))
                            {
                                return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
                            }
                        var CLI_ID = User?.FindFirst("CLI_ID")?.Value;

                        if (CLI_ID != "Admin")
                        {
                        return Unauthorized(new { message = "Jum neduotos teisės" });
                        }

                        try 
                    {
                        Guid newGuid = Guid.NewGuid();

                        using(var connection = _connectionProvider.GetConnection()){
                            connection.Open();
                            var query = @"
                                INSERT INTO COMPANYS (CLI_ID,CLI_NAME)
                                VALUES (@CLI_ID,@CLI_NAME)
                            ";
                            using (var command = new SqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@CLI_ID", newGuid );
                                command.Parameters.AddWithValue("@CLI_NAME", addCompanyRequest.COMPANY_NAME);
                                command.ExecuteNonQuery();
                            }
                            return Ok(new { message = "sėkmingai pridėtas"});
                        
                    }
                    }catch(Exception ex ){
                    exceptionLogger.LogException(
                        source: "addCompany",
                        message: ex.Message,
                        stackTrace: ex.StackTrace
                    );
                            return StatusCode(StatusCodes.Status500InternalServerError,
                            new { message = "Nepavyko sukurti" });
                    }
                }
                private AppUser AuthenticateUser(string userName, string password)
                    {   
                        try{
                        if (_adminService.AuthorizeAdmin(userName,password)){
                            return new AppUser{
                                        USERID = AdminConfig.AdminGuid.ToString(),
                                        USERNAME = "Admin",
                                        NAME = "Adminas",
                                        Role = "0",
                                        CLI_ID = "Admin"
                                    };
                        }
                        var hashedPassword = HashPassword(password);
                        
                        using (var connection = _connectionProvider.GetConnection())
                        {
                            connection.Open();
                            var query = @"  
                            SELECT USERID, USERNAME, ROLE, NAME, SURNAME,CLI_ID
                            FROM [USER] 
                            WHERE USERNAME COLLATE Latin1_General_CS_AS = @userName
                            AND Password COLLATE Latin1_General_CS_AS = @Password
                            ";
                            var command = new SqlCommand(query, connection);
                            command.Parameters.AddWithValue("@userName", userName);
                            command.Parameters.AddWithValue("@Password", hashedPassword);

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return new AppUser
                                    {
                                        USERID = reader["USERID"].ToString(),
                                        USERNAME = reader["USERNAME"].ToString(),
                                        Role = reader["ROLE"].ToString(),  
                                        NAME = reader["NAME"].ToString(),
                                        SURNAME = reader["SURNAME"].ToString(),
                                        CLI_ID = reader["CLI_ID"].ToString()  
                                    };
                                }
                            }
                        }
                        }catch(Exception ex){
                            exceptionLogger.LogException(
                            source: "Authenticate User",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                            );
                            return null;
                        }
                        return null;
                    }
                public static string GenerateJwtToken(string USERID, string Role, string CLI_ID)
                    {   
                        var secretManager = new SecretManager();
                        var jwtSecretKey = secretManager.GetJwtSecretCode();
                        // const string key = "Your32ByteSecureKeyWithExactLength!";
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                        var claims = new[]
                        {
                            new Claim("USERID", USERID), 
                            new Claim("ROLE", Role), 
                            new Claim("CLI_ID", CLI_ID), 
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                        };

                        var token = new JwtSecurityToken(
                            issuer: "CarCard",
                            audience: "CarCard",
                            claims: claims,
                            expires: DateTime.UtcNow.AddMinutes(15),
                            signingCredentials: credentials
                        );

                        return new JwtSecurityTokenHandler().WriteToken(token);
                    }
                private string HashPassword(string password)
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                        var hash = sha256.ComputeHash(bytes);
                        return Convert.ToBase64String(hash);
                    }
                }
                private string GenerateRefreshToken()
                {
                    var randomNumber = new byte[32];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(randomNumber);
                    }
                    return Convert.ToBase64String(randomNumber);
                }
                private void SaveRefreshToken(string UserID, string refreshToken)
                    {   
                        
                        try{
                        using (var connection = _connectionProvider.GetConnection())
                        {
                            connection.Open();
                            var command = new SqlCommand(
                                "INSERT INTO RefreshTokens (USERID, Token, ExpiryDate) VALUES (@USERID, @Token, @ExpiryDate)",
                                connection
                            );
                            command.Parameters.AddWithValue("@USERID", UserID);
                            command.Parameters.AddWithValue("@Token", refreshToken);
                            command.Parameters.AddWithValue("@ExpiryDate", DateTime.UtcNow.AddDays(7)); 
                            command.ExecuteNonQuery();
                        }
                        }catch(Exception ex){
                            exceptionLogger.LogException(
                            source: "SaveRefreshToken",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                            );
                        }
                    }                               
                private void deleteRefreshToken(string token){
                    try{
                    using (var connection = _connectionProvider.GetConnection())
                    {
                        connection.Open();
                        var commandQuery = "DELETE FROM RefreshTokens WHERE Token= @Token";
                        using(var command = new SqlCommand(commandQuery,connection)){
                            command.Parameters.AddWithValue("@Token",token);
                            command.ExecuteNonQuery();
                        }
                    }
                    }catch(Exception ex){
                            exceptionLogger.LogException(
                            source: "deleteRefreshToken",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                            );
                    }
                }
                private void deleteOldRefreshTokens(string UserID){
                    try{
                    using (var connection = _connectionProvider.GetConnection())
                    {
                        connection.Open();
                        var commandQuery = "DELETE FROM RefreshTokens WHERE USERID =  @USERID";
                        using(var command = new SqlCommand(commandQuery,connection)){
                            command.Parameters.AddWithValue("@USERID",UserID);
                            command.ExecuteNonQuery();
                        }
                    }
                    }catch(Exception ex){
                            exceptionLogger.LogException(
                            source: "deleteOldRefreshTokens",
                            message: ex.Message,
                            stackTrace: ex.StackTrace
                            );
                    }
                }

    
                
    
    
    public class LoginRequest
    {
        public string USERNAME { get; set; }
        public string PASSWORD { get; set; }
    }
    public class AppUser
    {
        public string USERID { get; set; }
        public string USERNAME { get; set; }  
        public string PASSWORD { get; set; }
        public string NAME { get; set; }
        public string SURNAME { get; set; }
        public string  Role { get; set; }
        public string  CLI_ID { get; set; }
    }
    public class DTOAddUser
    {
        public string USERNAME { get; set; }  
        public string PASSWORD { get; set; }
        public string NAME { get; set; }
        public string SURNAME { get; set; }
        public string CLI_ID { get; set; }

    }
     public class DTODeleteUser
    {
        public string USER_ID { get; set; }

    }
    public class AppUserFULL
    {
        public string USERID { get; set; }
        public string USERNAME { get; set; }  
        public string  ROLE { get; set; }
        public string NAME { get; set; }
        public string SURNAME { get; set; }
        public string CAR_NUMBER { get; set; }
        public string CARD_NUMBER { get; set; }

    }
    public class UpdateUserRequest
    {
    public string USERID { get; set; }
    public string USERNAME { get; set; }
    public string ROLE { get; set; }
    public string NAME { get; set; }
    public string SURNAME { get; set; }
    public int? CAR_ID { get; set; } 

    }
    public class UpdateUserCarToNullRequest
    {
    public string USERID { get; set; }

    }
    public class AddCompanyRequest
    {
        public string COMPANY_NAME { get; set; }
    }
}

}


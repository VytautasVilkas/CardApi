
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.HttpResults;


namespace carCard.Controllers
{
[ApiController]
[Route("api/[controller]")]
public class CardController : ControllerBase
{



    private readonly IAdminService _adminService;
    private readonly ConnectionProvider _connectionProvider;
    private readonly DataTableService _dataTableService;
    private ExceptionLogger exceptionLogger;
    public CardController(ConnectionProvider connectionProvider, DataTableService dataTableService,IAdminService adminService)
    {
        _adminService = adminService;
        _connectionProvider = connectionProvider;
        _dataTableService = dataTableService;
        exceptionLogger = new ExceptionLogger(_connectionProvider);
    }


        [HttpPost("addcard")]
        [Authorize]
        public IActionResult addCard([FromBody] AddCard card)
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

                    // Check if a card with the given number already exists.
                    var checkQuery = "SELECT COUNT(*) FROM CARDS WHERE FCA_NUMBER = @Number AND FCA_VALID = 1";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Number", card.cardNumber);
                        int count = (int)checkCommand.ExecuteScalar();
                        if (count > 0)
                        {
                            return BadRequest(new { message = "Kortelė tokiu numeriu jau sukurta" });
                        }
                    }

                    // Insert the new card since it does not exist.
                    var query = @"
                        INSERT INTO CARDS (FCA_NUMBER, FCA_FUEL_TYPE, FCA_VALID_UNTIL, FAC_ADDITIONALINFO,FCA_CLI_ID,FCA_DATE,FCA_VALID) 
                        VALUES (@Number, @FuelType, @ValidUntil, @Info,@FCA_CLI_ID,@FCA_DATE,@FCA_VALID)
                    ";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Number", card.cardNumber);
                        command.Parameters.AddWithValue("@FuelType", card.fuelType);
                        command.Parameters.AddWithValue("@ValidUntil", card.expirationDate);
                        command.Parameters.AddWithValue("@Info", card.additionalInfo);
                        command.Parameters.AddWithValue("@FCA_CLI_ID", card.CLI_ID);
                        command.Parameters.AddWithValue("@FCA_DATE", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@FCA_VALID", 1);
                        command.ExecuteNonQuery();
                    }

                    return Ok(new { message = "Sėkmingai pridėtas" });
                }
            }
            catch (Exception ex)
            {
                exceptionLogger.LogException(
                    source: "addCard",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Nepavyko sukurti. Bandykite vėliau" });
            }
        }

        // GET: api/Card/getCards
        [HttpGet("getNotConnectedCards")]
        [Authorize]
        public IActionResult getCards([FromQuery] string CLI_ID)
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
                                SELECT c.FCA_ID, c.FCA_NUMBER 
                                FROM CARDS c
                                WHERE FCA_CLI_ID = @CLI_ID AND FCA_VALID = 1 AND NOT EXISTS (
                                    SELECT 1 
                                    FROM CARS 
                                    WHERE CAR_FCA_ID = c.FCA_ID
                                );
                            ";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CLI_ID", CLI_ID);
                        using (var reader = command.ExecuteReader())
                        {
                            var cards = new List<CardDto>();
                            while (reader.Read())
                            {
                                var cardDto = new CardDto
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("FCA_ID")),
                                    Number = reader.GetString(reader.GetOrdinal("FCA_NUMBER"))
                                };
                                cards.Add(cardDto);
                            }
                            return Ok(cards);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                exceptionLogger.LogException(
                    source: "getCards",
                    message: ex.Message,
                    stackTrace: ex.StackTrace
                );
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Nepavyko gauti kortelių. Bandykite dar kartą." });
            }
        }
    
    
    
    
            [HttpGet("getCards")]
            [Authorize]
            public IActionResult getAllCards([FromQuery] string CLI_ID, [FromQuery] string search = "")
            {
                var currentUserId = User?.FindFirst("USERID")?.Value;
                if (!_adminService.IsAdmin(currentUserId))
                {
                    return Unauthorized(new { message = "Jūs neturite teisių atlikti šią operaciją." });
                }

                try
                {
                    DataTable dt = new DataTable();
                    using (var connection = _connectionProvider.GetConnection())
                    {
                        connection.Open();
                        var query = @"
                            SELECT 
                                c.FCA_ID, 
                                c.FCA_NUMBER, 
                                c.FAC_ADDITIONALINFO, 
                                c.FCA_VALID_UNTIL, 
                                c.FCA_FUEL_TYPE
                            FROM CARDS c
                            WHERE FCA_VALID = 1 AND FCA_CLI_ID = @CLI_ID";
                        
                        // Trim and check the search parameter
                        var trimmedSearch = search?.Trim();
                        if (!string.IsNullOrEmpty(trimmedSearch))
                        {
                            query += " AND c.FCA_NUMBER LIKE '%' + @search + '%'";
                        }
                        using (var command = new SqlCommand(query, connection))
                        {
                            // If CLI_ID is a GUID in the database, you might want to parse it:
                            if (Guid.TryParse(CLI_ID, out Guid parsedCliId))
                            {
                                command.Parameters.AddWithValue("@CLI_ID", parsedCliId);
                            }
                            else
                            {
                                command.Parameters.AddWithValue("@CLI_ID", CLI_ID);
                            }
                            if (!string.IsNullOrEmpty(trimmedSearch))
                            {
                                command.Parameters.AddWithValue("@search", trimmedSearch);
                            }
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                adapter.Fill(dt);
                            }
                        }
                    }

                    var jsonResult = _dataTableService.ConvertToJson(dt);
                    return Ok(jsonResult);
                }
                catch (Exception ex)
                {
                    exceptionLogger.LogException(
                        source: "getCards",
                        message: ex.Message,
                        stackTrace: ex.StackTrace
                    );
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Nepavyko gauti kortelių. Bandykite dar kartą." });
                }
            }


        [HttpPost("updateCard")]
        [Authorize]
        public IActionResult updateCard([FromBody] UpdateCard card)
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
                    var checkQuery = "SELECT COUNT(*) FROM CARDS WHERE FCA_NUMBER = @NewNumber AND FCA_ID <> @CardId";
                    using (var checkCommand = new SqlCommand(checkQuery, connection, transaction))
                    {
                        checkCommand.Parameters.AddWithValue("@NewNumber", card.FCA_NUMBER);
                        checkCommand.Parameters.AddWithValue("@CardId", card.FCA_ID);
                        int count = (int)checkCommand.ExecuteScalar();
                        if (count > 0)
                        {
                            transaction.Rollback();
                            return BadRequest(new { message = "Kortelė tokiu numeriu jau sukurta." });
                        }
                    }
                    string oldCardNumber = "";
                    var getOldQuery = "SELECT FCA_NUMBER FROM CARDS WHERE FCA_ID = @CardId";
                    using (var getOldCommand = new SqlCommand(getOldQuery, connection, transaction))
                    {
                        getOldCommand.Parameters.AddWithValue("@CardId", card.FCA_ID);
                        var result = getOldCommand.ExecuteScalar();
                        if (result != null)
                        {
                            oldCardNumber = result.ToString();
                        }
                    }
                    var updateQuery = @"
                        UPDATE CARDS
                        SET FCA_NUMBER = @NewNumber,
                            FCA_FUEL_TYPE = @FuelType,
                            FCA_VALID_UNTIL = @ValidUntil,
                            FAC_ADDITIONALINFO = @AdditionalInfo
                        WHERE FCA_ID = @CardId";
                    using (var updateCommand = new SqlCommand(updateQuery, connection, transaction))
                    {
                        updateCommand.Parameters.AddWithValue("@NewNumber", card.FCA_NUMBER);
                        updateCommand.Parameters.AddWithValue("@FuelType", card.FCA_FUEL_TYPE);
                        updateCommand.Parameters.AddWithValue("@ValidUntil", card.FCA_VALID_UNTIL); 
                        updateCommand.Parameters.AddWithValue("@AdditionalInfo", card.FAC_ADDITIONALINFO);
                        updateCommand.Parameters.AddWithValue("@CardId", card.FCA_ID); 
                        updateCommand.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
            return Ok(new { message = "Kortelė sėkmingai atnaujinta" });
        }
        catch (Exception ex)
        {
            exceptionLogger.LogException(
                source: "updateCard",
                message: ex.Message,
                stackTrace: ex.StackTrace
            );
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Nepavyko atnaujinti kortelės. Bandykite dar kartą." });
        }
        }

            [HttpPost("deleteCard")]
            [Authorize]
            public IActionResult deleteCard([FromBody] deleteCard card)
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
                            string deleteCarsQuery = "DELETE FROM CARS WHERE CAR_FCA_ID = @FCA_ID";
                            using (var deleteCarsCommand = new SqlCommand(deleteCarsQuery, connection, transaction))
                            {
                                deleteCarsCommand.Parameters.AddWithValue("@FCA_ID", card.FCA_ID);
                                deleteCarsCommand.ExecuteNonQuery();
                            }
                            string deleteCardsQuery = "UPDATE CARDS SET FCA_VALID = 0  WHERE FCA_ID = @FCA_ID";
                            using (var deleteCardsCommand = new SqlCommand(deleteCardsQuery, connection, transaction))
                            {
                                deleteCardsCommand.Parameters.AddWithValue("@FCA_ID", card.FCA_ID);
                                int rowsAffected = deleteCardsCommand.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    transaction.Rollback();
                                    return BadRequest(new { message = "Kortelė nerasta." });
                                }
                            }

                            transaction.Commit();
                        }
                    }

                    return Ok(new { message = "Kortelė sėkmingai ištrinta" });
                }
                catch (Exception ex)
                {
                    exceptionLogger.LogException(
                        source: "deleteCard",
                        message: ex.Message,
                        stackTrace: ex.StackTrace
                    );
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Nepavyko ištrinti kortelės. Bandykite dar kartą." });
                }
            }










        }
    
    
    
    
    
    
    
    






    
    public class AddCard
    {
        public string cardNumber { get; set; }
        public int fuelType { get; set; }
        public string additionalInfo { get; set; }
        public string expirationDate { get; set; }
        public string CLI_ID { get; set; }
    }
    public class UpdateCard
    {
        public int FCA_ID { get; set; }
        public string FCA_NUMBER { get; set; }
        public int FCA_FUEL_TYPE { get; set; }
        public string FAC_ADDITIONALINFO { get; set; }
        public DateTime FCA_VALID_UNTIL { get; set; }
    }

    public class CardDto
    {
        public int Id { get; set; }
        public string Number { get; set; }
    }
   public class deleteCard
   
   {
    public int FCA_ID {get;set;}




   }

 }

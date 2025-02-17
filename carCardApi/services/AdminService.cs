using System;
using System.Security.Claims;
using carCard;
using Microsoft.Data.SqlClient;

public interface IAdminService
{   
    bool IsTheAdmin(string userID);
    bool IsAdmin(string userID);
    bool AuthorizeAdmin(string Username, string Password);
}

public class AdminService : IAdminService
{   
    private ExceptionLogger _exceptionLogger;
    private readonly SecretManager _secretManager;
    private readonly ConnectionProvider _connectionProvider;


    public AdminService(SecretManager secretManager, ConnectionProvider connectionProvider) {
        _secretManager = secretManager;
        _connectionProvider = connectionProvider;
        _exceptionLogger = new ExceptionLogger(_connectionProvider);

    }
    public bool IsAdmin(string userID)
    {

    var adminGuid = AdminConfig.AdminGuid;
    string dbRole = "";
    try
    {
        using (var connection = _connectionProvider.GetConnection())
        {
            connection.Open();
            var query = "SELECT ROLE FROM [USER] WHERE USERID = @USERID";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@USERID", userID);
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    dbRole = result.ToString();
                }
            }
        }
    }
    catch (Exception ex)
    {
        _exceptionLogger.LogException(
            source: "IsAdmin",
            message: ex.Message,
            stackTrace: ex.StackTrace
        );
        return false;
    }
    if (!string.IsNullOrEmpty(dbRole) && dbRole == "1")
    {
        return true;
    }
    return !string.IsNullOrEmpty(userID) &&
           !string.IsNullOrEmpty(adminGuid) &&
           userID.Equals(adminGuid, StringComparison.OrdinalIgnoreCase);
    }
    public bool IsTheAdmin(string userID)
    {
    var adminGuid = AdminConfig.AdminGuid;
    return !string.IsNullOrEmpty(userID) &&
           !string.IsNullOrEmpty(adminGuid) &&
           userID.Equals(adminGuid, StringComparison.OrdinalIgnoreCase);
    }
    public bool AuthorizeAdmin(string Username, string Password){
        if (Username == "Admin" && Password == _secretManager.GetAdminSecretCode())
        {return true;}else{return false;}
    }
    }


public static class AdminConfig
{
    public static readonly string AdminGuid = Guid.NewGuid().ToString();
}

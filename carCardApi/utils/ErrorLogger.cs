using System;
using System.Data;
using Microsoft.Data.SqlClient;

public class ExceptionLogger
{
    private readonly carCard.ConnectionProvider _connectionProvider;

    public ExceptionLogger(carCard.ConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }
    public void LogException( string source, string message, string stackTrace)
    {
        try
        {
            using (var connection = _connectionProvider.GetConnection())
            {
                connection.Open();

                var query = @"
                    INSERT INTO ErrorLogger (Source, Message, StackTrace, LoggedAt) 
                    VALUES (@Source, @Message, @StackTrace, @LoggedAt)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Source", source);
                    command.Parameters.AddWithValue("@Message", message);
                    command.Parameters.AddWithValue("@StackTrace", stackTrace);
                    command.Parameters.AddWithValue("@LoggedAt", DateTime.UtcNow);

                    command.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to log exception to the database: {ex.Message}");
        }
    }
}

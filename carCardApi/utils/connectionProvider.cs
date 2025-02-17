using Microsoft.Data.SqlClient;

namespace carCard
{
    public class ConnectionProvider
    {
        private readonly string _connectionString;

        public ConnectionProvider(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LocalConnection")
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

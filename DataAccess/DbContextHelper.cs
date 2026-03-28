using Microsoft.Data.SqlClient;

namespace PSA.DataAccess
{
    public class DbContextHelper
    {
        private readonly string _connectionString;

        public DbContextHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool TestConnection()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                return connection.State == System.Data.ConnectionState.Open;
            }
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
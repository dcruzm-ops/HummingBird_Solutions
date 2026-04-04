using System.Data;

namespace PSA.DataAccess
{
    public class DbContextHelper
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DbContextHelper(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool TestConnection()
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            return connection.State == ConnectionState.Open;
        }
    }
}

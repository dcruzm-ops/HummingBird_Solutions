using Microsoft.Data.SqlClient;

namespace PSA.DataAccess;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}

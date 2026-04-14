using Microsoft.Data.SqlClient;

namespace PSA.DataAccess.DAO
{
    public class LandingDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public LandingDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<(string Titulo, string Descripcion)> ObtenerContenidoEquipoAsync()
        {
            return await EjecutarConsultaLandingAsync("dbo.SP_Landing_ObtenerEquipo");
        }

        public async Task<(string Titulo, string Descripcion)> ObtenerContenidoProductoAsync()
        {
            return await EjecutarConsultaLandingAsync("dbo.SP_Landing_ObtenerProducto");
        }

        private async Task<(string Titulo, string Descripcion)> EjecutarConsultaLandingAsync(string nombreSp)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(nombreSp, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return (string.Empty, string.Empty);
            }

            return (
                reader["Titulo"]?.ToString() ?? string.Empty,
                reader["Descripcion"]?.ToString() ?? string.Empty
            );
        }
    }
}

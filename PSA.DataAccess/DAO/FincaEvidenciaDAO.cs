using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Fincas;
using PSA.EntidadesDTO.Entidades.Fincas;

using PSA.DataAccess;

namespace PSA.DataAccess.DAO
{
    public class FincaEvidenciaDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public FincaEvidenciaDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CrearAsync(FincaEvidencia evidencia)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand("dbo.SP_FincaEvidencias_Crear", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@IdFinca", evidencia.FincaId);
            command.Parameters.AddWithValue("@NombreArchivo", evidencia.NombreArchivo);
            command.Parameters.AddWithValue("@RutaArchivo", evidencia.RutaArchivo);
            command.Parameters.AddWithValue("@TipoArchivo", evidencia.TipoArchivo);
            command.Parameters.AddWithValue("@CargadoPor", evidencia.CargadoPor);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<List<FincaEvidenciaDTO>> ObtenerPorFincaAsync(int idFinca)
        {
            const string sql = @"
SELECT
    IdEvidencia,
    IdFinca,
    NombreArchivo,
    RutaArchivo,
    TipoArchivo,
    FechaCarga,
    CargadoPor
FROM FincaEvidencias
WHERE IdFinca = @IdFinca
ORDER BY FechaCarga DESC;";

            var lista = new List<FincaEvidenciaDTO>();

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdFinca", idFinca);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new FincaEvidenciaDTO
                {
                    IdEvidencia = Convert.ToInt32(reader["IdEvidencia"]),
                    FincaId = Convert.ToInt32(reader["IdFinca"]),
                    NombreArchivo = reader["NombreArchivo"]?.ToString() ?? string.Empty,
                    RutaArchivo = reader["RutaArchivo"]?.ToString() ?? string.Empty,
                    TipoArchivo = reader["TipoArchivo"]?.ToString() ?? string.Empty,
                    FechaCarga = Convert.ToDateTime(reader["FechaCarga"]),
                    CargadoPor = Convert.ToInt32(reader["CargadoPor"])
                });
            }

            return lista;
        }

        public async Task<FincaEvidenciaDTO?> ObtenerPorIdAsync(int idEvidencia)
        {
            const string sql = @"
SELECT TOP 1
    IdEvidencia,
    IdFinca,
    NombreArchivo,
    RutaArchivo,
    TipoArchivo,
    FechaCarga,
    CargadoPor
FROM FincaEvidencias
WHERE IdEvidencia = @IdEvidencia;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEvidencia", idEvidencia);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new FincaEvidenciaDTO
            {
                IdEvidencia = Convert.ToInt32(reader["IdEvidencia"]),
                FincaId = Convert.ToInt32(reader["IdFinca"]),
                NombreArchivo = reader["NombreArchivo"]?.ToString() ?? string.Empty,
                RutaArchivo = reader["RutaArchivo"]?.ToString() ?? string.Empty,
                TipoArchivo = reader["TipoArchivo"]?.ToString() ?? string.Empty,
                FechaCarga = Convert.ToDateTime(reader["FechaCarga"]),
                CargadoPor = Convert.ToInt32(reader["CargadoPor"])
            };
        }
    }
}

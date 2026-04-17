using PSA.DataAccess.BaseDatos;
using PSA.EntidadesDTO.DTOs.Administracion;
using Microsoft.Data.SqlClient;
using System.Data;

namespace PSA.DataAccess.DAO
{
    public class RolPermisoDAO
    {
        private readonly DbContextHelper _dbContext;

        public RolPermisoDAO(DbContextHelper dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<RolPermisoDTO>> ObtenerRolesConPermisosAsync()
        {
            using var conn = _dbContext.CrearConexion();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "SP_OBTENER_ROLES_CON_PERMISOS";

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var roles = new Dictionary<int, RolPermisoDTO>();
            var permisosDisponibles = new List<PermisoDTO>();

            while (await reader.ReadAsync())
            {
                if (permisosDisponibles.Count == 0 && !reader.IsDBNull(reader.GetOrdinal("IdPermisoDisponible")))
                {
                    permisosDisponibles.Add(new PermisoDTO
                    {
                        IdPermiso = reader.GetInt32(reader.GetOrdinal("IdPermisoDisponible")),
                        NombrePermiso = reader.GetString(reader.GetOrdinal("NombrePermisoDisponible")),
                        Descripcion = reader.IsDBNull(reader.GetOrdinal("DescripcionPermisoDisponible")) ? null : reader.GetString(reader.GetOrdinal("DescripcionPermisoDisponible")),
                        Modulo = reader.IsDBNull(reader.GetOrdinal("ModuloPermisoDisponible")) ? null : reader.GetString(reader.GetOrdinal("ModuloPermisoDisponible"))
                    });
                }

                var idRol = reader.GetInt32(reader.GetOrdinal("IdRol"));
                if (!roles.TryGetValue(idRol, out var rol))
                {
                    rol = new RolPermisoDTO
                    {
                        IdRol = idRol,
                        NombreRol = reader.GetString(reader.GetOrdinal("NombreRol")),
                        Descripcion = reader.IsDBNull(reader.GetOrdinal("DescripcionRol")) ? null : reader.GetString(reader.GetOrdinal("DescripcionRol")),
                        PermisosAsignados = new List<PermisoDTO>(),
                        PermisosDisponibles = new List<PermisoDTO>()
                    };
                    roles[idRol] = rol;
                }

                if (!reader.IsDBNull(reader.GetOrdinal("IdPermisoAsignado")))
                {
                    rol.PermisosAsignados.Add(new PermisoDTO
                    {
                        IdPermiso = reader.GetInt32(reader.GetOrdinal("IdPermisoAsignado")),
                        NombrePermiso = reader.GetString(reader.GetOrdinal("NombrePermisoAsignado")),
                        Descripcion = reader.IsDBNull(reader.GetOrdinal("DescripcionPermisoAsignado")) ? null : reader.GetString(reader.GetOrdinal("DescripcionPermisoAsignado")),
                        Modulo = reader.IsDBNull(reader.GetOrdinal("ModuloPermisoAsignado")) ? null : reader.GetString(reader.GetOrdinal("ModuloPermisoAsignado"))
                    });
                }
            }

            foreach (var rol in roles.Values)
            {
                rol.PermisosDisponibles = permisosDisponibles
                    .GroupBy(p => p.IdPermiso)
                    .Select(g => g.First())
                    .OrderBy(p => p.Modulo)
                    .ThenBy(p => p.NombrePermiso)
                    .ToList();

                rol.PermisosAsignados = rol.PermisosAsignados
                    .GroupBy(p => p.IdPermiso)
                    .Select(g => g.First())
                    .OrderBy(p => p.Modulo)
                    .ThenBy(p => p.NombrePermiso)
                    .ToList();
            }

            return roles.Values.OrderBy(r => r.NombreRol).ToList();
        }

        public async Task GuardarPermisosRolAsync(GuardarPermisosRolDTO dto)
        {
            using var conn = _dbContext.CrearConexion();
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();
            try
            {
                var cmdDelete = conn.CreateCommand();
                cmdDelete.Transaction = tx;
                cmdDelete.CommandType = CommandType.StoredProcedure;
                cmdDelete.CommandText = "SP_ELIMINAR_PERMISOS_DE_ROL";
                cmdDelete.Parameters.AddWithValue("@IdRol", dto.IdRol);
                await cmdDelete.ExecuteNonQueryAsync();

                if (dto.IdsPermisos != null)
                {
                    foreach (var idPermiso in dto.IdsPermisos.Distinct())
                    {
                        var cmdInsert = conn.CreateCommand();
                        cmdInsert.Transaction = tx;
                        cmdInsert.CommandType = CommandType.StoredProcedure;
                        cmdInsert.CommandText = "SP_ASIGNAR_PERMISO_A_ROL";
                        cmdInsert.Parameters.AddWithValue("@IdRol", dto.IdRol);
                        cmdInsert.Parameters.AddWithValue("@IdPermiso", idPermiso);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}

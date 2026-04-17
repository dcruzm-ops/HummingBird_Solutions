using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PSA.EntidadesDTO.DTOs.Administracion
{
    public class AuditoriaEventoDTO
    {
        public int IdLog { get; set; }
        public int? IdUsuario { get; set; }
        public string? NombreUsuario { get; set; }
        public string Modulo { get; set; } = string.Empty;
        public string TablaAfectada { get; set; } = string.Empty;
        public int? IdRegistroAfectado { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? IpOrigen { get; set; }
        public DateTime FechaAccion { get; set; }
        public string? ValorAnterior { get; set; }
        public string? ValorNuevo { get; set; }
    }

    public class AuditoriaFiltroDTO
    {
        public string? Modulo { get; set; }
        public string? Accion { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int MaximoRegistros { get; set; } = 50;
    }

    public class AuditoriaOpcionesFiltroDTO
    {
        public List<string> Modulos { get; set; } = new();
        public List<string> Acciones { get; set; } = new();
    }

    public class ConfiguracionPagoAdminDTO
    {
        public int IdConfiguracionPago { get; set; }
        public int Version { get; set; }

        [Required(ErrorMessage = "El nombre de la versión es obligatorio.")]
        public string NombreVersion { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "El precio base no puede ser negativo.")]
        public decimal PrecioBasePorHectarea { get; set; }

        [Range(0, 100, ErrorMessage = "El tope de ajuste debe estar entre 0 y 100.")]
        public decimal TopePorcentajeAjuste { get; set; }

        [Required(ErrorMessage = "La fecha de vigencia inicial es obligatoria.")]
        public DateTime FechaVigenciaDesde { get; set; } = DateTime.Today;

        public DateTime? FechaVigenciaHasta { get; set; }
        public bool Activa { get; set; }
        public int CreadoPor { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<ConfiguracionPagoAjusteDTO> Ajustes { get; set; } = new();
    }

    public class ConfiguracionPagoAjusteDTO
    {
        public int IdDetalleConfiguracion { get; set; }
        public string TipoFactor { get; set; } = string.Empty;
        public string ValorFactor { get; set; } = string.Empty;

        [Range(-100, 100, ErrorMessage = "El porcentaje debe estar entre -100 y 100.")]
        public decimal PorcentajeAjuste { get; set; }
    }

    public class CuentaBancariaPendienteDTO
    {
        public int IdCuentaBancaria { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;
        public string Banco { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
        public string EstadoValidacion { get; set; } = string.Empty;
        public string? ObservacionesValidacion { get; set; }
        public bool Activa { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class GuardarPermisosRolDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un rol válido.")]
        public int IdRol { get; set; }

        public List<string> CodigosPermiso { get; set; } = new();
    }

    public class CrearRolDTO
    {
        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class PermisoDTO
    {
        public int IdPermiso { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class ReasignacionClienteDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un propietario válido.")]
        public int IdPropietario { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un asesor válido.")]
        public int IdIngenieroDestino { get; set; }
    }

    public class RolPermisoDTO
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string? DescripcionRol { get; set; }
        public bool Activo { get; set; }
        public List<string> CodigosPermisoAsignados { get; set; } = new();
        public List<PermisoDTO> PermisosDisponibles { get; set; } = new();
    }

    public class UsuarioAdminEdicionDTO
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un rol válido.")]
        public int IdRol { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        public string Estado { get; set; } = "Activo";

        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string? Contrasena { get; set; }

        [Compare("Contrasena", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string? ConfirmacionContrasena { get; set; }
    }

    public class UsuarioAdminListadoDTO
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public int CantidadFincas { get; set; }
        public int CantidadEvaluacionesActivas { get; set; }
    }

    public class ValidacionCuentaBancariaDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una cuenta válida.")]
        public int IdCuentaBancaria { get; set; }

        public bool Aprobada { get; set; }
        public string? Observaciones { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe indicar el administrador responsable.")]
        public int IdAdministrador { get; set; }
    }
}

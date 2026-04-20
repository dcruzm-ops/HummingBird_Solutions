using Microsoft.AspNetCore.Authorization;

namespace PSA.WebAPI.Services.Security;

public static class AppPermissions
{
    public const string AdminUsuariosVer = "ADMIN_USUARIOS_VER";
    public const string AdminUsuariosCrear = "ADMIN_USUARIOS_CREAR";
    public const string AdminUsuariosEditar = "ADMIN_USUARIOS_EDITAR";
    public const string AdminUsuariosEliminar = "ADMIN_USUARIOS_ELIMINAR";
    public const string AdminPagosConfigurar = "ADMIN_PAGOS_CONFIGURAR";
    public const string AdminCuentasValidar = "ADMIN_CUENTAS_VALIDAR";
    public const string AdminAuditoriaConsultar = "ADMIN_AUDITORIA_CONSULTAR";
    public const string AdminReportes = "ADMIN_REPORTES_CONSULTAR";
    public const string IngenieroAprobarPlan = "ING_PLAN_APROBAR";
    public const string PropietarioRenovarFinca = "DUENO_FINCAS_RENOVAR";
}

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var granted = context.User.Claims
            .Where(c => c.Type == "perm")
            .Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PSA.WebApp.Models;

namespace PSA.WebApp.Controllers;

public abstract class AppControllerBase : Controller
{
    private static readonly Dictionary<string, (string Label, string Action)> Modulos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Administracion"] = ("Administración", "GestionUsuarios"),
        ["Fincas"] = ("Fincas", "MisFincas"),
        ["Evaluaciones"] = ("Evaluaciones", "Pendientes"),
        ["Pagos"] = ("Pagos", "PlanesPago"),
        ["Reportes"] = ("Reportes", "Index"),
        ["MiPerfil"] = ("Mi perfil", "Index"),
        ["Notificaciones"] = ("Notificaciones", "Index")
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        if (!(User?.Identity?.IsAuthenticated ?? false))
        {
            return;
        }

        var controller = (context.RouteData.Values["controller"]?.ToString() ?? string.Empty).Trim();
        var action = (context.RouteData.Values["action"]?.ToString() ?? string.Empty).Trim();

        var titulo = ViewBag.TituloPagina as string
            ?? ViewBag.BreadcrumbActual as string
            ?? ViewData["Title"]?.ToString()
            ?? Humanizar(action);

        ViewBag.TituloPagina = titulo;

        var inicioAccion = ViewBag.BreadcrumbInicioAccion as string ?? ResolverInicioPorRol();
        var inicioControlador = ViewBag.BreadcrumbInicioControlador as string ?? "Dashboard";
        var inicioTexto = ViewBag.BreadcrumbInicioTexto as string ?? "Inicio";

        var breadcrumbs = new List<BreadcrumbItemViewModel>
        {
            new() { Label = inicioTexto, Url = Url.Action(inicioAccion, inicioControlador) }
        };

        var padreTexto = (ViewBag.BreadcrumbPadreTexto as string)?.Trim();
        var padreUrl = (ViewBag.BreadcrumbPadreUrl as string)?.Trim();

        if (!string.IsNullOrWhiteSpace(padreTexto))
        {
            breadcrumbs.Add(new BreadcrumbItemViewModel { Label = padreTexto, Url = padreUrl });
        }
        else if (Modulos.TryGetValue(controller, out var modulo) && !controller.Equals("Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            breadcrumbs.Add(new BreadcrumbItemViewModel
            {
                Label = modulo.Label,
                Url = Url.Action(modulo.Action, controller)
            });
        }

        breadcrumbs.Add(new BreadcrumbItemViewModel
        {
            Label = (ViewBag.BreadcrumbActual as string ?? titulo).Trim(),
            IsCurrent = true
        });

        ViewBag.BreadcrumbItems = breadcrumbs;
    }

    private string ResolverInicioPorRol()
    {
        var rol = User?.Claims.FirstOrDefault(c => c.Type.EndsWith("role", StringComparison.OrdinalIgnoreCase))?.Value;
        return rol switch
        {
            "1" => "Administrador",
            "3" => "Ingeniero",
            _ => "Dueno"
        };
    }

    private static string Humanizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "Página";
        }

        return string.Concat(texto.Select((c, i) => i > 0 && char.IsUpper(c) ? $" {c}" : c.ToString())).Trim();
    }
}

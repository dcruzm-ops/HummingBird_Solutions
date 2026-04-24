using System.Text.RegularExpressions;
using PSA.AppCore;
using PSA.AppCore.Managers;
using PSA.AppCore.Services.Security;
using PSA.AppCore.Services.Notifications;
using PSA.AppCore.Servicios;
using PSA.DataAccess;
using PSA.DataAccess.DAO;
using PSA.WebAPI.Controllers.Middleware;
using PSA.WebAPI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using PSA.WebAPI.Services.Security;
using System.Text;

SanitizarAppSettingsSiEsNecesario();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = ObtenerCorsOrigins(builder.Configuration);

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddScoped<IServicioHashContrasena, ServicioHashContrasena>();
builder.Services.AddScoped<IPasswordPolicy, PasswordPolicy>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"];
    var jwtConfigurado = !string.IsNullOrWhiteSpace(jwtKey) && !jwtKey.Contains("set-via", StringComparison.OrdinalIgnoreCase);
    if (!jwtConfigurado)
    {
        // Evita que toda la app se caiga al arrancar; la emisión de token falla de forma controlada en login.
        jwtKey = "development-placeholder-key-not-for-production";
    }

    var keyBytes = Encoding.UTF8.GetBytes(jwtKey ?? string.Empty);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PSA.WebAPI",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PSA.WebApp",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPermissions.AdminUsuariosVer, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.AdminUsuariosVer)));
    options.AddPolicy(AppPermissions.AdminUsuariosCrear, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.AdminUsuariosCrear)));
    options.AddPolicy(AppPermissions.AdminUsuariosEditar, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.AdminUsuariosEditar)));
    options.AddPolicy(AppPermissions.AdminUsuariosEliminar, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.AdminUsuariosEliminar)));
    options.AddPolicy(AppPermissions.AdminPagosConfigurar, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.AdminPagosConfigurar)));
    options.AddPolicy(AppPermissions.AdminCuentasValidar, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.AdminCuentasValidar)));
    options.AddPolicy(AppPermissions.AdminAuditoriaConsultar, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.AdminAuditoriaConsultar)));
    options.AddPolicy(AppPermissions.AdminReportes, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.AdminReportes)));
    options.AddPolicy(AppPermissions.IngenieroAprobarPlan, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.IngenieroAprobarPlan)));
    options.AddPolicy(AppPermissions.PropietarioRenovarFinca, p => p.Requirements.Add(new PermissionRequirement(AppPermissions.PropietarioRenovarFinca)));
});



builder.Services.AddScoped<IDbConnectionFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection'.");
    }

    return new SqlConnectionFactory(connectionString);
});

builder.Services.AddScoped<DbContextHelper>();
builder.Services.AddScoped<UsuarioDAO>();
builder.Services.AddScoped<FincaDAO>();
builder.Services.AddScoped<EvaluacionTecnicaDAO>();
builder.Services.AddScoped<RecuperacionContrasenaDAO>();
builder.Services.AddScoped<TokenRecuperacionDAO>();
builder.Services.AddScoped<FincaEvidenciaDAO>();
builder.Services.AddScoped<EvaluacionDAO>();
builder.Services.AddScoped<EvaluacionEvidenciaDAO>();
builder.Services.AddScoped<AuditoriaLogDAO>();
builder.Services.AddScoped<RolPermisoDAO>();
builder.Services.AddScoped<ConfiguracionPagoDAO>();
builder.Services.AddScoped<PlanPagoDAO>();
builder.Services.AddScoped<CuentaBancariaDAO>();
builder.Services.AddScoped<DashboardDAO>();
builder.Services.AddScoped<ReportesDAO>();
builder.Services.AddScoped<LandingDAO>();
builder.Services.AddScoped<NotificacionDAO>();

builder.Services.AddScoped<PSA.AppCore.Services.IPaymentCalculationService, PSA.AppCore.Services.PaymentCalculationService>();
builder.Services.AddScoped<PSA.AppCore.Services.IPaymentPlanService, PSA.AppCore.Services.PaymentPlanService>();
builder.Services.AddScoped<PSA.AppCore.Services.IPaymentPlanReadService, PSA.AppCore.Services.PaymentPlanReadService>();
builder.Services.AddScoped<FincaService>();
builder.Services.AddScoped<EvaluacionService>();
builder.Services.AddScoped<EvaluacionEvidenciaService>();
builder.Services.AddScoped<FincaEvidenciaService>();
builder.Services.AddScoped<AutenticacionManager>();
builder.Services.AddScoped<RecuperacionContrasenaManager>();
builder.Services.AddScoped<IPasswordRecoveryPolicy, PasswordRecoveryPolicy>();
builder.Services.AddScoped<IPasswordRecoveryEmailSender, PasswordRecoveryEmailSender>();
builder.Services.AddScoped<EvaluacionTecnicaManager>();
builder.Services.AddScoped<FincaManager>();
builder.Services.AddScoped<AdministracionManager>();
builder.Services.AddScoped<PagosManager>();
builder.Services.AddScoped<ReportesManager>();
builder.Services.AddScoped<LandingManager>();
builder.Services.AddScoped<NotificacionesManager>();
builder.Services.AddScoped<INotificationEmailSender, SmtpNotificationEmailSender>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<ISecurityThrottleService, SecurityThrottleService>();

ValidarConfiguracionCritica(builder.Configuration, builder.Environment);

var app = builder.Build();
AsegurarCarpetasCarga(app.Environment);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("./v1/swagger.json", "PSA.WebAPI v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/openapi/v1.json", () => Results.Redirect("/swagger/v1/swagger.json"));

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();


static string[] ObtenerCorsOrigins(IConfiguration configuration)
{
    var originsSection = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    var originsCsv = configuration["Cors:AllowedOriginsCsv"] ?? string.Empty;

    var origins = originsSection
        .Concat(originsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out _))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    return origins;
}

static void ValidarConfiguracionCritica(IConfiguration configuration, IHostEnvironment environment)
{
    if (!environment.IsProduction())
    {
        return;
    }

    var connectionString = configuration.GetConnectionString("PSAConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Falta ConnectionStrings:PSAConnection para ambiente Production.");
    }

    var jwtKey = configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("set-via", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Falta Jwt:Key válido para ambiente Production.");
    }

    var corsOrigins = ObtenerCorsOrigins(configuration);
    if (corsOrigins.Length == 0)
    {
        throw new InvalidOperationException("Falta configurar Cors:AllowedOrigins para ambiente Production.");
    }

    var smtp = SmtpSettingsResolver.Resolve(configuration);
    var missingSmtpKeys = SmtpSettingsResolver.GetMissingRequiredKeys(smtp);
    if (missingSmtpKeys.Count > 0)
    {
        Console.Error.WriteLine($"[CONFIG] SMTP incompleto para Production. Variables faltantes: {string.Join(", ", missingSmtpKeys)}");
    }
}

static void AsegurarCarpetasCarga(IHostEnvironment environment)
{
    var uploadsPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
    Directory.CreateDirectory(uploadsPath);
}

static void SanitizarAppSettingsSiEsNecesario()
{
    var rutaBase = AppContext.BaseDirectory;
    var rutaCandidata1 = Path.Combine(rutaBase, "appsettings.json");
    var rutaCandidata2 = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    var appSettingsPath = File.Exists(rutaCandidata1) ? rutaCandidata1 : rutaCandidata2;

    if (!File.Exists(appSettingsPath))
    {
        return;
    }

    var contenido = File.ReadAllText(appSettingsPath);
    var tieneSeccionConnectionStrings = contenido.Contains("\"ConnectionStrings\"", StringComparison.Ordinal);
    var tieneClaveDuplicada = Regex.IsMatch(contenido, "\"ConnectionStrings:PSAConnection\"\\s*:", RegexOptions.CultureInvariant);

    if (!tieneSeccionConnectionStrings || !tieneClaveDuplicada)
    {
        return;
    }

    var contenidoSanitizado = Regex.Replace(
        contenido,
        "^\\s*\"ConnectionStrings:PSAConnection\"\\s*:\\s*\".*?\"\\s*,?\\s*(?:\\r?\\n)?",
        string.Empty,
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    File.WriteAllText(appSettingsPath, contenidoSanitizado);
}

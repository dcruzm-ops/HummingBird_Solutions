using PSA.AppCore.Managers;
using PSA.AppCore.Servicios;
using PSA.DataAccess.DAO;
using Microsoft.AspNetCore.Authentication.Cookies;
using PSA.WebApp.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    var provider = options.ModelBindingMessageProvider;
    provider.SetValueMustNotBeNullAccessor(_ => "Este campo es obligatorio.");
    provider.SetMissingBindRequiredValueAccessor(_ => "Este campo es obligatorio.");
    provider.SetMissingRequestBodyRequiredValueAccessor(() => "La solicitud es obligatoria.");
    provider.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
    provider.SetUnknownValueIsInvalidAccessor(campo => $"El valor seleccionado no es válido para {campo}.");
    provider.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
});
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Autenticacion/IniciarSesion";
        options.AccessDeniedPath = "/Autenticacion/IniciarSesion";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddHttpClient("AuthApi")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return handler;
    });

builder.Services.AddScoped<IServicioHashContrasena, ServicioHashContrasena>();

builder.Services.AddScoped<UsuarioDAO>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection' en WebApp.");
    }

    return new UsuarioDAO(connectionString);
});

builder.Services.AddScoped<FincaDAO>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection' en WebApp.");
    }

    return new FincaDAO(connectionString);
});

builder.Services.AddScoped<TokenRecuperacionDAO>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection' en WebApp.");
    }

    return new TokenRecuperacionDAO(connectionString);
});

builder.Services.AddScoped<DashboardDAO>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection' en WebApp.");
    }

    return new DashboardDAO(connectionString);
});

builder.Services.AddScoped<AutenticacionManager>();
builder.Services.AddScoped<RecuperacionContrasenaManager>();
builder.Services.AddScoped<IServicioCorreo, ServicioCorreoSmtp>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

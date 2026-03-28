using PSA.AppCore.Managers;
using PSA.AppCore.Servicios;
using PSA.DataAccess.DAO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

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

builder.Services.AddScoped<AutenticacionManager>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Autenticacion}/{action=IniciarSesion}/{id?}");

app.Run();

using Microsoft.AspNetCore.Authentication.Cookies;
using PSA.DataAccess;
using PSA.DataAccess.DAO;
using PSA.WebApp.Services;

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

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("Debe configurar ApiSettings:BaseUrl en PSA.WebApp.");
}

var httpClientBuilder = builder.Services.AddHttpClient("AuthApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

if (builder.Environment.IsDevelopment())
{
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
}

builder.Services.AddScoped<HttpClientService>();

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

builder.Services.AddScoped<DashboardDAO>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Autenticacion/IniciarSesion";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

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
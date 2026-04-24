using PSA.WebApp.Services.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiUserHeadersHandler>();

var httpClientBuilder = builder.Services.AddHttpClient("AuthApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

httpClientBuilder.AddHttpMessageHandler<ApiUserHeadersHandler>();

if (builder.Environment.IsDevelopment())
{
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
}

builder.Services.AddScoped<HttpClientService>();

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
app.Use(async (context, next) =>
{
    await next();

    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using PSA.AppCore;
using PSA.AppCore.Managers;
using PSA.AppCore.Servicios;
using PSA.DataAccess;
using PSA.DataAccess.DAO;
using PSA.WebAPI.Controllers.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:59664")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IServicioHashContrasena, ServicioHashContrasena>();

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
builder.Services.AddScoped<AuditoriaLogDAO>();
builder.Services.AddScoped<RolPermisoDAO>();
builder.Services.AddScoped<ConfiguracionPagoDAO>();
builder.Services.AddScoped<CuentaBancariaDAO>();
builder.Services.AddScoped<DashboardDAO>();
builder.Services.AddScoped<ReportesDAO>();
builder.Services.AddScoped<LandingDAO>();

builder.Services.AddScoped<FincaService>();
builder.Services.AddScoped<EvaluacionService>();
builder.Services.AddScoped<FincaEvidenciaService>();
builder.Services.AddScoped<AutenticacionManager>();
builder.Services.AddScoped<RecuperacionContrasenaManager>();
builder.Services.AddScoped<EvaluacionTecnicaManager>();
builder.Services.AddScoped<FincaManager>();
builder.Services.AddScoped<AdministracionManager>();
builder.Services.AddScoped<ReportesManager>();
builder.Services.AddScoped<LandingManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("./v1/swagger.json", "PSA.WebAPI v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapGet("/openapi/v1.json", () => Results.Redirect("/swagger/v1/swagger.json"));

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();

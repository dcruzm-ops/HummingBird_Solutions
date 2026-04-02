using PSA.AppCore;
using PSA.AppCore.Managers;
using PSA.AppCore.Servicios;
using PSA.DataAccess;
using PSA.DataAccess.DAO;
using PSA.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Ambiente: " + builder.Environment.EnvironmentName);
Console.WriteLine("PSAConnection: " + builder.Configuration["ConnectionStrings:PSAConnection"]);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("https://localhost:59664")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddScoped<IServicioHashContrasena, ServicioHashContrasena>();

builder.Services.AddScoped<DbContextHelper>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection'.");

    return new DbContextHelper(connectionString);
});

builder.Services.AddScoped<UsuarioDAO>(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("PSAConnection");
    return new UsuarioDAO(cs);
});

builder.Services.AddScoped<FincaDAO>(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("PSAConnection");
    return new FincaDAO(cs);
});

builder.Services.AddScoped<EvaluacionDAO>(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("PSAConnection");
    return new EvaluacionDAO(cs);
});

builder.Services.AddScoped<EvaluacionTecnicaDAO>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection'.");
    }

    return new EvaluacionTecnicaDAO(connectionString);
});

builder.Services.AddScoped<RecuperacionContrasenaDAO>(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("PSAConnection");
    return new RecuperacionContrasenaDAO(cs);
});

builder.Services.AddScoped<TokenRecuperacionDAO>(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("PSAConnection");
    return new TokenRecuperacionDAO(cs);
});

builder.Services.AddScoped<FincaEvidenciaDAO>(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("PSAConnection");
    return new FincaEvidenciaDAO(cs);
});

builder.Services.AddScoped<FincaService>();
builder.Services.AddScoped<EvaluacionService>();
builder.Services.AddScoped<FincaEvidenciaService>();
builder.Services.AddScoped<AutenticacionManager>();
builder.Services.AddScoped<RecuperacionContrasenaManager>();
builder.Services.AddScoped<EvaluacionTecnicaManager>();

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

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();

app.Run();

using PSA.AppCore;
using PSA.AppCore.Managers;
using PSA.AppCore.Servicios;
using PSA.DataAccess;
using PSA.DataAccess.DAO;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Ambiente: " + builder.Environment.EnvironmentName);
Console.WriteLine("PSAConnection: " + builder.Configuration["ConnectionStrings:PSAConnection"]);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IServicioHashContrasena, ServicioHashContrasena>();

builder.Services.AddScoped<DbContextHelper>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection'.");
    }

    return new DbContextHelper(connectionString);
});

builder.Services.AddScoped<UsuarioDAO>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection'.");
    }

    return new UsuarioDAO(connectionString);
});

builder.Services.AddScoped<FincaDAO>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection'.");
    }

    return new FincaDAO(connectionString);
});

builder.Services.AddScoped<RecuperacionContrasenaDAO>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("PSAConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No se encontró la cadena de conexión 'PSAConnection'.");
    }

    return new RecuperacionContrasenaDAO(connectionString);
});

builder.Services.AddScoped<FincaService>();
builder.Services.AddScoped<AutenticacionManager>();
builder.Services.AddScoped<RecuperacionContrasenaManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PSA.WebAPI v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
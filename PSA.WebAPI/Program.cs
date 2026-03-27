using PSA.AppCore.Managers;
using PSA.AppCore.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IServicioHashContrasena, ServicioHashContrasena>();
builder.Services.AddScoped<AutenticacionManager>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
using PSA.DataAccess;
using PSA.AppCore;
using PSA.DataAccess.DAO;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión DefaultConnection.");

builder.Services.AddSingleton(new DbContextHelper(connectionString));

builder.Services.AddScoped<FincaDAO>();
builder.Services.AddScoped<FincaService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirWebApp", policy =>
    {
        policy.WithOrigins("https://localhost:59664", "http://localhost:59664")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("PermitirWebApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
using TTronAlert.Api.Data;
using TTronAlert.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using TTronAlert.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MariaDB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("MariaDB"),
        new MariaDbServerVersion(new Version(10, 4, 32))
    ));

// SignalR
builder.Services.AddSignalR();

// CORS avec AllowCredentials pour SignalR
builder.Services.AddCors(options =>
    options.AddPolicy("DevCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // autorise TOUT, y compris file:// et origin null
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // obligatoire pour SignalR
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS avant UseAuthorization
app.UseCors("DevCorsPolicy");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AlertHub>("/alerthub"); // Matcher le client

app.Run();
using App.Server.ORM;
using App.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

//Purpose: App bootstrap (DI, EF Core SQLite, cookie auth, CORS, middleware pipeline, controller mapping).
//Inputs/Outputs: Reads appsettings connection string and environment; exposes API endpoints and static SPA fallback.


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Keep API payloads aligned with frontend camelCase access.
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Making TestService available for dependency injection 
//Dependency Injection reduces the hard-coded dependencies among your classes by injecting those dependencies at run time instead of design time technically.
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<AuthService>();

// Connects the backend to the SQLite database
builder.Services.AddDbContext<AppDbContext>((config) => {
    var connectionBuilder = new SqliteConnectionStringBuilder(
        builder.Configuration.GetConnectionString("DefaultConnection"))
    {
        DefaultTimeout = 30
    };

    config.UseSqlite(connectionBuilder.ToString());
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

// CORS controls which frontend URLs are allowed to call the backend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                // Common dev frontend ports
                "https://localhost:7167",
                "http://localhost:7167",
                // Backend (Kestrel) default when run via launch (https)
                "https://localhost:7227",
                "http://localhost:7227",
                // legacy / alternate dev ports
                "https://localhost:5173",
                "http://localhost:5173",
                "https://localhost:59553",
                "http://localhost:59553"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for cookies
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// Use CORS before authentication
app.UseCors();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();

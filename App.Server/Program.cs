using App.Server.ORM;
using App.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

//Purpose: App bootstrap (DI, EF Core SQLite, cookie auth, CORS, middleware pipeline, controller mapping).
//Inputs/Outputs: Reads appsettings connection string and environment; exposes API endpoints and static SPA fallback.
//Depends on: App.Server/ORM/AppDbContext.cs, App.Server/Services/AuthService.cs, App.Server/Services/TestService.cs.

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
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<AuthService>();

// Fix: Use "DefaultConnection" to match appsettings.json
builder.Services.AddDbContext<AppDbContext>((config) => {
    config.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

// Fix: Single CORS policy that allows both possible frontend URLs
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

// Fix: Use CORS before authentication
app.UseCors();

// Fix: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();
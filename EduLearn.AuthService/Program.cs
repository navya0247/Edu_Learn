using System.Text;
using EduLearn.AuthService.Data;
using EduLearn.AuthService.Helpers;
using EduLearn.AuthService.Interfaces;
using EduLearn.AuthService.Repositories;
using EduLearn.AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;


// ── Serilog early setup ───────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// ── Database — EduLearn_Auth (SQL Server LocalDB) ─────────────────────────────
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(
    builder.Configuration.GetConnectionString("AuthDb"),
    sql => sql.MigrationsAssembly("EduLearn.AuthService")
));

// ── Dependency Injection ──────────────────────────────────────────────────────
// AddScoped = new instance per HTTP request
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// JwtHelper registered as singleton — stateless, safe to reuse
builder.Services.AddSingleton<JwtHelper>();

// ── JWT Bearer Authentication ─────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret missing in appsettings.json");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey        = new SymmetricSecurityKey(
                                          Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer          = false,
            ValidateAudience        = false,
            ClockSkew               = TimeSpan.Zero
        };
    })
    .AddGoogle(options =>
    {
        // Fill these in appsettings.json when you want Google login
        options.ClientId     = builder.Configuration["Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
    });

builder.Services.AddAuthorization();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger with JWT support ──────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "EduLearn Auth Service",
        Version     = "v1",
        Description = "Handles user registration, login, JWT tokens, and profile management"
    });

    // Adds Authorize button in Swagger UI to test protected endpoints
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {your JWT token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS — allow React frontend ───────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("EduLearnCors", policy =>
        policy.WithOrigins(
                  "http://localhost:3000",  // React dev server
                  "http://localhost:5173")  // Vite dev server (if used)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddHealthChecks();

// ── Build App ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Swagger UI ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduLearn Auth Service v1");
    c.RoutePrefix = "swagger";
});

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseCors("EduLearnCors");
app.UseSerilogRequestLogging();
app.UseAuthentication();   // Must be before UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("EduLearn Auth Service running → http://localhost:5001/swagger");
app.Run();
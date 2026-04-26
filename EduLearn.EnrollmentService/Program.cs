using System.Text;
using EduLearn.EnrollmentService.Data;
using EduLearn.EnrollmentService.Interfaces;
using EduLearn.EnrollmentService.Messaging;
using EduLearn.EnrollmentService.Repositories;
using EduLearn.EnrollmentService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// ── PostgreSQL ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<EnrollmentDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("EnrollmentDb"),
        sql => sql.MigrationsAssembly("EduLearn.EnrollmentService")));

// ── Dependency Injection ──────────────────────────────────────────────────────
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// ── RabbitMQ Consumer (Background Service) ────────────────────────────────────
// PDF Non-Functional: async enrollment confirmation events via RabbitMQ.
// Listens to "payment.success" queue — auto-enrolls student after payment.
// Skips gracefully if RabbitMQ is not running.
builder.Services.AddHostedService<PaymentSuccessConsumer>();

// ── JWT Bearer ────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret missing");

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
            IssuerSigningKey        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer          = false,
            ValidateAudience        = false,
            ClockSkew               = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "EduLearn Enrollment Service",
        Version     = "v1",
        Description = "Student enrollment with RabbitMQ auto-enrollment on payment"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {JWT token}",
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("EduLearnCors", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduLearn Enrollment Service v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("EduLearnCors");
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("EduLearn Enrollment Service running → http://localhost:5004/swagger");
app.Run();

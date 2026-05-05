using System.Text;
using EduLearn.ReviewService.Data;
using EduLearn.ReviewService.Interfaces;
using EduLearn.ReviewService.Repositories;
using EduLearn.ReviewService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// ── Database: EduLearn_Review PostgreSQL ──────────────────────────────────────
builder.Services.AddDbContext<ReviewDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ReviewDb"),
        sql => sql.MigrationsAssembly("EduLearn.ReviewService")));

// ── Dependency Injection ──────────────────────────────────────────────────────
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// ── JWT Bearer — same secret as AuthService ───────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret missing");

builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
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

// ── Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "EduLearn Review Service",
        Version     = "v1",
        Description = "Course reviews, star ratings, admin moderation and rating analytics"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT from AuthService /api/auth/login — format: Bearer {token}",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddCors(o => o.AddPolicy("EduLearnCors", p =>
    p.WithOrigins("http://localhost:3000", "http://localhost:5173")
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduLearn Review Service v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("EduLearnCors");
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("EduLearn Review Service running → http://localhost:5007/swagger");
app.Run();
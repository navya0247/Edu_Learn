using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// ── YARP Reverse Proxy — reads routes from appsettings.json ──────────────────
// PDF: YARP (Yarp.ReverseProxy) for service routing and load balancing
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── JWT Bearer — Gateway validates token once before forwarding ───────────────
// This means each microservice does NOT need to re-validate (optional)
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret missing");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// ── Rate Limiting — PDF: Microsoft.AspNetCore.RateLimiting ───────────────────
// Protects all services from being flooded with requests
builder.Services.AddRateLimiter(options =>
{
    // Fixed window: 100 requests per 1 minute per IP
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit         = 100;
        limiterOptions.Window              = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit          = 5;
    });

    // Return 429 Too Many Requests when limit hit
    options.RejectionStatusCode = 429;

    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded — please wait before sending more requests.");
    };
});

// ── CORS — allow React frontend ───────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("EduLearnCors", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ── Swagger — shows gateway routes ───────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "EduLearn API Gateway",
        Version     = "v1",
        Description = "Single entry point for all EduLearn microservices via YARP"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {JWT token from /api/auth/login}",
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

builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduLearn API Gateway v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("EduLearnCors");
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint for the gateway itself
app.MapHealthChecks("/health");

// ── YARP handles all /api/* routes ───────────────────────────────────────────
// Rate limiter applied to all proxied routes
app.MapReverseProxy().RequireRateLimiting("fixed");

Log.Information("EduLearn API Gateway running → http://localhost:5000/swagger");
Log.Information("All /api/* routes proxied to correct microservices");
app.Run();

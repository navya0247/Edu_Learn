using System.Text;
using EduLearn.CourseService.Data;
using EduLearn.CourseService.Interfaces;
using EduLearn.CourseService.Repositories;
using EduLearn.CourseService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddDbContext<CourseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CourseDb"),
        sql => sql.MigrationsAssembly("EduLearn.CourseService")));

builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"; options.InstanceName = "EduLearn_Course_"; });
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");
builder.Services.AddAuthentication(options => { options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; })
    .AddJwtBearer(options => { options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), ValidateIssuer = false, ValidateAudience = false, ClockSkew = TimeSpan.Zero }; });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduLearn Course Service", Version = "v1" }); c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Description = "Enter: Bearer {JWT token}", Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.ApiKey, Scheme = "Bearer" }); c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } }); });

builder.Services.AddCors(options => options.AddPolicy("EduLearnCors", policy =>
    policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://edulearn-frontend-sbw4.onrender.com")
          .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddHealthChecks();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
    try { db.Database.Migrate(); Log.Information("Course DB migrated!"); }
    catch (Exception ex) { Log.Error(ex, "Course DB migration failed!"); }
}

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduLearn Course Service v1"); c.RoutePrefix = "swagger"; });
app.UseCors("EduLearnCors");
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
Log.Information("EduLearn Course Service running → http://localhost:5002/swagger");
app.Run();
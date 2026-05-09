using EduLearn.PaymentService.Data;
using EduLearn.PaymentService.Interfaces;
using EduLearn.PaymentService.Messaging;
using EduLearn.PaymentService.Repositories;
using EduLearn.PaymentService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb"),
        sql => sql.MigrationsAssembly("EduLearn.PaymentService")));

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), ValidateIssuer = false, ValidateAudience = false, ClockSkew = TimeSpan.Zero }; });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<RabbitMqPublisher>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduLearn Payment Service", Version = "v1" }); c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header }); c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } }); });

builder.Services.AddCors(o => o.AddPolicy("EduLearnCors", p =>
    p.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://edulearn-frontend-sbw4.onrender.com")
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try { db.Database.Migrate(); Log.Information("Payment DB migrated!"); }
    catch (Exception ex) { Log.Error(ex, "Payment DB migration failed!"); }
}

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduLearn Payment Service v1"); c.RoutePrefix = "swagger"; });
app.UseCors("EduLearnCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
Log.Information("EduLearn Payment Service running → http://localhost:5008/swagger");
app.Run();
using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using TcellxFreedom.API.Extensions;
using TcellxFreedom.API.Middleware;
using TcellxFreedom.Application;
using TcellxFreedom.Infrastructure;
using TcellxFreedom.Infrastructure.Jobs;
using TcellxFreedom.Infrastructure.Data.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Cloud Run sets PORT env var — listen on it
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TcellxFreedom API",
        Version = "v1",
        Description = "API барои идоракунии корбарон бо OTP authentication"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header. Намуна: 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddMemoryCache();

var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? ["https://tcellx-freedom.vercel.app"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.ApplyMigrationsAsync();
await app.SeedTcellPassDataAsync();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

app.MapControllers();

// Register recurring background jobs
RecurringJob.AddOrUpdate<RecurringTaskGeneratorJob>(
    "generate-recurring-tasks",
    job => job.ExecuteAsync(),
    Cron.Daily);

RecurringJob.AddOrUpdate<WeeklyStatisticsCalculatorJob>(
    "calculate-weekly-stats",
    job => job.ExecuteAsync(),
    "0 23 * * 0");

RecurringJob.AddOrUpdate<DailyTaskAssignmentJob>(
    "assign-daily-tasks",
    job => job.ExecuteAsync(),
    Cron.Daily);

RecurringJob.AddOrUpdate<ExpireOldTasksJob>(
    "expire-old-tasks",
    job => job.ExecuteAsync(),
    "1 0 * * *");

app.Run();

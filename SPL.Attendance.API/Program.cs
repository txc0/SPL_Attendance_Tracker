using Microsoft.EntityFrameworkCore;
using SPL.Attendance.API.Middleware;
using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Services;
using SPL.Attendance.Data.Context;
using SPL.Attendance.Data.Repositories;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("SPLAttendanceDB")
    ?? throw new InvalidOperationException(
        "Connection string 'SPLAttendanceDB' is missing from appsettings.json.");

builder.Services.AddDbContext<SPLAttendanceDbContext>(options =>
    options.UseMySql(connectionString,
                     ServerVersion.AutoDetect(connectionString),
                     mySqlOptions => mySqlOptions.MigrationsAssembly("SPL.Attendance.Data")));

//   Data Layer
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IShowCauseRepository, ShowCauseRepository>();
builder.Services.AddScoped<IShowCauseService, ShowCauseService>();

//   Business Layer
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IShowCauseRepository, ShowCauseRepository>();
builder.Services.AddScoped<IShowCauseService, ShowCauseService>();

// ── JWT Authentication 
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                                         Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();



builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.WriteIndented        = true;
    });

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "SPL Attendance Management System API",
        Version     = "v1",
        Contact     = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name  = "SPL Development Team",
            Email = "dev@spl.com"
        }
    });

    // Include XML documentation in Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactClient", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json",
                          "SPL Attendance Management API v1");
        c.RoutePrefix = string.Empty; // Swagger at root: https://localhost:{port}/
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseAuthentication();
app.UseCors("AllowReactClient");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SPLAttendanceDbContext>();
    db.Database.Migrate();
}

app.Run();

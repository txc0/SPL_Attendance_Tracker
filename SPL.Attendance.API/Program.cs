using Microsoft.EntityFrameworkCore;
using SPL.Attendance.API.Middleware;
using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Services;
using SPL.Attendance.Data.Context;
using SPL.Attendance.Data.Repositories;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════════════════
// 1. DATABASE — EF Core 8 + Pomelo MySQL provider
// ══════════════════════════════════════════════════════════════════════════
var connectionString = builder.Configuration.GetConnectionString("SPLAttendanceDB")
    ?? throw new InvalidOperationException(
        "Connection string 'SPLAttendanceDB' is missing from appsettings.json.");

builder.Services.AddDbContext<SPLAttendanceDbContext>(options =>
    options.UseMySql(connectionString,
                     ServerVersion.AutoDetect(connectionString),
                     mySqlOptions => mySqlOptions.MigrationsAssembly("SPL.Attendance.Data")));

// ══════════════════════════════════════════════════════════════════════════
// 2. DEPENDENCY INJECTION (3-Tier wiring)
// ══════════════════════════════════════════════════════════════════════════
//   Data Layer
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();

//   Business Layer
builder.Services.AddScoped<IAttendanceService, AttendanceService>();

// ══════════════════════════════════════════════════════════════════════════
// 3. API LAYER — Controllers, JSON, CORS
// ══════════════════════════════════════════════════════════════════════════
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.WriteIndented        = true;
    });

builder.Services.AddEndpointsApiExplorer();

// ══════════════════════════════════════════════════════════════════════════
// 4. SWAGGER / SWASHBUCKLE
// ══════════════════════════════════════════════════════════════════════════
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title       = "SPL Attendance Management System API",
        Version     = "v1",
        Description = "Sprint 1 — Core Attendance Logic: Check-In / Check-Out / Work Hour Calculation",
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

// ══════════════════════════════════════════════════════════════════════════
// 5. CORS (allow React dev server)
// ══════════════════════════════════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactClient", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ══════════════════════════════════════════════════════════════════════════
// BUILD
// ══════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Middleware pipeline ─────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>(); // Global error handler — must be first

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
app.UseCors("AllowReactClient");
app.UseAuthorization();
app.MapControllers();

// ── Auto-apply pending EF Core migrations on startup (dev convenience) ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SPLAttendanceDbContext>();
    db.Database.Migrate();
}

app.Run();

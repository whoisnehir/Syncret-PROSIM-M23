using Microsoft.AspNetCore.Identity.Data;
using SyncretAPI.Data;
using SyncretAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);


// --- SERVICII ---
builder.Services.AddScoped<SyncretRepository>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<StatePollingService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
        policy.WithOrigins(
                  "http://localhost:5173",   // Vite dev local
                  "http://localhost:8081")   // frontend containerizat (nginx)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// --- JWT AUTH ---
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key lipsește din appsettings.json");
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();


// --- SEED USERI (rulează o dată la pornire) ---
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<SyncretRepository>();
    var adminHash = BCrypt.Net.BCrypt.HashPassword("admin123");
    var operatorHash = BCrypt.Net.BCrypt.HashPassword("operator123");
    await repo.EnsureUserAsync("admin", adminHash, "admin");
    await repo.EnsureUserAsync("operator", operatorHash, "operator");
}

// --- MIDDLEWARE ---
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();

// --- ENDPOINTS ---

// GET /api/state — stare curentă (fallback dacă SignalR nu e conectat)
app.MapGet("/api/state", async (SyncretRepository repo) =>
{
    var state = await repo.GetStateAsync();
    return state is null ? Results.NotFound() : Results.Ok(state);
})
.WithName("GetState")
.WithOpenApi();

// GET /api/logs — istoric cu filtre opționale
// Ex: /api/logs?component=B1&eventType=MOTOR_START&page=1&pageSize=50
app.MapGet("/api/logs", async (
    SyncretRepository repo,
    string? component,
    string? eventType,
    int page = 1,
    int pageSize = 50) =>
{
    var logs = await repo.GetLogsAsync(component, eventType, page, pageSize);
    return Results.Ok(logs);
})
.WithName("GetLogs")
.WithOpenApi();

// Control proces: start/stop din web
app.MapPost("/api/control", async (ControlRequest req, SyncretRepository repo, HttpContext ctx) =>
{
    await repo.SetRunningAsync(req.IsRunning);

    // Cine a apăsat — din token-ul JWT (nu poate fi falsificat de client)
    var username = ctx.User.Identity?.Name ?? "necunoscut";
    var action = req.IsRunning ? "START" : "STOP";
    await repo.AddControlLogAsync(username, action);

    return Results.Ok(new { success = true, isRunning = req.IsRunning });
})
.WithName("SetControl")
.RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "admin" });

// GET /api/control-log — raport întreruperi (cine a oprit/pornit) — doar admin
app.MapGet("/api/control-log", async (SyncretRepository repo) =>
{
    var log = await repo.GetControlLogAsync(100);
    return Results.Ok(log);
})
.WithName("GetControlLog")
.RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "admin" });

// ---- GESTIONARE UTILIZATORI (CRUD) — doar admin ----

// GET /api/users — listă utilizatori (fără parole)
app.MapGet("/api/users", async (SyncretRepository repo) =>
{
    var users = await repo.GetAllUsersAsync();
    return Results.Ok(users);
})
.WithName("GetUsers")
.RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "admin" });

// POST /api/users — creează user nou
app.MapPost("/api/users", async (CreateUserRequest req, SyncretRepository repo) =>
{
    // Validare minimă
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Username și parola sunt obligatorii." });

    if (req.Role != "admin" && req.Role != "operator")
        return Results.BadRequest(new { error = "Rol invalid (admin sau operator)." });

    var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var created = await repo.CreateUserAsync(req.Username, hash, req.Role);

    if (!created)
        return Results.Conflict(new { error = "Există deja un utilizator cu acest nume." });

    return Results.Ok(new { success = true });
})
.WithName("CreateUser")
.RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "admin" });

// DELETE /api/users/{id} — șterge user
app.MapDelete("/api/users/{id:int}", async (int id, SyncretRepository repo) =>
{
    await repo.DeleteUserAsync(id);
    return Results.Ok(new { success = true });
})
.WithName("DeleteUser")
.RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = "admin" });

// POST /api/auth/login — autentificare, întoarce token JWT
app.MapPost("/api/auth/login", async (LoginRequest req, SyncretRepository repo, IConfiguration config) =>
{
    var user = await repo.GetUserByUsernameAsync(req.Username);
    if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        return Results.Unauthorized();

    // Construiește token-ul JWT cu rolul userului
    var claims = new[]
    {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role)
    };

    var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
        System.Text.Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
        key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);

    var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new { token = tokenString, username = user.Username, role = user.Role });
})
.WithName("Login");

// GET /api/stats?lastHours=24 — statistici pentru grafic
app.MapGet("/api/stats", async (SyncretRepository repo, int lastHours = 24) =>
{
    var stats = await repo.GetHourlyStatsAsync(lastHours);
    return Results.Ok(stats);
})
.WithName("GetStats")
.WithOpenApi();

// --- SIGNALR HUB ---
app.MapHub<ProcessHub>("/hubs/process");
// Body pentru /api/control

app.Run();
// Body pentru /api/control
record ControlRequest(bool IsRunning);
record LoginRequest(string Username, string Password);
record CreateUserRequest(string Username, string Password, string Role);
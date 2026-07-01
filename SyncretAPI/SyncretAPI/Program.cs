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

var app = builder.Build();

// --- MIDDLEWARE ---
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("ReactApp");

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

app.Run();
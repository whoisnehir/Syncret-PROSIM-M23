using Microsoft.AspNetCore.SignalR;
using SyncretAPI.Data;

namespace SyncretAPI.Hubs
{
    public class ProcessHub : Hub
    {
        // Clientul React se conectează la acest hub.
        // Serverul îi trimite starea prin SendStateToAll() din background service.
        // Nu avem nevoie de metode primite de la client deocamdată.
    }

    // Background service care polling-uiește DB-ul la fiecare secundă
    // și trimite starea tuturor clienților conectați prin SignalR
    public class StatePollingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ProcessHub> _hub;
        private readonly ILogger<StatePollingService> _logger;

        public StatePollingService(
            IServiceScopeFactory scopeFactory,
            IHubContext<ProcessHub> hub,
            ILogger<StatePollingService> logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StatePollingService pornit.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<SyncretRepository>();
                    var state = await repo.GetStateAsync();

                    if (state != null)
                    {
                        // Trimite starea tuturor clienților SignalR conectați
                        await _hub.Clients.All.SendAsync("ReceiveState", state, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Eroare în StatePollingService.");
                }

                // Interval polling: 1 secundă
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
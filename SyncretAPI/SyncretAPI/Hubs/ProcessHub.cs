using Microsoft.AspNetCore.SignalR;
using SyncretAPI.Data;

namespace SyncretAPI.Hubs
{
    public class ProcessHub : Hub
    {
        // Clientul React se conecteaza la acest hub.
        // Serverul ii trimite starea prin SendStateToAll() din background service
    }

    // Background service care polling-uieste DB-ul la fiecare secunda si trimite starea tuturor clientilor conectati prin SignalR
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
                        // trimiterea tuturor clientilor SignalR conectati
                        await _hub.Clients.All.SendAsync("ReceiveState", state, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Eroare în StatePollingService.");
                }

                // Interval polling: 1 secunda
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
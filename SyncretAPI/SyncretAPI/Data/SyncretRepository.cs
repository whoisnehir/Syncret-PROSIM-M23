using Microsoft.Data.SqlClient;
using SyncretAPI.Models;

namespace SyncretAPI.Data
{
    public class SyncretRepository
    {
        private readonly string _connectionString;

        public SyncretRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("SyncretDB")
                ?? throw new InvalidOperationException("Connection string 'SyncretDB' not found.");
        }

        // ----------------------------------------------------------------
        // ProcessState — stare curentă (rândul Id=1)
        // ----------------------------------------------------------------
        public async Task<ProcessState?> GetStateAsync()
        {
            const string sql = @"
                SELECT M1, M2, M3, M4, IsAlarm, ClapetaPos, UpdatedAt
                FROM ProcessState
                WHERE Id = 1";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return new ProcessState
            {
                M1 = reader.GetBoolean(0),
                M2 = reader.GetBoolean(1),
                M3 = reader.GetBoolean(2),
                M4 = reader.GetBoolean(3),
                IsAlarm = reader.GetBoolean(4),
                ClapetaPos = reader.GetString(5),
                UpdatedAt = reader.GetDateTime(6)
            };
        }

        // ----------------------------------------------------------------
        // ProcessLogs — istoric evenimente cu filtre opționale
        // ----------------------------------------------------------------
        public async Task<List<ProcessLog>> GetLogsAsync(
            string? component = null,
            string? eventType = null,
            int page = 1,
            int pageSize = 50)
        {
            var where = new List<string>();
            if (!string.IsNullOrEmpty(component)) where.Add("Component = @Component");
            if (!string.IsNullOrEmpty(eventType)) where.Add("EventType = @EventType");

            string whereClause = where.Count > 0
                ? "WHERE " + string.Join(" AND ", where)
                : "";

            string sql = $@"
                SELECT Id, Timestamp, Component, EventType, Message
                FROM ProcessLogs
                {whereClause}
                ORDER BY Timestamp DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);

            if (!string.IsNullOrEmpty(component))
                cmd.Parameters.AddWithValue("@Component", component);
            if (!string.IsNullOrEmpty(eventType))
                cmd.Parameters.AddWithValue("@EventType", eventType);

            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            var logs = new List<ProcessLog>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new ProcessLog
                {
                    Id = reader.GetInt32(0),
                    Timestamp = reader.GetDateTime(1),
                    Component = reader.GetString(2),
                    EventType = reader.GetString(3),
                    Message = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
            return logs;
        }

        // ----------------------------------------------------------------
        // Statistici — evenimente grupate pe oră (pentru grafic)
        // ----------------------------------------------------------------
        public async Task<List<HourlyStats>> GetHourlyStatsAsync(int lastHours = 24)
        {
            const string sql = @"
                SELECT
                    DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0) AS Hour,
                    EventType,
                    COUNT(*) AS Count
                FROM ProcessLogs
                WHERE Timestamp >= DATEADD(HOUR, -@LastHours, SYSUTCDATETIME())
                GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0), EventType
                ORDER BY Hour ASC, EventType ASC";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@LastHours", lastHours);

            var stats = new List<HourlyStats>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.Add(new HourlyStats
                {
                    Hour = reader.GetDateTime(0),
                    EventType = reader.GetString(1),
                    Count = reader.GetInt32(2)
                });
            }
            return stats;
        }
    }

    public class HourlyStats
    {
        public DateTime Hour { get; set; }
        public string EventType { get; set; } = "";
        public int Count { get; set; }
    }
}
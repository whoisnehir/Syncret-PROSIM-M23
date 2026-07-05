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
        // ProcessState — stare curenta (randul Id=1)
        // ----------------------------------------------------------------
        public async Task<ProcessState?> GetStateAsync()
        {
            const string sql = @"
            SELECT M1, M2, M3, M4, IsAlarm, ClapetaPos, IsRunning, UpdatedAt
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
                IsRunning = reader.GetBoolean(6),
                UpdatedAt = reader.GetDateTime(7)
            };
        }
        // ----------------------------------------------------------------
        // Control proces — seteaza IsRunning (start/stop din web)
        // ----------------------------------------------------------------
        public async Task SetRunningAsync(bool isRunning)
        {
            const string sql = @"
        UPDATE ProcessState
        SET IsRunning = @IsRunning,
            UpdatedAt = @UpdatedAt
        WHERE Id = 1";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IsRunning", isRunning);
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
        }

        // ----------------------------------------------------------------
        // CONTROL LOG — jurnal cine a oprit/pornit (pentru raport admin)
        // ----------------------------------------------------------------
        public async Task AddControlLogAsync(string username, string action)
        {
            const string sql = @"
        INSERT INTO ControlLog (Username, Action, Timestamp)
        VALUES (@Username, @Action, @Timestamp)";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<ControlLogEntry>> GetControlLogAsync(int limit = 100)
        {
            string sql = @"
        SELECT TOP (@Limit) Id, Username, Action, Timestamp
        FROM ControlLog
        ORDER BY Timestamp DESC";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Limit", limit);

            var list = new List<ControlLogEntry>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ControlLogEntry
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Action = reader.GetString(2),
                    Timestamp = reader.GetDateTime(3)
                });
            }
            return list;
        }

        // ----------------------------------------------------------------
        // USERS — autentificare
        // ----------------------------------------------------------------
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            const string sql = "SELECT Id, Username, PasswordHash, Role FROM Users WHERE Username = @Username";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Role = reader.GetString(3)
            };
        }

        // Seed: creeaza un user daca nu exista deja
        public async Task EnsureUserAsync(string username, string passwordHash, string role)
        {
            const string sql = @"
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = @Username)
        INSERT INTO Users (Username, PasswordHash, Role)
        VALUES (@Username, @PasswordHash, @Role)";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@Role", role);
            await cmd.ExecuteNonQueryAsync();
        }

        // Lista utilizatori (fara parole — doar pentru administrare)
        public async Task<List<User>> GetAllUsersAsync()
        {
            const string sql = "SELECT Id, Username, Role FROM Users ORDER BY Id";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);

            var users = new List<User>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    PasswordHash = "",          
                    Role = reader.GetString(2)
                });
            }
            return users;
        }

        // Creeaza user nou. intoarce false daca username-ul exista deja
        public async Task<bool> CreateUserAsync(string username, string passwordHash, string role)
        {
            const string sql = @"
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = @Username)
        BEGIN
            INSERT INTO Users (Username, PasswordHash, Role)
            VALUES (@Username, @PasswordHash, @Role);
            SELECT 1;
        END
        ELSE
            SELECT 0;";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@Role", role);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }

        // sterge user dupa Id.
        public async Task DeleteUserAsync(int id)
        {
            const string sql = "DELETE FROM Users WHERE Id = @Id";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // ----------------------------------------------------------------
        // ProcessLogs — istoric evenimente cu filtre optionale
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
        // Statistici — evenimente grupate pe ora (pentru grafic)
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

    public class ControlLogEntry
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Action { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}
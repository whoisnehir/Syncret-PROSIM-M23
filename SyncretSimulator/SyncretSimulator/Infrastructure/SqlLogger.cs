using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SysConfig = System.Configuration.ConfigurationManager;

namespace SyncretSimulator.Infrastructure
{
    internal static class SqlLogger
    {
        private static readonly string _connectionString =
            SysConfig.ConnectionStrings["SyncretDB"]?.ConnectionString;

        private static bool _connectionErrorLogged = false;

        // ----------------------------------------------------------------
        // LOG EVENT — ProcessLogs (istoric)
        // ----------------------------------------------------------------
        public static void LogAsync(string component, string eventType, string message)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                Debug.WriteLine("[SqlLogger] Connection string 'SyncretDB' lipsește din App.config.");
                return;
            }

            Task.Run(() => WriteLog(component, eventType, message))
                .ContinueWith(HandleFault, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task WriteLog(string component, string eventType, string message)
        {
            const string sql = @"
                INSERT INTO ProcessLogs (Component, EventType, Message)
                VALUES (@Component, @EventType, @Message)";

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Component", Truncate(component, 100));
                    cmd.Parameters.AddWithValue("@EventType", Truncate(eventType, 50));
                    cmd.Parameters.AddWithValue("@Message",
                        string.IsNullOrEmpty(message) ? (object)DBNull.Value : message);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            _connectionErrorLogged = false;
        }

        // ----------------------------------------------------------------
        // UPSERT STATE — ProcessState (stare curentă, rândul Id=1)
        // ----------------------------------------------------------------
        public static void UpsertStateAsync(bool m1, bool m2, bool m3, bool m4,
                                            bool isAlarm, string clapetaPos)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            Task.Run(() => WriteState(m1, m2, m3, m4, isAlarm, clapetaPos))
                .ContinueWith(HandleFault, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task WriteState(bool m1, bool m2, bool m3, bool m4,
                                              bool isAlarm, string clapetaPos)
        {
            const string sql = @"
                UPDATE ProcessState
                SET M1         = @M1,
                    M2         = @M2,
                    M3         = @M3,
                    M4         = @M4,
                    IsAlarm    = @IsAlarm,
                    ClapetaPos = @ClapetaPos,
                    UpdatedAt  = GETDATE()
                WHERE Id = 1";

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@M1", m1);
                    cmd.Parameters.AddWithValue("@M2", m2);
                    cmd.Parameters.AddWithValue("@M3", m3);
                    cmd.Parameters.AddWithValue("@M4", m4);
                    cmd.Parameters.AddWithValue("@IsAlarm", isAlarm);
                    cmd.Parameters.AddWithValue("@ClapetaPos", Truncate(clapetaPos, 10));
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            _connectionErrorLogged = false;
        }

        // ----------------------------------------------------------------
        // HELPERS
        // ----------------------------------------------------------------
        private static void HandleFault(Task t)
        {
            if (!_connectionErrorLogged)
            {
                _connectionErrorLogged = true;
                Debug.WriteLine("[SqlLogger] Eroare DB: " +
                                t.Exception?.InnerException?.Message);
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "N/A";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
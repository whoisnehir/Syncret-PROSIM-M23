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

        // ----------------------------------------------------------------
        // LOG EVENT — ProcessLogs (istoric)
        // ----------------------------------------------------------------
        public static void LogAsync(string component, string eventType, string message)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                WriteError("Connection string 'SyncretDB' lipsește din App.config.");
                return;
            }

            Task.Run(() => WriteLog(component, eventType, message))
                .ContinueWith(HandleFault, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task WriteLog(string component, string eventType, string message)
        {
            const string sql = @"
                INSERT INTO ProcessLogs (Timestamp, Component, EventType, Message)
                VALUES (@Timestamp, @Component, @EventType, @Message)";

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@Component", Truncate(component, 100));
                    cmd.Parameters.AddWithValue("@EventType", Truncate(eventType, 50));
                    cmd.Parameters.AddWithValue("@Message",
                        string.IsNullOrEmpty(message) ? (object)DBNull.Value : message);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
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
                    UpdatedAt  = @UpdatedAt
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
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow); 
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        // ----------------------------------------------------------------
        // READ CONTROL — citește IsRunning din ProcessState (start/stop din web)
        // ----------------------------------------------------------------
        public static bool ReadIsRunning()
        {
            if (string.IsNullOrEmpty(_connectionString)) return true; // fallback: rulează

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT IsRunning FROM ProcessState WHERE Id = 1", conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return (bool)result;
                    }
                }
            }
            catch
            {
                // dacă DB-ul e inaccesibil, lăsăm procesul să ruleze (fail-safe)
            }
            return true;
        }
        // ----------------------------------------------------------------
        // HELPERS
        // ----------------------------------------------------------------
        private static void HandleFault(Task t)
        {
            var msg = t.Exception?.InnerException?.Message
                      ?? t.Exception?.Message
                      ?? "eroare necunoscută";
            WriteError(msg);
        }

        // Scrie eroarea pe Desktop ca s-o vezi sigur (Debug.WriteLine apare doar cu debugger atașat)
        private static void WriteError(string msg)
        {
            Debug.WriteLine("[SqlLogger] Eroare DB: " + msg);
            try
            {
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "syncret_errors.txt");
                System.IO.File.AppendAllText(path,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
            }
            catch { }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "N/A";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
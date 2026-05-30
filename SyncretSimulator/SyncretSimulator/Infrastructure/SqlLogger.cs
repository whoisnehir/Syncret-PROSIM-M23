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

        public static void LogAsync(string component, string eventType, string message)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                Debug.WriteLine("[SqlLogger] Connection string 'SyncretDB' lipsește din App.config.");
                return;
            }

            Task.Run(() => WriteToDatabase(component, eventType, message))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        if (!_connectionErrorLogged)
                        {
                            _connectionErrorLogged = true;
                            Debug.WriteLine("[SqlLogger] Eroare DB: " +
                                            t.Exception.InnerException?.Message);
                        }
                    }
                    else
                    {
                        _connectionErrorLogged = false;
                    }
                }, TaskContinuationOptions.NotOnCanceled);
        }

        private static async Task WriteToDatabase(string component, string eventType, string message)
        {
            const string sql = @"INSERT INTO ProcessLogs (Component, EventType, Message)
                                 VALUES (@Component, @EventType, @Message)";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Component", Truncate(component, 100));
                    command.Parameters.AddWithValue("@EventType", Truncate(eventType, 50));
                    command.Parameters.AddWithValue("@Message",
                        string.IsNullOrEmpty(message) ? (object)DBNull.Value : message);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "N/A";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
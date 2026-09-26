using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CC.Services
{
    public record BackupRecord(
        string FileName,
        string DatabaseName,
        string FilePath,
        long FileSizeBytes,
        DateTime Timestamp,
        bool IsSuccess,
        string? ErrorMessage = null)
    {
        public string FormattedSize => FileSizeBytes >= 1024 * 1024
            ? $"{(double)FileSizeBytes / (1024 * 1024):N2} MB"
            : $"{(double)FileSizeBytes / 1024:N1} KB";
    }

    public static class BackupService
    {
        private static readonly string DefaultBackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SweetStoryCRM",
            "Backups"
        );

        public static string GetBackupDirectory()
        {
            if (!Directory.Exists(DefaultBackupDirectory))
            {
                Directory.CreateDirectory(DefaultBackupDirectory);
            }
            return DefaultBackupDirectory;
        }

        public static async Task<List<string>> GetRegisteredDatabasesAsync()
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "MSME_MasterCRM",
                "CustomCakeCRM"
            };

            try
            {
                await using var masterContext = CrmDataService.CreateMasterDbContext();
                var tenantDbs = await masterContext.CompanyDatabases
                    .Where(cd => cd.IsActive)
                    .Select(cd => cd.DatabaseName)
                    .ToListAsync();

                foreach (var db in tenantDbs)
                {
                    if (!string.IsNullOrWhiteSpace(db))
                    {
                        list.Add(db.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetRegisteredDatabasesAsync warning] {ex.Message}");
            }

            return list.OrderBy(d => d == "MSME_MasterCRM" ? 0 : 1).ThenBy(d => d).ToList();
        }

        public static async Task<BackupRecord> BackupDatabaseAsync(string databaseName, string? server = null)
        {
            string cleanDbName = databaseName.Replace("]", "").Replace("[", "").Trim();
            string backupDir = GetBackupDirectory();
            string timestampStr = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{cleanDbName}_{timestampStr}.bak";
            string filePath = Path.Combine(backupDir, fileName);
            string serverName = string.IsNullOrWhiteSpace(server) ? "(localdb)\\MSSQLLocalDB" : server.Trim();
            string connStr = $"Server={serverName};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            try
            {
                await using (var conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string sql = $"BACKUP DATABASE [{cleanDbName}] TO DISK = @path WITH FORMAT, INIT;";
                    await using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@path", filePath);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                }

                var fileInfo = new FileInfo(filePath);
                long bytes = fileInfo.Exists ? fileInfo.Length : 0;

                await CrmDataService.LogAuditAsync(
                    "DATABASE_BACKUP_COMPLETED",
                    $"Database '{cleanDbName}' backed up successfully to '{fileName}' ({(double)bytes / (1024 * 1024):N2} MB)."
                );

                return new BackupRecord(fileName, cleanDbName, filePath, bytes, DateTime.UtcNow, true);
            }
            catch (Exception ex)
            {
                await CrmDataService.LogAuditAsync(
                    "DATABASE_BACKUP_FAILED",
                    $"Backup failed for database '{cleanDbName}': {ex.Message}"
                );

                return new BackupRecord(fileName, cleanDbName, filePath, 0, DateTime.UtcNow, false, ex.Message);
            }
        }

        public static async Task<List<BackupRecord>> BackupAllDatabasesAsync()
        {
            var databases = await GetRegisteredDatabasesAsync();
            var results = new List<BackupRecord>();

            foreach (var db in databases)
            {
                var record = await BackupDatabaseAsync(db);
                results.Add(record);
            }

            return results;
        }

        public static List<BackupRecord> GetBackupHistory()
        {
            var dir = GetBackupDirectory();
            var result = new List<BackupRecord>();

            if (!Directory.Exists(dir)) return result;

            var files = new DirectoryInfo(dir).GetFiles("*.bak")
                .OrderByDescending(f => f.CreationTimeUtc);

            foreach (var f in files)
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(f.Name);
                string dbName = nameWithoutExt;
                int lastUnderscore = nameWithoutExt.LastIndexOf('_');
                if (lastUnderscore > 0)
                {
                    int secondLast = nameWithoutExt.LastIndexOf('_', lastUnderscore - 1);
                    if (secondLast > 0)
                    {
                        dbName = nameWithoutExt.Substring(0, secondLast);
                    }
                }

                result.Add(new BackupRecord(
                    f.Name,
                    dbName,
                    f.FullName,
                    f.Length,
                    f.CreationTimeUtc,
                    true
                ));
            }

            return result;
        }
    }
}

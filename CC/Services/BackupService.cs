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
        string? ErrorMessage = null,
        bool IsCloud = false)
    {
        public string FormattedSize => FileSizeBytes >= 1024 * 1024
            ? $"{(double)FileSizeBytes / (1024 * 1024):N2} MB"
            : $"{(double)FileSizeBytes / 1024:N1} KB";
    }

    public record CloudBackupResult(
        bool Success,
        string Message,
        int TablesSynced,
        int TotalRecordsSynced,
        DateTime Timestamp,
        string? Error = null);

    public record CloudConnectionStatus(
        bool IsConnected,
        string Server,
        string Database,
        string Message,
        DateTime CheckedAt);

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

        // =========================================================
        // MONSTERASP CLOUD BACKUP (HYBRID / DISASTER RECOVERY)
        // =========================================================

        public const string CloudServer = "tcp:db67053.public.databaseasp.net,1433";
        public const string CloudServerDisplay = "db67053.public.databaseasp.net";
        public const string CloudDatabase = "db67053";
        public const string CloudUser = "db67053";
        public const string CloudPassword = "sF=9@4LrY#t2";

        public static string CloudConnectionString =>
            $"Server={CloudServer};Database={CloudDatabase};User Id={CloudUser};Password={CloudPassword};TrustServerCertificate=True;Encrypt=True;Connection Timeout=60;MultipleActiveResultSets=True;";

        private static readonly string CloudBackupMetaFile = Path.Combine(GetBackupDirectory(), "last_cloud_backup.txt");

        public static (DateTime? Timestamp, int TotalRows, int TablesCount) GetLastCloudBackupInfo()
        {
            try
            {
                if (File.Exists(CloudBackupMetaFile))
                {
                    var lines = File.ReadAllLines(CloudBackupMetaFile);
                    if (lines.Length >= 3 &&
                        DateTime.TryParse(lines[0], out var dt) &&
                        int.TryParse(lines[1], out var rows) &&
                        int.TryParse(lines[2], out var tbls))
                    {
                        return (dt, rows, tbls);
                    }
                }
            }
            catch { }
            return (null, 0, 0);
        }

        private static void SaveLastCloudBackupInfo(DateTime timestamp, int rows, int tables)
        {
            try
            {
                File.WriteAllLines(CloudBackupMetaFile, new[]
                {
                    timestamp.ToString("o"),
                    rows.ToString(),
                    tables.ToString()
                });
            }
            catch { }
        }

        public static async Task<CloudConnectionStatus> TestCloudConnectionAsync()
        {
            var now = DateTime.UtcNow;
            try
            {
                await using var conn = new SqlConnection(CloudConnectionString);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT @@VERSION";
                var ver = (await cmd.ExecuteScalarAsync())?.ToString() ?? "SQL Server";
                string shortVer = ver.Split('\n')[0].Trim();
                return new CloudConnectionStatus(true, CloudServerDisplay, CloudDatabase, $"Online · {shortVer}", now);
            }
            catch (Exception ex)
            {
                return new CloudConnectionStatus(false, CloudServerDisplay, CloudDatabase, ex.Message, now);
            }
        }

        public static async Task<CloudBackupResult> BackupToCloudAsync(IProgress<string>? progress = null)
        {
            progress?.Report("Connecting to MonsterASP Cloud database...");

            try
            {
                // 1. Ensure Schema exists in MonsterASP cloud database
                var cloudOptions = new DbContextOptionsBuilder<CC.infrastructure.Data.CrmDbContext>()
                    .UseSqlServer(CloudConnectionString, sql => sql.CommandTimeout(180))
                    .Options;

                await using (var cloudContext = new CC.infrastructure.Data.CrmDbContext(cloudOptions))
                {
                    progress?.Report("Verifying/creating database schema in MonsterASP cloud...");
                    var creator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>(cloudContext.Database);
                    if (!await creator.HasTablesAsync())
                    {
                        await creator.CreateTablesAsync();
                    }
                }

                // 2. Open connections to Local DB and Cloud DB
                string localConnStr = CrmDataService.DefaultTenantConnectionString;
                int tablesSynced = 0;
                int totalRows = 0;

                string[] tables = new[]
                {
                    "Roles",
                    "Companies",
                    "Addresses",
                    "SubscriptionPlans",
                    "SubscriptionStatuses",
                    "Subscriptions",
                    "AppUsers",
                    "TermsAndConditions",
                    "TermsAcceptances",
                    "FollowUpStatuses",
                    "OrderStatuses",
                    "PaymentMethods",
                    "PaymentStatuses",
                    "Customers",
                    "CakeCustomizations",
                    "SalesOrders",
                    "SalesOrderDetails",
                    "Payments",
                    "CustomerInquiries",
                    "CustomerFollowUps",
                    "RetentionEmailTemplates",
                    "RetentionSettings",
                    "RetentionRequests",
                    "RetentionEmailLogs",
                    "ReportLogs",
                    "ReportParameters",
                    "SystemAuditLogs"
                };

                await using var localConn = new SqlConnection(localConnStr);
                await using var cloudConn = new SqlConnection(CloudConnectionString);

                await localConn.OpenAsync();
                await cloudConn.OpenAsync();

                // 3. Temporarily disable foreign keys on cloud database
                progress?.Report("Preparing cloud database tables...");
                await using (var disableCmd = cloudConn.CreateCommand())
                {
                    disableCmd.CommandText = @"
                        DECLARE @sql NVARCHAR(MAX) = N'';
                        SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL; '
                        FROM sys.tables t
                        JOIN sys.schemas s ON t.schema_id = s.schema_id;
                        IF LEN(@sql) > 0 EXEC sp_executesql @sql;";
                    try { await disableCmd.ExecuteNonQueryAsync(); } catch { }
                }

                // 4. Truncate/delete existing cloud data in reverse dependency order
                for (int i = tables.Length - 1; i >= 0; i--)
                {
                    string tbl = tables[i];
                    try
                    {
                        await using var delCmd = cloudConn.CreateCommand();
                        delCmd.CommandText = $"IF OBJECT_ID('[{tbl}]', 'U') IS NOT NULL DELETE FROM [{tbl}];";
                        await delCmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Delete cloud table {tbl}] {ex.Message}");
                    }
                }

                // 5. Bulk copy each table from local to cloud
                foreach (var tbl in tables)
                {
                    try
                    {
                        await using var chkCmd = localConn.CreateCommand();
                        chkCmd.CommandText = $"IF OBJECT_ID('[{tbl}]', 'U') IS NOT NULL SELECT COUNT(*) FROM [{tbl}]; ELSE SELECT -1;";
                        int count = (int)(await chkCmd.ExecuteScalarAsync() ?? -1);

                        if (count <= 0) continue;

                        progress?.Report($"Syncing {tbl} ({count} rows) to cloud...");

                        await using var readCmd = localConn.CreateCommand();
                        readCmd.CommandText = $"SELECT * FROM [{tbl}];";
                        await using var reader = await readCmd.ExecuteReaderAsync();

                        using var bulk = new SqlBulkCopy(cloudConn, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls, null)
                        {
                            DestinationTableName = $"[{tbl}]",
                            BulkCopyTimeout = 120
                        };

                        for (int c = 0; c < reader.FieldCount; c++)
                        {
                            string colName = reader.GetName(c);
                            bulk.ColumnMappings.Add(colName, colName);
                        }

                        await bulk.WriteToServerAsync(reader);

                        tablesSynced++;
                        totalRows += count;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Sync {tbl} to cloud warning] {ex.Message}");
                    }
                }

                // 6. Re-enable foreign keys
                progress?.Report("Finalizing cloud database constraints...");
                await using (var enableCmd = cloudConn.CreateCommand())
                {
                    enableCmd.CommandText = @"
                        DECLARE @sql NVARCHAR(MAX) = N'';
                        SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL; '
                        FROM sys.tables t
                        JOIN sys.schemas s ON t.schema_id = s.schema_id;
                        IF LEN(@sql) > 0 EXEC sp_executesql @sql;";
                    try { await enableCmd.ExecuteNonQueryAsync(); } catch { }
                }

                // 7. Audit log
                await CrmDataService.LogAuditAsync(
                    "DATABASE_CLOUD_BACKUP_COMPLETED",
                    $"Successfully backed up local database to MonsterASP Cloud ({CloudServer}). Synced {tablesSynced} tables, {totalRows} total records."
                );

                SaveLastCloudBackupInfo(DateTime.UtcNow, totalRows, tablesSynced);

                progress?.Report("Cloud backup completed successfully!");
                return new CloudBackupResult(true, $"Successfully backed up to MonsterASP Cloud! ({totalRows} records across {tablesSynced} tables)", tablesSynced, totalRows, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await CrmDataService.LogAuditAsync(
                    "DATABASE_CLOUD_BACKUP_FAILED",
                    $"Failed to backup to MonsterASP Cloud: {ex.Message}"
                );
                return new CloudBackupResult(false, $"Cloud backup failed: {ex.Message}", 0, 0, DateTime.UtcNow, ex.Message);
            }
        }

        public static async Task<CloudBackupResult> RestoreFromCloudAsync(IProgress<string>? progress = null)
        {
            progress?.Report("Connecting to MonsterASP Cloud to retrieve backup...");

            try
            {
                string localConnStr = CrmDataService.DefaultTenantConnectionString;
                int tablesRestored = 0;
                int totalRows = 0;

                string[] tables = new[]
                {
                    "Roles",
                    "Companies",
                    "Addresses",
                    "SubscriptionPlans",
                    "SubscriptionStatuses",
                    "Subscriptions",
                    "AppUsers",
                    "TermsAndConditions",
                    "TermsAcceptances",
                    "FollowUpStatuses",
                    "OrderStatuses",
                    "PaymentMethods",
                    "PaymentStatuses",
                    "Customers",
                    "CakeCustomizations",
                    "SalesOrders",
                    "SalesOrderDetails",
                    "Payments",
                    "CustomerInquiries",
                    "CustomerFollowUps",
                    "RetentionEmailTemplates",
                    "RetentionSettings",
                    "RetentionRequests",
                    "RetentionEmailLogs",
                    "ReportLogs",
                    "ReportParameters",
                    "SystemAuditLogs"
                };

                await using var localConn = new SqlConnection(localConnStr);
                await using var cloudConn = new SqlConnection(CloudConnectionString);

                await localConn.OpenAsync();
                await cloudConn.OpenAsync();

                // 1. Temporarily disable foreign keys on local database
                progress?.Report("Preparing local database tables...");
                await using (var disableCmd = localConn.CreateCommand())
                {
                    disableCmd.CommandText = @"
                        DECLARE @sql NVARCHAR(MAX) = N'';
                        SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL; '
                        FROM sys.tables t
                        JOIN sys.schemas s ON t.schema_id = s.schema_id;
                        IF LEN(@sql) > 0 EXEC sp_executesql @sql;";
                    try { await disableCmd.ExecuteNonQueryAsync(); } catch { }
                }

                // 2. Truncate/delete existing local data in reverse dependency order
                for (int i = tables.Length - 1; i >= 0; i--)
                {
                    string tbl = tables[i];
                    try
                    {
                        await using var delCmd = localConn.CreateCommand();
                        delCmd.CommandText = $"IF OBJECT_ID('[{tbl}]', 'U') IS NOT NULL DELETE FROM [{tbl}];";
                        await delCmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Delete local table {tbl}] {ex.Message}");
                    }
                }

                // 3. Bulk copy each table from cloud to local
                foreach (var tbl in tables)
                {
                    try
                    {
                        await using var chkCmd = cloudConn.CreateCommand();
                        chkCmd.CommandText = $"IF OBJECT_ID('[{tbl}]', 'U') IS NOT NULL SELECT COUNT(*) FROM [{tbl}]; ELSE SELECT -1;";
                        int count = (int)(await chkCmd.ExecuteScalarAsync() ?? -1);

                        if (count <= 0) continue;

                        progress?.Report($"Restoring {tbl} ({count} rows) from cloud...");

                        await using var readCmd = cloudConn.CreateCommand();
                        readCmd.CommandText = $"SELECT * FROM [{tbl}];";
                        await using var reader = await readCmd.ExecuteReaderAsync();

                        using var bulk = new SqlBulkCopy(localConn, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls, null)
                        {
                            DestinationTableName = $"[{tbl}]",
                            BulkCopyTimeout = 120
                        };

                        for (int c = 0; c < reader.FieldCount; c++)
                        {
                            string colName = reader.GetName(c);
                            bulk.ColumnMappings.Add(colName, colName);
                        }

                        await bulk.WriteToServerAsync(reader);

                        tablesRestored++;
                        totalRows += count;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Restore {tbl} from cloud warning] {ex.Message}");
                    }
                }

                // 4. Re-enable foreign keys
                progress?.Report("Finalizing local database constraints...");
                await using (var enableCmd = localConn.CreateCommand())
                {
                    enableCmd.CommandText = @"
                        DECLARE @sql NVARCHAR(MAX) = N'';
                        SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL; '
                        FROM sys.tables t
                        JOIN sys.schemas s ON t.schema_id = s.schema_id;
                        IF LEN(@sql) > 0 EXEC sp_executesql @sql;";
                    try { await enableCmd.ExecuteNonQueryAsync(); } catch { }
                }

                // 5. Audit log
                await CrmDataService.LogAuditAsync(
                    "DATABASE_CLOUD_RESTORE_COMPLETED",
                    $"Successfully restored local database from MonsterASP Cloud ({CloudServer}). Restored {tablesRestored} tables, {totalRows} total records."
                );

                progress?.Report("Cloud restore completed successfully!");
                return new CloudBackupResult(true, $"Successfully restored from MonsterASP Cloud! ({totalRows} records across {tablesRestored} tables)", tablesRestored, totalRows, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await CrmDataService.LogAuditAsync(
                    "DATABASE_CLOUD_RESTORE_FAILED",
                    $"Failed to restore from MonsterASP Cloud: {ex.Message}"
                );
                return new CloudBackupResult(false, $"Cloud restore failed: {ex.Message}", 0, 0, DateTime.UtcNow, ex.Message);
            }
        }
    }
}

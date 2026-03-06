using System.Text.Json;
using Microsoft.Data.Sqlite;
using OverSync.Contracts;
using OverSync.Core.Abstractions;
using OverSync.Core.Models;

namespace OverSync.Core.State;

public sealed class SqliteSyncStateStore : ISyncStateStore
{
    private string? _connectionString;

    public async Task InitializeAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={databasePath}";
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var commands = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS sync_cursor (
                vault_id TEXT PRIMARY KEY,
                version INTEGER NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS pending_changes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                vault_id TEXT NOT NULL,
                path TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                UNIQUE(vault_id, path)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS tracked_files (
                vault_id TEXT NOT NULL,
                path TEXT NOT NULL,
                hash TEXT NOT NULL,
                version INTEGER NOT NULL,
                last_modified_utc TEXT NOT NULL,
                chunk_refs_json TEXT NOT NULL,
                PRIMARY KEY (vault_id, path)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS conflicts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                vault_id TEXT NOT NULL,
                path TEXT NOT NULL,
                local_hash TEXT NOT NULL,
                remote_hash TEXT NOT NULL,
                resolution TEXT NOT NULL,
                created_at_utc TEXT NOT NULL
            );
            """
        };

        foreach (var sql in commands)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<long?> GetSyncCursorAsync(Guid vaultId, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM sync_cursor WHERE vault_id = $vaultId;";
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is long version ? version : null;
    }

    public async Task SetSyncCursorAsync(Guid vaultId, long version, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO sync_cursor(vault_id, version)
            VALUES($vaultId, $version)
            ON CONFLICT(vault_id) DO UPDATE SET version = excluded.version;
            """;
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        command.Parameters.AddWithValue("$version", version);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task QueuePendingChangeAsync(Guid vaultId, string relativePath, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO pending_changes(vault_id, path, created_at_utc)
            VALUES($vaultId, $path, $createdAtUtc);
            """;
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        command.Parameters.AddWithValue("$path", relativePath);
        command.Parameters.AddWithValue("$createdAtUtc", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> DequeuePendingChangesAsync(
        Guid vaultId,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText =
                """
                SELECT id, path
                FROM pending_changes
                WHERE vault_id = $vaultId
                ORDER BY id
                LIMIT $maxItems;
                """;
            select.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
            select.Parameters.AddWithValue("$maxItems", maxItems);

            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            var ids = new List<long>();
            while (await reader.ReadAsync(cancellationToken))
            {
                ids.Add(reader.GetInt64(0));
                results.Add(reader.GetString(1));
            }

            if (ids.Count > 0)
            {
                await using var delete = connection.CreateCommand();
                delete.Transaction = transaction;
                delete.CommandText =
                    $"DELETE FROM pending_changes WHERE id IN ({string.Join(",", ids)});";
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return results;
    }

    public async Task<int> GetPendingCountAsync(Guid vaultId, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM pending_changes WHERE vault_id = $vaultId;";
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpsertTrackedFileAsync(Guid vaultId, TrackedFileState file, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO tracked_files(vault_id, path, hash, version, last_modified_utc, chunk_refs_json)
            VALUES($vaultId, $path, $hash, $version, $lastModifiedUtc, $chunkRefsJson)
            ON CONFLICT(vault_id, path) DO UPDATE SET
                hash = excluded.hash,
                version = excluded.version,
                last_modified_utc = excluded.last_modified_utc,
                chunk_refs_json = excluded.chunk_refs_json;
            """;
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        command.Parameters.AddWithValue("$path", file.Path);
        command.Parameters.AddWithValue("$hash", file.Hash);
        command.Parameters.AddWithValue("$version", file.Version);
        command.Parameters.AddWithValue("$lastModifiedUtc", file.LastModifiedUtc.ToString("O"));
        command.Parameters.AddWithValue("$chunkRefsJson", JsonSerializer.Serialize(file.Chunks));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TrackedFileState?> GetTrackedFileAsync(
        Guid vaultId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT path, hash, version, last_modified_utc, chunk_refs_json
            FROM tracked_files
            WHERE vault_id = $vaultId AND path = $path;
            """;
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        command.Parameters.AddWithValue("$path", relativePath);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TrackedFileState(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt64(2),
            DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DeserializeChunkRefs(reader.GetString(4)));
    }

    public async Task<IReadOnlyDictionary<string, TrackedFileState>> GetTrackedFilesAsync(
        Guid vaultId,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, TrackedFileState>(StringComparer.OrdinalIgnoreCase);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT path, hash, version, last_modified_utc, chunk_refs_json
            FROM tracked_files
            WHERE vault_id = $vaultId;
            """;
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var file = new TrackedFileState(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt64(2),
                DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
                DeserializeChunkRefs(reader.GetString(4)));
            result[file.Path] = file;
        }

        return result;
    }

    public async Task SaveConflictAsync(Guid vaultId, ConflictDto conflict, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO conflicts(vault_id, path, local_hash, remote_hash, resolution, created_at_utc)
            VALUES($vaultId, $path, $localHash, $remoteHash, $resolution, $createdAtUtc);
            """;
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        command.Parameters.AddWithValue("$path", conflict.Path);
        command.Parameters.AddWithValue("$localHash", conflict.LocalHash);
        command.Parameters.AddWithValue("$remoteHash", conflict.RemoteHash);
        command.Parameters.AddWithValue("$resolution", conflict.Resolution);
        command.Parameters.AddWithValue("$createdAtUtc", conflict.CreatedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> GetConflictCountAsync(Guid vaultId, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM conflicts WHERE vault_id = $vaultId;";
        command.Parameters.AddWithValue("$vaultId", vaultId.ToString("D"));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private SqliteConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("State store has not been initialized.");
        }

        return new SqliteConnection(_connectionString);
    }

    private static IReadOnlyList<ChunkRefDto> DeserializeChunkRefs(string json)
    {
        var refs = JsonSerializer.Deserialize<List<ChunkRefDto>>(json);
        return refs ?? [];
    }
}

namespace OverSync.Contracts;

public sealed record ChunkRefDto(
    string Hash,
    long Size,
    int Index);

public sealed record FileEntryDto(
    string Path,
    string Hash,
    long Size,
    DateTime LastModifiedUtc,
    long Version,
    IReadOnlyList<ChunkRefDto> Chunks);

public sealed record ManifestDto(
    Guid VaultId,
    long Version,
    DateTime GeneratedAtUtc,
    IReadOnlyList<FileEntryDto> Files);

public sealed record UploadManifestRequestDto(ManifestDto Manifest);

public sealed record CommitRequestDto(
    Guid VaultId,
    string DeviceId,
    long Version);

public sealed record DeviceRegistrationRequestDto(
    Guid VaultId,
    string DeviceName,
    string Platform);

public sealed record DeviceDto(
    string DeviceId,
    Guid VaultId,
    string DeviceName,
    string Platform,
    DateTime LastSeenUtc);

public sealed record SyncStatusDto(
    bool IsRunning,
    bool IsSyncing,
    DateTime? LastSyncUtc,
    int QueueLength,
    int ConflictCount,
    int ConnectedDevices);

public sealed record SyncProgressDto(
    int ProcessedFiles,
    int TotalFiles,
    long UploadedBytes,
    long DownloadedBytes);

public sealed record ConflictDto(
    string Path,
    string LocalHash,
    string RemoteHash,
    string Resolution,
    DateTime CreatedAtUtc);

public sealed record RecentActivityDto(
    string Path,
    string Action,
    DateTime TimestampUtc);

using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OverSync.Contracts;
using OverSync.Core.Abstractions;
using OverSync.Core.Models;
using OverSync.Core.Services;
using OverSync.Windows.Services;

namespace OverSync.Windows.ViewModels;

public sealed partial class OnboardingViewModel : ObservableObject
{
    private readonly ISyncApiClient _apiClient;
    private readonly ISecretStore _secretStore;
    private readonly CryptoService _cryptoService;
    private readonly SyncOrchestratorService _orchestrator;
    private readonly AppSessionState _session;

    [ObservableProperty]
    private string _vaultPath = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _passphrase = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public OnboardingViewModel(
        ISyncApiClient apiClient,
        ISecretStore secretStore,
        CryptoService cryptoService,
        SyncOrchestratorService orchestrator,
        AppSessionState session)
    {
        _apiClient = apiClient;
        _secretStore = secretStore;
        _cryptoService = cryptoService;
        _orchestrator = orchestrator;
        _session = session;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(VaultPath) || !Directory.Exists(VaultPath))
        {
            ErrorMessage = "Vault path does not exist.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Passphrase))
        {
            ErrorMessage = "Email, password and passphrase are required.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            AuthTokenDto tokens;
            try
            {
                tokens = await _apiClient.RegisterAsync(new RegisterRequestDto(Email, Password));
            }
            catch
            {
                tokens = await _apiClient.LoginAsync(new LoginRequestDto(Email, Password));
            }

            var vaultId = Guid.NewGuid();
            var deviceId = $"win-{Guid.NewGuid():N}";
            var salt = RandomNumberGenerator.GetBytes(16);
            var vaultKey = await _cryptoService.DeriveVaultKeyAsync(Passphrase, salt);
            await _secretStore.SaveVaultKey(vaultId, vaultKey);

            await _apiClient.RegisterDeviceAsync(
                new DeviceRegistrationRequestDto(vaultId, Environment.MachineName, "windows"),
                tokens.AccessToken);

            var stateDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OverSync",
                "State");
            Directory.CreateDirectory(stateDirectory);
            var stateDbPath = Path.Combine(stateDirectory, $"{vaultId:D}.db");

            var config = new VaultConfig(
                vaultId,
                VaultPath,
                deviceId,
                Environment.MachineName,
                "windows",
                _session.ApiBaseUrl,
                tokens.AccessToken,
                tokens.RefreshToken,
                Passphrase,
                salt,
                stateDbPath,
                TimeSpan.FromSeconds(30));

            _session.Tokens = tokens;
            await _orchestrator.StartAsync(config);
            _session.Log($"Onboarding completed for vault {vaultId:D}");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

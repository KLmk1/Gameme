using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkService : MonoBehaviour
{
    public static NetworkService Instance { get; private set; }

    [SerializeField] private string lobbySceneName = "Lobby"; // Название сцены лобби

    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Сохраняем менеджер между сценами
    }

    // Инициализация сервисов Unity
    public async Task<bool> InitializeAsync()
    {
        if (isInitialized) return true;

        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"Авторизован анонимно. PlayerID: {AuthenticationService.Instance.PlayerId}");
                }
            }
            isInitialized = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка инициализации Unity Services: {e.Message}");
            return false;
        }
    }

    // Логика создания комнаты (Хост)
    public async Task<string> CreateRoomAsync(int maxPlayers = 10)
    {
        if (!await InitializeAsync()) return null;

        try
        {
            // Создаем выделение в Relay
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Настраиваем транспорт
            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Запускаем хост
            NetworkManager.Singleton.StartHost();

            // Переключаем сцену сетевым менеджером
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Ошибка создания комнаты Relay: {e.Message}");
            return null;
        }
    }

    // Логика подключения к комнате (Клиент)
    public async Task<bool> JoinRoomAsync(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("Код подключения не может быть пустым!");
            return false;
        }

        if (!await InitializeAsync()) return false;

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Запускаем клиент
            return NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Ошибка подключения по коду Relay: {e.Message}");
            return false;
        }
    }
}
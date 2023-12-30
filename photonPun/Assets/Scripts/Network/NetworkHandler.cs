using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkHandler : NetworkBehaviour
{
    public static NetworkHandler Instance;
    [SerializeField] private NetworkRunner networkRunnerPrefab = null;
    private Dictionary<PlayerRef, NetworkObject> playerList = new Dictionary<PlayerRef, NetworkObject>();
    NetworkRunner networkRunner;
    CharacterInputHandler characterInputHandler;

    [SerializeField]
    private GameMode gameMode;


    public EnemyHandler enemyHandler;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
            Instance = this;

        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        networkRunner = Instantiate(networkRunnerPrefab);
        networkRunner.name = "Network runner";

        var clientTask = InitializeNetworkRunner(networkRunner, gameMode, GameManager.Instance.GetConnectionToken(), NetAddress.Any(), SceneManager.GetActiveScene().buildIndex, null);

        Debug.Log($"Server NetworkRunner started");
    }

    public void SpawnEnemy(NetworkRunner runner)
    {
        runner.Spawn(enemyHandler, Vector3.up * 2, Quaternion.identity);
    }

    public void StartHostMigration(HostMigrationToken hostMigrationToken)
    {
        networkRunner = Instantiate(networkRunnerPrefab);
        networkRunner.name = "Network runner";

        var clientTask = InitializeNetworkRunnerHostMigration(networkRunner, hostMigrationToken);

        Debug.Log($"Host migration started");
    }

    protected virtual Task InitializeNetworkRunner(NetworkRunner runner, GameMode gameMode, byte[] connectionToken, NetAddress address, SceneRef scene, Action<NetworkRunner> initilized)
    {
        var sceneManager = GetSceneManager(networkRunner);
        networkRunner.ProvideInput = true;                          //告知runner 是否提供 input

        return networkRunner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            Address = address,
            Scene = scene,
            SessionName = "Fusion Room",
            Initialized = initilized,
            SceneManager = sceneManager,
            ConnectionToken = connectionToken,
        });
    }

    public Task InitializeNetworkRunnerHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        var sceneManager = GetSceneManager(networkRunner);
        networkRunner.ProvideInput = true;                          //告知runner 是否提供 input

        return networkRunner.StartGame(new StartGameArgs
        {
            // GameMode = gameMode,
            // Address = address,
            // Scene = scene,
            // SessionName = "Fusion Room",
            // Initialized = initilized,
            SceneManager = sceneManager,
            HostMigrationToken = hostMigrationToken,// cointains all necessary Info to restart the Runner
            HostMigrationResume = HostMigrationResume, // this will be Invoked to resume the simulation
            ConnectionToken = GameManager.Instance.GetConnectionToken(),
        });
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (characterInputHandler == null && NetworkPlayer.Local != null)
        {
            characterInputHandler = NetworkPlayer.Local.GetComponent<CharacterInputHandler>();
        }

        if (characterInputHandler != null)
        {
            var data = characterInputHandler.GetNetworkInputData();
            input.Set(data);
        }
    }

    private INetworkSceneManager GetSceneManager(NetworkRunner runner)
    {
        var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();

        if (sceneManager == null)
        {
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        return sceneManager;
    }

    private void HostMigrationResume(NetworkRunner runner)
    {
        Debug.Log("$HostMigration started");

        //Get a reference for each Network object from the old host
        foreach (var resumeNetworkObject in runner.GetResumeSnapshotNetworkObjects())
        {
            //Grab all the player object, they have a NetworkCharacterControllerPrototypeCustom
            if (resumeNetworkObject.TryGetBehaviour<NetworkCharacterControllerPrototypeCostum>(out var characterController))
            {
                runner.Spawn(resumeNetworkObject, position: characterController.ReadPosition(), rotation: characterController.ReadRotation(),
                    onBeforeSpawned: (runner, newNetworkObject) =>
                    {
                        newNetworkObject.CopyStateFrom(resumeNetworkObject);

                        //Copy info stats from old Behaviour to new behaviour
                        if (resumeNetworkObject.TryGetBehaviour<HPHandler>(out HPHandler oldHPHandler))
                        {
                            HPHandler newHPHandler = newNetworkObject.GetComponent<HPHandler>();
                            newHPHandler.CopyStateFrom(oldHPHandler);
                            newHPHandler.skipSettingStartValues = true;
                        }

                        //Map the connection token with tje new network player
                        if (resumeNetworkObject.TryGetBehaviour<NetworkPlayer>(out var oldNewPlayer))
                        {
                            //Store Player token for reconnection
                            FindObjectOfType<Spawner>().SetConnectionTokenMapping(oldNewPlayer.token, newNetworkObject.GetComponent<NetworkPlayer>());
                        }
                    });
            }
        }

        StartCoroutine(CleanUpHostMigrationCO());

        Debug.Log("$HostMigration completed");
    }

    private IEnumerator CleanUpHostMigrationCO()
    {
        yield return new WaitForSeconds(5.0f);

        FindObjectOfType<Spawner>().OnHostMigrationCleanUp();
    }
}

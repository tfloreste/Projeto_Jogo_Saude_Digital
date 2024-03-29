using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class DataPersistenceManager : MonoBehaviour
{
    [Header("Debugging")]
    [SerializeField] private bool disableDataPersistence = false;
    [SerializeField] private bool initializeDataIfNull = false;
    [SerializeField] private bool overrideSelectedProfileId = false;
    [SerializeField] private string testSelectedProfileId = "test";

    [Header("File Storage Config")]
    [SerializeField] private string fileName;
    [SerializeField] private bool useEncryption;

    //[Header("Auto Saving Configuration")]
    //[SerializeField] private float autoSaveTimeSeconds = 60f;

    private GameData gameData;
    private List<IDataPersistence> dataPersistenceObjects;
    private FileDataHandler dataHandler;
    private GameLoadedMode loadedMode = GameLoadedMode.NORMAL;

    private string standardProfileId = "save";
    private string selectedProfileId;

    //private Coroutine autoSaveCoroutine;

    public static DataPersistenceManager Instance { get; private set; }
    public bool UseEncryption { get => useEncryption; private set => useEncryption = value; }
    public GameLoadedMode LoadedMode { get => loadedMode; private set => loadedMode = value; }

    private void Awake() 
    {
        if (Instance != null) 
        {
            Debug.Log("Found more than one Data Persistence Manager in the scene. Destroying the newest one.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        if (disableDataPersistence) 
        {
            Debug.LogWarning("Data Persistence is currently disabled!");
        }

        Debug.Log("setting standardProfileId");
        selectedProfileId = standardProfileId;
        SetDefaultDataHandler();

        InitializeSelectedProfileId();
    }

    private void OnEnable() 
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() 
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetDataHander(FileDataHandler dataHandler)
    {
        this.dataHandler = dataHandler;
    }

    public void SetDefaultDataHandler()
    {
        this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, useEncryption);
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
    {
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();

        // start up the auto saving coroutine
        /*if (autoSaveCoroutine != null) 
        {
            StopCoroutine(autoSaveCoroutine);
        }
        autoSaveCoroutine = StartCoroutine(AutoSave());*/
    }

    public void ChangeSelectedProfileId(string newProfileId) 
    {
        // update the profile to use for saving and loading
        this.selectedProfileId = newProfileId;
        // load the game, which will use that profile, updating our game data accordingly
        LoadGame();
    }

    public void SetDefaultProfileId()
    {
        string profileId = overrideSelectedProfileId ?
            testSelectedProfileId :
            standardProfileId;

        ChangeSelectedProfileId(profileId);
    }

    public void SetGameLoadedMode(GameLoadedMode gameLoadedMode)
    {
        loadedMode = gameLoadedMode;
    }

    public void ReturnDefaultSettings()
    {
        SetDefaultDataHandler();
        SetDefaultProfileId();
        SetGameLoadedMode(GameLoadedMode.NORMAL);
    }

    public void DeleteProfileData(string profileId, bool callLoadGame) 
    {
        // delete the data for this profile id
        dataHandler.Delete(profileId);
        // initialize the selected profile id
        InitializeSelectedProfileId();
        // reload the game so that our data matches the newly selected profile id
        if(callLoadGame)
            LoadGame();
    }

    public void DeleteCurrentProfileData()
    {
        if (this.selectedProfileId == "")
            return;

        this.DeleteProfileData(this.selectedProfileId, false);
    }

    private void InitializeSelectedProfileId() 
    {
        //this.selectedProfileId = dataHandler.GetMostRecentlyUpdatedProfileId();
        if (overrideSelectedProfileId) 
        {
            this.selectedProfileId = testSelectedProfileId;
            Debug.LogWarning("Overrode selected profile id with test id: " + testSelectedProfileId);
        }
    }

    public void NewGame() 
    {
        this.gameData = new GameData();
    }

    public void LoadGame()
    {
        // return right away if data persistence is disabled
        if (disableDataPersistence) 
        {
            return;
        }

        // load any saved data from a file using the data handler
        this.gameData = dataHandler.Load(selectedProfileId);

        // start a new game if the data is null and we're configured to initialize data for debugging purposes
        if (this.gameData == null && initializeDataIfNull) 
        {
            NewGame();
        }

        // if no data can be loaded, don't continue
        if (this.gameData == null) 
        {
            Debug.Log("No data was found. A New Game needs to be started before data can be loaded.");
            return;
        }

        // push the loaded data to all other scripts that need it
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) 
        {
            dataPersistenceObj.LoadData(gameData);
        }
    }

    public void SaveGame()
    {
        // return right away if data persistence is disabled
        if (disableDataPersistence || loadedMode == GameLoadedMode.GALLERY) 
        {
            return;
        }

        // if we don't have any data to save, log a warning here
        if (this.gameData == null) 
        {
            Debug.LogWarning("No data was found. A New Game needs to be started before data can be saved.");
            return;
        }

        // pass the data to other scripts so they can update it
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) 
        {
            dataPersistenceObj.SaveData(gameData);
        }

        // timestamp the data so we know when it was last saved
        gameData.lastUpdated = System.DateTime.Now.ToBinary();

        // save that data to a file using the data handler
        dataHandler.Save(gameData, selectedProfileId);
    }

    /*private void OnApplicationQuit() 
    {
        SaveGame();
    }*/

    private List<IDataPersistence> FindAllDataPersistenceObjects() 
    {
        // FindObjectsofType takes in an optional boolean to include inactive gameobjects
        IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsOfType<MonoBehaviour>(true)
            .OfType<IDataPersistence>();

        return new List<IDataPersistence>(dataPersistenceObjects);
    }

    public bool HasGameData() 
    {
        return gameData != null;
    }

    public GameData GetGameData()
    {
        return gameData;
    }

    public GameData GetCurrentGameState()
    {
        if (this.gameData == null)
        {
            return null;
        }

        GameData currentGameData = GameData.CopyGameData(gameData);

        // Realizar um "fakeSave" para pegar o estado mais atual
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
        {
            dataPersistenceObj.SaveData(currentGameData);
        }

        PlayerSwipeController playerController = FindObjectOfType<PlayerSwipeController>();
        if(playerController)
        {
            Transform playerTransform = playerController.gameObject.transform;
            currentGameData.playerPosition = playerTransform.position;
        }
        
        return currentGameData;
    }

    public Dictionary<string, GameData> GetAllProfilesGameData() 
    {
        return dataHandler.LoadAllProfiles();
    }

    /*private IEnumerator AutoSave() 
    {
        while (true) 
        {
            yield return new WaitForSeconds(autoSaveTimeSeconds);
            SaveGame();
            Debug.Log("Auto Saved Game");
        }
    }*/
}

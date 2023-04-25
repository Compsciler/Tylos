using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using static GameStats;

[RequireComponent(typeof(PlayerArmies))]
public class MyPlayer : NetworkBehaviour
{
    [SerializeField] Transform cameraTransform;
    public Transform CameraTransform => cameraTransform;

    // Is currently unused and set to the connectionId, but could later be used for team modes (would have to refactor connectionToClient.connectionId)
    int playerId = -1;

    GameStats stats = new GameStats();
    [SerializeField] int BaseCreationCost = 5;
    List<Army> myArmies = new List<Army>();
    public List<Army> MyArmies => myArmies;
    List<Base> myBases = new List<Base>();
    public List<Base> MyBases => myBases;

    [SerializeField] public Material fog;
    [SerializeField] public Material mergeMaterial;

    [SerializeField] public int fogResolutionX = 32;
    [SerializeField] public int fogResolutionY = 32;
    [SerializeField] public int mergeResolutionX = 32;
    [SerializeField] public int mergeResolutionY = 32;
    [SerializeField] public float viewDistance = 4;
    [SerializeField] private float holdDuration = 1f; // Adjust this value to set the required hold duration
    private bool isHoldingKey = false;
    Texture2D fogTex;
    Texture2D mergeTex;
    PlayerArmies myPlayerArmies;
    ObjectIdentity playerIdentity;
    Color teamColor = new Color();
    public Color TeamColor => teamColor;
    private Controls controls;
    private AudioSource audioSource;


    [SyncVar(hook = nameof(AuthorityHandlePartyOwnerStateUpdated))]
    bool isPartyOwner = false;
    public bool IsPartyOwner => isPartyOwner;

    [SyncVar(hook = nameof(ClientHandleDisplayNameUpdated))]
    string displayName;
    public string DisplayName => displayName;

    public static event Action ClientOnInfoUpdated;
    public static event Action<bool> AuthorityOnPartyOwnerStateUpdated;

    public static event Action<Base> ServerOnPlayerHandledBaseDespawned;


    void Awake()
    {
        myPlayerArmies = GetComponent<PlayerArmies>();
        audioSource = GetComponent<AudioSource>();
    }

    #region Server

    public override void OnStartServer()
    {
        playerId = connectionToClient.connectionId;

        Army.ServerOnArmySpawned += ServerHandleArmySpawned;
        Army.ServerOnArmyDespawned += ServerHandleArmyDespawned;
        Base.ServerOnBaseSpawned += ServerHandleBaseSpawned;
        Base.ServerOnBaseDespawned += ServerHandleBaseDespawned;

        DontDestroyOnLoad(gameObject);
    }

    public override void OnStopServer()
    {
        Army.ServerOnArmySpawned -= ServerHandleArmySpawned;
        Army.ServerOnArmyDespawned -= ServerHandleArmyDespawned;
        Base.ServerOnBaseSpawned -= ServerHandleBaseSpawned;
        Base.ServerOnBaseDespawned -= ServerHandleBaseDespawned;
    }

    [Server]
    public void SetDisplayName(string displayName)
    {
        this.displayName = displayName;
    }

    [Server]
    public void SetPartyOwner(bool state)
    {
        isPartyOwner = state;
    }

    [Server]
    public void SetTeamColor(Color color)
    {
        teamColor = color;
    }

    [Command]
    public void CmdStartGame()
    {
        if (!isPartyOwner) { return; }

        ((MyNetworkManager)NetworkManager.singleton).StartGame();
    }

    private void ServerHandleArmySpawned(Army army)
    {
        if (army.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myArmies.Add(army);
        stats.AddUnitsCreated(connectionToClient.connectionId, army.ArmyUnits.Count);
        // myPlayerArmies.AddUnitToNewArmy(unit);
        // Adding and removing units from armies is done in the Army class
    }

    private void ServerHandleArmyDespawned(Army army)
    {
        if (army.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myArmies.Remove(army);
        // myPlayerArmies.RemoveUnitFromArmy(unit);
        // Adding and removing units from armies is done in the Army class
    }

    private void ServerHandleBaseSpawned(Base base_)
    {
        if (base_.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBases.Add(base_);
        stats.AddBaseCreated(connectionToClient.connectionId);
    }

    private void ServerHandleBaseDespawned(Base base_)
    {
        if (base_.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBases.Remove(base_);
        stats.AddBaseDestroyed(connectionToClient.connectionId);
        ServerOnPlayerHandledBaseDespawned?.Invoke(base_);
    }

    #endregion

    #region Client

    public override void OnStartAuthority()  // Start() for objects the client owns (equivalent to OnStartClient() with !isOwned guard)
    {
        setupFog();
        if (NetworkServer.active) { return; }  // Return if this is running as the server (before isClientOnly is set)

        Army.AuthorityOnArmySpawned += AuthorityHandleArmySpawned;
        Army.AuthorityOnArmyDespawned += AuthorityHandleArmyDespawned;
        Base.AuthorityOnBaseSpawned += AuthorityHandleBaseSpawned;
        Base.AuthorityOnBaseDespawned += AuthorityHandleBaseDespawned;
    }
    private void setupFog()
    {
        fogTex = new Texture2D(fogResolutionX, fogResolutionY);
        fogTex.wrapModeU = TextureWrapMode.Clamp;
        fogTex.wrapModeV = TextureWrapMode.Clamp;
        fog.SetTexture("FogTex", fogTex);

        mergeTex = new Texture2D(mergeResolutionX, mergeResolutionY);
        mergeTex.wrapModeU = TextureWrapMode.Clamp;
        mergeTex.wrapModeV = TextureWrapMode.Clamp;
        mergeMaterial.SetTexture("UnitTex", mergeTex);
        mergeMaterial.SetColor("FillColor", new Color(0, 1, 1, 1));
    }


    bool has_centered = false;
    [ClientCallback]
    void Update()
    {
        if (!isOwned) return;
        if (myBases.Count > 0 && !has_centered)
        {
            GetComponent<CameraController>().set_center(myBases[0].transform.position);
            has_centered = true;
        }
        if (fogTex == null || mergeTex == null)
        {
            return;
        }
        float[] fogVals = new float[fogResolutionX * fogResolutionY];
        float[] mergeVals = new float[mergeResolutionX * mergeResolutionY];
        for (int i = 0; i < fogResolutionX * fogResolutionY; i++)
        {
            fogVals[i] = 1;
        }

        foreach (Army army in myArmies)
        {
            for (int y = 0; y < fogResolutionY; y++)
            {
                for (int x = 0; x < fogResolutionX; x++)
                {
                    float game_x = -(((float)(x + 0.5) / fogResolutionX) * 20 - 10);
                    float game_y = -(((float)(y + 0.5) / fogResolutionY) * 20 - 10);
                    double d_x = army.transform.position.x - game_x;
                    double d_y = army.transform.position.z - game_y;
                    double dist = Math.Sqrt(d_x * d_x + d_y * d_y) / viewDistance / 2;
                    fogVals[x + y * fogResolutionX] = Math.Min(fogVals[x + y * fogResolutionX], (float)dist);
                }
            }

            float scale = army.transform.localScale.x;

            for (int y = 0; y < mergeResolutionY; y++)
            {
                for (int x = 0; x < mergeResolutionX; x++)
                {
                    float game_x = -(((float)(x + 0.5) / mergeResolutionX) * 20 - 10);
                    float game_y = -(((float)(y + 0.5) / mergeResolutionY) * 20 - 10);
                    double d_x = army.transform.position.x - game_x;
                    double d_y = army.transform.position.z - game_y;
                    double dist = Math.Sqrt(d_x * d_x + d_y * d_y);
                    double sdf_height = ((((scale - dist) - 1) * 0.8) + 1) * 0.6;
                    mergeVals[x + y * mergeResolutionX] += (float)Math.Clamp(sdf_height, 0, 0.499);
                }
            }
        }



        foreach (Base b in myBases)
        {
            for (int y = 0; y < fogResolutionY; y++)
            {
                for (int x = 0; x < fogResolutionX; x++)
                {
                    float game_x = -(((float)x / fogResolutionX) * 20 - 10);
                    float game_y = -(((float)y / fogResolutionY) * 20 - 10);
                    double d_x = b.transform.position.x - game_x;
                    double d_y = b.transform.position.z - game_y;
                    double dist = Math.Sqrt(d_x * d_x + d_y * d_y) / viewDistance / 2;
                    fogVals[x + y * fogResolutionX] = Math.Min(fogVals[x + y * fogResolutionX], (float)dist);
                }
            }
        }

        for (int y = 0; y < fogResolutionY; y++)
        {
            for (int x = 0; x < fogResolutionX; x++)
            {
                fogTex.SetPixel(x, y, new Color(fogVals[x + y * fogResolutionX], 0, 0, 0));
            }
        }
        for (int y = 0; y < mergeResolutionY; y++)
        {
            for (int x = 0; x < mergeResolutionX; x++)
            {
                mergeTex.SetPixel(x, y, new Color(mergeVals[x + y * mergeResolutionX], teamColor.r, teamColor.g, teamColor.b));
            }
        }
        fogTex.Apply(true, false);
        mergeTex.Apply(true, false);
    }
    public override void OnStartClient()
    {
        controls = new Controls();
        controls.Player.MakeBase.performed += makeBase;
        controls.Enable();

        if (NetworkServer.active) { return; }

        DontDestroyOnLoad(gameObject);

        ((MyNetworkManager)NetworkManager.singleton).Players.Add(this);
    }

    public override void OnStopClient()  // OnStopAuthority() is only called when authority is removed, which can happen even if the object is not destroyed
    {
        ClientOnInfoUpdated?.Invoke();

        if (!isClientOnly) { return; }  //  Return this is not the server

        ((MyNetworkManager)NetworkManager.singleton).Players.Remove(this);

        if (!isOwned) { return; }  // Return if not owned by this client

        Army.AuthorityOnArmySpawned -= AuthorityHandleArmySpawned;
        Army.AuthorityOnArmyDespawned -= AuthorityHandleArmyDespawned;
        Base.AuthorityOnBaseSpawned -= AuthorityHandleBaseSpawned;
        Base.AuthorityOnBaseDespawned -= AuthorityHandleBaseDespawned;
    }

    private void ClientHandleDisplayNameUpdated(string oldDisplayName, string newDisplayName)
    {
        ClientOnInfoUpdated?.Invoke();
    }

    private void AuthorityHandlePartyOwnerStateUpdated(bool oldState, bool newState)
    {
        if (!isOwned) { return; }

        AuthorityOnPartyOwnerStateUpdated?.Invoke(newState);
    }

    private void AuthorityHandleArmySpawned(Army army)  // Necessary?
    {
        myArmies.Add(army);
        // TODO: Add here too?
    }

    private void AuthorityHandleArmyDespawned(Army army)  // Necessary?
    {
        myArmies.Remove(army);
        // TODO: Add here too?
    }

    private void AuthorityHandleBaseSpawned(Base base_)
    {
        myBases.Add(base_);
    }

    private void AuthorityHandleBaseDespawned(Base base_)
    {
        myBases.Remove(base_);
    }

    public void makeBase(InputAction.CallbackContext input)
    {
        List<Army> armies = new List<Army>();
        foreach (Entity e in SelectionHandler.SelectedEntities)
        {
            if (e is Army army)
            {
                armies.Add(army);
            }
        }
        if (armies.Count == 0) return;

        if (input.ReadValue<float>() == 1) // Key pressed
        {
            if (!isHoldingKey) // Only start coroutine if not already holding key
            {
                isHoldingKey = true;
                audioSource.Play();
                setBuildIconOnArmies(true, armies);
                StartCoroutine(HandleBaseCreation());
            }
        }
        else if (input.ReadValue<float>() == 0) // Key released
        {
            isHoldingKey = false;
            audioSource.Stop();
            setBuildIconOnArmies(false, armies);
            StopCoroutine(HandleBaseCreation());
        }
    }


    private IEnumerator HandleBaseCreation()
    {
        yield return new WaitForSeconds(holdDuration);

        if (isHoldingKey)
        {
            List<Army> armies = new List<Army>();
            foreach (Entity e in SelectionHandler.SelectedEntities)
            {
                if ((e is Army army) && (e != null))
                {
                    armies.Add(army);
                    // Check for nearby bases
                    Collider[] colliders = Physics.OverlapSphere(army.transform.position, 0.5f * army.transform.lossyScale.x);
                    bool baseNearby = false;

                    foreach (Collider collider in colliders)
                    {
                        if (collider.GetComponent<Base>() != null)
                        {
                            baseNearby = true;
                            break;
                        }
                    }

                    if (!baseNearby && army.ArmyUnits.Count > BaseCreationCost)
                    {
                        for (int i = 0; i < BaseCreationCost; i++)
                        {
                            army.ArmyUnits.RemoveAt(0);
                        }
                        ((MyNetworkManager)NetworkManager.singleton).MakeBase(this, army.transform.position);
                        audioSource.Stop();
                        setBuildIconOnArmies(false, armies);
                    }
                    else
                    {
                        army.ShowUnableToBuildIcon();
                    }
                }
            }
        }
        audioSource.Stop();
    }

    private void setBuildIconOnArmies(bool active, List<Army> armies)
    {
        foreach (Army army in armies)
        {
            army.SetBuildingIcon(active);
        }
    }

    #endregion
}

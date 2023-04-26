using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Mirror;

public class SelectedUnitsInfoController : MonoBehaviour
{
    [SerializeField] GameObject selectedUnitsInfoGO;

    public static event Action<Army> AuthorityOnArmyInfoOpened;
    public static event Action<Army> AuthorityOnArmyInfoClosed;

    // This should be on a separate script on the selectedUnitsInfoGO GameObject
    [SerializeField] TMP_Text armyColorText;

    [SerializeField] TMP_Text totalUnitCountText;

    Army army;
    ObjectIdentity armyObjectIdentity;

    MyPlayer player;

    void Awake()
    {
        Army.AuthorityOnArmySelected += AuthorityHandleArmySelected;
        Army.AuthorityOnArmyDeselected += AuthorityHandleArmyDeselected;
    }

    void OnDestroy()
    {
        Army.AuthorityOnArmySelected -= AuthorityHandleArmySelected;
        Army.AuthorityOnArmyDeselected -= AuthorityHandleArmyDeselected;
    }

    void Start()
    {
        player = NetworkClient.connection.identity.GetComponent<MyPlayer>();
    }

    void Update()
    {
        int totalUnitCount = player.GetTotalUnitCount();
        totalUnitCountText.text = totalUnitCount.ToString();

        if (army == null) { return; }
        IdentityInfo armyIdentity = armyObjectIdentity.Identity;
        Color armyIdentityColor = armyIdentity.GetColor();
        armyColorText.text = $"#{ColorUtility.ToHtmlStringRGB(armyIdentityColor)}";
    }

    private void AuthorityHandleArmySelected(Army army)
    {
        selectedUnitsInfoGO.SetActive(true);  // TODO
        AuthorityOnArmyInfoOpened?.Invoke(army);

        this.army = army;
        armyObjectIdentity = army.GetComponent<ObjectIdentity>();
    }

    private void AuthorityHandleArmyDeselected(Army army)
    {
        selectedUnitsInfoGO.SetActive(false);  // TODO
        AuthorityOnArmyInfoClosed?.Invoke(army);

        this.army = null;
        armyObjectIdentity = null;
    }
}

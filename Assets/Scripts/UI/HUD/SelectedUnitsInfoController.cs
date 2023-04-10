using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SelectedUnitsInfoController : MonoBehaviour
{
    [SerializeField] GameObject selectedUnitsInfoGO;

    public static event Action<Army> AuthorityOnArmyInfoOpened;
    public static event Action<Army> AuthorityOnArmyInfoClosed;

    // This should be on a separate script on the selectedUnitsInfoGO GameObject
    [SerializeField] TMP_Text armyColorText;

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

    private void AuthorityHandleArmySelected(Army army)
    {
        selectedUnitsInfoGO.SetActive(true);  // TODO
        AuthorityOnArmyInfoOpened?.Invoke(army);

        IdentityInfo armyIdentity = army.GetComponent<ObjectIdentity>().Identity;
        Color armyIdentityColor = armyIdentity.GetColor();
        armyColorText.text = $"#{ColorUtility.ToHtmlStringRGB(armyIdentityColor)}";
    }

    private void AuthorityHandleArmyDeselected(Army army)
    {
        selectedUnitsInfoGO.SetActive(false);  // TODO
        AuthorityOnArmyInfoClosed?.Invoke(army);
    }
}

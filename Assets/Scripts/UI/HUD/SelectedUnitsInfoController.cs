using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedUnitsInfoController : MonoBehaviour
{
    [SerializeField] GameObject selectedUnitsInfoGO;

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
    }

    private void AuthorityHandleArmyDeselected(Army army)
    {
        selectedUnitsInfoGO.SetActive(false);  // TODO
    }
}

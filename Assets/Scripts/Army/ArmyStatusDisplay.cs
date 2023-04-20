using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Army))]
public class ArmyStatusDisplay : MonoBehaviour
{
    [SerializeField] GameObject armyStatusParent;
    [SerializeField] Image armyStatusImage;

    [SerializeField] Sprite armyAttackStatusSprite;
    [SerializeField] Sprite armyConvertStatusSprite;

    Army thisArmy;

    void Awake()
    {
        thisArmy = GetComponent<Army>();

        Army.AuthorityOnArmySelected += AuthorityHandleArmySelected;
        Army.AuthorityOnArmyDeselected += AuthorityHandleArmyDeselected;

        // Army.AuthorityOnArmyModeChanged += AuthorityHandleArmyModeChanged;
    }

    void OnDestroy()
    {
        Army.AuthorityOnArmySelected -= AuthorityHandleArmySelected;
        Army.AuthorityOnArmyDeselected -= AuthorityHandleArmyDeselected;

        // Army.AuthorityOnArmyModeChanged -= AuthorityHandleArmyModeChanged;
    }

    private void AuthorityHandleArmySelected(Army army)
    {
        if (army != thisArmy) return;

        armyStatusParent.SetActive(true);
        // AuthorityHandleArmyModeChanged(army, army.GetMode());
    }

    private void AuthorityHandleArmyDeselected(Army army)
    {
        if (army != thisArmy) return;

        armyStatusParent.SetActive(false);
    }

    private void AuthorityHandleArmyModeChanged(Army army, EntityCommandGiver.Mode mode)
    {
        if (army != thisArmy) return;

        switch (mode)
        {
            case EntityCommandGiver.Mode.Attack:
                armyStatusImage.sprite = armyAttackStatusSprite;
                break;
            case EntityCommandGiver.Mode.Convert:
                armyStatusImage.sprite = armyConvertStatusSprite;
                break;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieChartController : MonoBehaviour
{
    public Image redImage;
    public Image greenImage;
    public Image blueImage;

    Army army;
    ObjectIdentity armyObjectIdentity;

    void Awake()
    {
        SelectedUnitsInfoController.AuthorityOnArmyInfoOpened += AuthorityHandleArmyInfoOpened;
        SelectedUnitsInfoController.AuthorityOnArmyInfoClosed += AuthorityHandleArmyInfoClosed;
    }

    void OnDestroy()
    {
        SelectedUnitsInfoController.AuthorityOnArmyInfoOpened -= AuthorityHandleArmyInfoOpened;
        SelectedUnitsInfoController.AuthorityOnArmyInfoClosed -= AuthorityHandleArmyInfoClosed;
    }

    void Update()
    {
        if (army == null) { return; }
        IdentityInfo armyIdentity = armyObjectIdentity.Identity;
        UpdatePieChart(armyIdentity.r, armyIdentity.g, armyIdentity.b);
    }

    public void UpdatePieChart(float redValue, float greenValue, float blueValue)
    {
        float totalRGB = redValue + greenValue + blueValue;

        if (totalRGB == 0)
        {
            redImage.fillAmount = 0;
            greenImage.fillAmount = 0;
            blueImage.fillAmount = 0;
        }
        else
        {
            redImage.fillAmount = redValue / totalRGB;
            greenImage.fillAmount = greenValue / totalRGB;
            blueImage.fillAmount = blueValue / totalRGB;
        }
        greenImage.fillAmount = greenImage.fillAmount + redImage.fillAmount;
        blueImage.fillAmount = blueImage.fillAmount + greenImage.fillAmount;
    }

    private void AuthorityHandleArmyInfoOpened(Army army)
    {
        this.army = army;
        armyObjectIdentity = army.GetComponent<ObjectIdentity>();
    }

    private void AuthorityHandleArmyInfoClosed(Army army)
    {
        this.army = null;
        armyObjectIdentity = null;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotkeyButtons : MonoBehaviour
{
    [SerializeField] Button attackButton;
    [SerializeField] Button convertButton;

    // If only I had a serializable dictionary
    // [SerializeField] Dictionary<Button, Color> buttonOnColors = new Dictionary<Button, Color>();
    [SerializeField] Color attackButtonOnColor;
    [SerializeField] Color convertButtonOnColor;
    Color offModeColor;

    void Awake()
    {
        EntityCommandGiver.AuthorityOnModeChanged += AuthorityHandleModeChanged;
    }

    void OnDestroy()
    {
        EntityCommandGiver.AuthorityOnModeChanged -= AuthorityHandleModeChanged;
    }

    private void AuthorityHandleModeChanged(EntityCommandGiver.Mode mode)
    {
        switch (mode)
        {
            case EntityCommandGiver.Mode.Attack:
                SetButtonStatus(attackButton, true, attackButtonOnColor);
                SetButtonStatus(convertButton, false, convertButtonOnColor);
                break;
            case EntityCommandGiver.Mode.Convert:
                SetButtonStatus(attackButton, false, attackButtonOnColor);
                SetButtonStatus(convertButton, true, convertButtonOnColor);
                break;
        }
    }

    private void SetButtonStatus(Button button, bool status, Color onModeColor)
    {
        if (status)
        {
            button.GetComponent<RawImage>().color = onModeColor;
        }
        else
        {
            button.GetComponent<RawImage>().color = offModeColor;
        }
    }
}

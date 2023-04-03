using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieChartController : MonoBehaviour
{
    public Image redImage;
    public Image greenImage;
    public Image blueImage;

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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RGBSliderController : MonoBehaviour
{
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    public PieChartController pieChartController;

    private void Start()
    {
        redSlider.onValueChanged.AddListener(SliderChanged);
        greenSlider.onValueChanged.AddListener(SliderChanged);
        blueSlider.onValueChanged.AddListener(SliderChanged);
    }

    public void SliderChanged(float _)
    {
        float redValue = redSlider.value;
        float greenValue = greenSlider.value;
        float blueValue = blueSlider.value;

        pieChartController.UpdatePieChart(redValue, greenValue, blueValue);
    }
}

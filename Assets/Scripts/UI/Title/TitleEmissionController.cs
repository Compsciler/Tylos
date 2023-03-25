using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleEmissionController : MonoBehaviour
{
    public ParticleSystem titleEmission;
    private const float MinSize = 0.3f;
    private const float MaxSize = 1f;
    private const float MinSpeed = 1f;
    private const float MaxSpeed = 2f;

    private ParticleSystem.MainModule _mainModule;

    private void Start()
    {
        _mainModule = titleEmission.main;
    }

    private void Update()
    {
        _mainModule.startSize = Random.Range(MinSize, MaxSize);
        _mainModule.startSpeed = Random.Range(MinSpeed, MaxSpeed);
        _mainModule.startColor = new Color(Random.value, Random.value, Random.value, 1);
    }
}

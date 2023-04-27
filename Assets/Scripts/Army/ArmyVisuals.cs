using UnityEngine;
using Mirror;
using System.Collections.Generic;
/// <summary>
/// Handles attack visuals for an army
/// This component is run entirely on the client
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ArmyVisuals : NetworkBehaviour
{

    [Header("Army visual settings")]
    [SerializeField]
    [Range(0.1f, 10f)]
    private float defaultScale = 1f;

    [SerializeField]
    [Range(0.1f, 2f)]
    private float scaleIncrementPerUnit = 0.1f;

    [SerializeField]
    [Range(2f, 10f)]
    private float maxScale = 10f;

    [Header("Death ray settings")]
    [SerializeField] private float lineDuration = 0.1f;
    private float alpha = 1f;
    [SerializeField]
    [Range(0, 10)]
    [Tooltip("Changes the amount of bloom on the line")]
    private float intensity = 5f;

    // Internal variables
    LineRenderer lineRenderer;
    float durationRemaining = 0f; // Line is drawn if this is greater than 0
    Vector3 targetPosition;
    public int Count { get; private set; } = 0;  // The number of units in the army at last SetScale() call

    // a bit inelegant, bonk me when this becomes a problem - rj

    [SerializeField] private SpriteRenderer armyRenderer;
    private Material armyShaderMat;
    [SerializeField] private SpriteRenderer highlightRenderer;

    private Army army;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        ClearDeathRay();
        army = GetComponent<Army>();
        armyShaderMat = armyRenderer.material;
    }

    void Update()
    {
        if (durationRemaining > 0f)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, targetPosition);
            durationRemaining -= Time.deltaTime;
            if (durationRemaining <= 0f)
            {
                ClearDeathRay();
            }
        }

        // update wrinkle shader
        armyShaderMat.SetColor("_ArmyColor", armyRenderer.color);
        armyShaderMat.SetColor("_HighlightColor", highlightRenderer.color);
        var deviance = army.Deviance;
        // max amplitude 0.15 (look already kinda bumpy)
        // also note that max deviance is 1 (when literally the entire army is split across the ring
        // 0.2 makes it so that a cell that is about to split look kinda bumpy
        armyShaderMat.SetFloat("_Amplitude", deviance * 0.2f);

    }

    #region Server

    /// <summary>
    /// Sets the scale of the army based on the number of units in the army
    /// </summary>
    /// <param name="count">The number of units in the army</param>
    [Server]
    public void SetScale(int count)
    {
        Count = count;
        Vector3 start = gameObject.transform.localScale;
        Vector3 end = (Vector3.one * defaultScale) + (Vector3.one * scaleIncrementPerUnit * count);
        end = Vector3.Min(end, Vector3.one * maxScale); // Cap the max scale
        transform.localScale = end;
    }

    #endregion
    #region Client

    /// <summary>
    /// Sets the color of the army based on the units in the army
    /// </summary>
    /// <param name="armyUnits">
    /// Reference to the army's SyncList of units 
    /// </param>
    [Client]
    public void SetColor(List<Unit> armyUnits)
    {
        if (armyUnits == null)
        {
            Debug.LogError("ArmyVisuals: Army units is null");
            return;
        }

        // Set the color of the line renderer
        Color baseColor = Color.white;
        if (armyUnits.Count > 0)
        {
            float r = 0;
            float g = 0;
            float b = 0;
            // Get the average color of the units
            foreach (Unit unit in armyUnits)
            {
                r += unit.identityInfo.r;
                g += unit.identityInfo.g;
                b += unit.identityInfo.b;
            }

            r /= armyUnits.Count;
            g /= armyUnits.Count;
            b /= armyUnits.Count;
            baseColor = new Color(r, g, b);
        }

        Color finalColor = new Color(baseColor.r * intensity, baseColor.g * intensity, baseColor.b * intensity, alpha);

        lineRenderer.material.SetColor("_Color", finalColor);
    }

    public void DrawDeathRay(Vector3 targetPosition)
    {
        if (!lineRenderer.enabled)
        {
            lineRenderer.enabled = true;
        }

        durationRemaining = lineDuration; //  Reset the duration
        if (this.targetPosition != targetPosition)
        {
            this.targetPosition = targetPosition;
        }
    }

    public void ClearDeathRay()
    {
        // Clear the line renderer
        lineRenderer.enabled = false;
    }

    #endregion
}
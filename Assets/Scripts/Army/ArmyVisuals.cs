using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Handles attack visuals for an army
/// </summary>
public class ArmyVisuals : MonoBehaviour
{
    public void DrawDeathRay(Vector3 targetPosition)
    {
        // Renders a ray from the army to the target position
        Debug.DrawLine(transform.position, targetPosition, Color.red, 1f);
    }
}
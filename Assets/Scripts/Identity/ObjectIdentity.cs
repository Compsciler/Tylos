using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Resisted the urge to name this EntityIdentity
public class ObjectIdentity : MonoBehaviour
{
    float r;
    float g;
    float b;

    public float R => r;
    public float G => g;
    public float B => b;

    // public float R { get => r; set => r = value; }
    // public float G { get => g; set => g = value; }
    // public float B { get => b; set => b = value; }

    public void SetIdentity(float r, float g, float b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }
    public void SetIdentity(Color color)
    {
        SetIdentity(color.r, color.g, color.b);
    }

    public Color GetColorFromIdentity()
    {
        return new Color(r, g, b);
    }
}

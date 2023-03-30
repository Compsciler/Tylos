using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class TestEigen : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var testData = new List<Vector2>
        {
            new Vector2(0, 1),
            new Vector2(0, -1),
            new Vector2(0, 2),
            new Vector2(0, -2)
        };
        Debug.Log(GetEigen(testData));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    Vector2 GetEigen(List<Vector2> list)
    {
        var armyComplex = list;
        
        var xMean = 0f;
        xMean = armyComplex.Aggregate(xMean, (current, c) => current + c.x) / armyComplex.Count;
        var yMean = 0f;
        yMean = armyComplex.Aggregate(yMean, (current, c) => current + c.y) / armyComplex.Count;
        var xx = 0f;
        xx = armyComplex.Aggregate(xx, 
            (current, c) =>
            {
                return current + (c.x - xMean) * (c.x - xMean);
            }) / (armyComplex.Count - 1);
        
        var yy = 0f;
        yy = armyComplex.Aggregate(yy, 
            (current, c) =>
            {
                return current + (c.y - yMean) * (c.y - yMean);
            }) / (armyComplex.Count - 1);
        
        var xy = 0f;
        xy = armyComplex.Aggregate(xy, 
            (current, c) =>
            {
                return current + (c.y - yMean) * (c.x - xMean);
            }) / (armyComplex.Count - 1);
        
        // the cov matrix is [xx, xy; xy, yy]
        // now we calculate the eigenvectors and eigenvalues
        
        var delta = math.sqrt((xx + yy) * (xx + yy) - 4 * (xx * yy - xy * xy));
        var l1 = (xx + yy + delta)/2;
        var l2 = Mathf.Abs(xx + yy - delta) / 2;

        var l = Mathf.Max(l1, l2);
        // the new vector is then
        // [xx - l1, xy; xy - l2, yy]

        var aFactor = xx - l + xy;
        var bFactor = xy + yy - l;

        var eigenVector = new Vector2(bFactor, -aFactor).normalized;
        
        return eigenVector;
    }
}

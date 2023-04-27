using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;

public class ArmyUtils : MonoBehaviour
{
    const float MIN_COLOR_VALUE = 0f;
    const float MAX_COLOR_VALUE = 255f;

    public static float CalculateAttackPower(List<Unit> units, float minUnitAttackDamage, float maxUnitAttackDamage)
    {
        float attackPower = 0f;
        foreach (Unit unit in units)
        {
            float r = unit.identityInfo.r;
            attackPower += Map(r, MIN_COLOR_VALUE, MAX_COLOR_VALUE, minUnitAttackDamage, maxUnitAttackDamage);
        }
        return attackPower;
    }

    public static float CalculateHealth(List<Unit> units)
    {
        float health = 0f;
        foreach (Unit unit in units)
        {
            float g = unit.identityInfo.g;
            health += Map(g, 0f, 255f, 1f, 2f);
        }
        Debug.Log("Army health: " + health);
        return health;
    }

    public static Vector3 CalculateMeanColor(List<Unit> armyUnits)
    {
        // Calculate and set the new mean color
        Vector3 meanColor = Vector3.zero;
        meanColor = Vector3.zero;
        foreach (var u in armyUnits)
        {
            meanColor += new Vector3(u.identityInfo.r, u.identityInfo.g, u.identityInfo.b);
        }
        meanColor /= armyUnits.Count;
        return meanColor;
    }

    public static float CalculateDeviance(List<Unit> armyUnits, Vector3 meanColor)
    {
        var armyIdentityColors = armyUnits.Select
            (i => new Vector3(i.identityInfo.r, i.identityInfo.g, i.identityInfo.b)).ToList();
        if (armyIdentityColors.Count == 0)
        {
            return 0f;
        }
        var mean = meanColor;
        // using squared magnitude because it's like a standard
        // PCA uses another three stdev metric
        // is there a way to unify them to reduce problems?
        var stdev = armyIdentityColors.Sum(c => (c - mean).sqrMagnitude);
        stdev /= armyIdentityColors.Count;
        return stdev;
    }

    public static Vector2 CalculateMeanZ(List<Vector2> armyComplex)
    {
        Vector2 meanZ = Vector2.zero;
        foreach (var z in armyComplex)
        {
            meanZ += z;
        }
        meanZ /= armyComplex.Count;
        return meanZ;
    }

    public static List<Vector2> GetArmyComplex(List<Unit> armyUnits)
    {
        List<Vector2> armyComplex;
        armyComplex = armyUnits.Select(i => i.GetIdentityZ()).ToList();
        return armyComplex;
    }

    /// <summary>
    /// This function calculates the eigenvector as well as the centroid
    /// </summary>
    /// <param name="data"></param>
    /// <param name="meanZ"></param>
    /// <returns></returns>
    public static (Vector2, Vector2) GetEigenCentroid(List<Vector2> data, Vector2 meanZ)
    {
        var xMean = meanZ.x;
        var yMean = meanZ.y;

        var varX = 0f;
        varX = data.Aggregate(varX,
            (current, c) =>
            {
                return current + (c.x - xMean) * (c.x - xMean);
            }) / (data.Count - 1);

        var varY = 0f;
        varY = data.Aggregate(varY,
            (current, c) =>
            {
                return current + (c.y - yMean) * (c.y - yMean);
            }) / (data.Count - 1);

        var covXY = 0f;
        covXY = data.Aggregate(covXY,
            (current, c) =>
            {
                return current + (c.y - yMean) * (c.x - xMean);
            }) / (data.Count - 1);

        // the cov matrix is [varX, covXY; covXY, varY]
        // now we calculate the eigenvectors and eigenvalues

        var delta = math.sqrt((varX + varY) * (varX + varY) - 4 * (varX * varY - covXY * covXY));
        var l1 = (varX + varY + delta) / 2;
        var l2 = Mathf.Abs(varX + varY - delta) / 2;

        var l = Mathf.Max(l1, l2);
        // the new thing to solve is then
        // [varX - l, covXY; covXY, varY - l][a; b] = 0

        var aFactor = varX - l + covXY;
        var bFactor = covXY + varY - l;

        var eigenVector = new Vector2(bFactor, -aFactor).normalized;
        return (eigenVector, new Vector2(xMean, yMean));
    }

    /// <summary>
    /// This function does a PCA on the units identities
    /// and splits the army along the principle axis
    /// the split is returned as a tuple of lists
    /// </summary>
    /// <param name="armyUnits"></param>
    /// <param name="armyComplex"></param>
    /// <param name="meanZ"></param>
    /// <param name="splitThreshold"></param>
    /// <returns>Two lists of Units representing the two splits</returns>
    public static (List<Unit>, List<Unit>) CalculateSplit(List<Unit> armyUnits, List<Vector2> armyComplex, Vector2 meanZ, float splitThreshold)
    {
        // GetEigenCentroid returns (eigenvector, centroid) of the dataset
        var (eigenVector, centroid) = ArmyUtils.GetEigenCentroid(armyComplex, meanZ);

        // if we get a perfectly circular set it is possible for eigen to be 0
        // just randomly pick an axis if so
        if (eigenVector == Vector2.zero)
        {
            var angle = UnityEngine.Random.Range(0, Mathf.PI * 2);
            eigenVector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        // project the centroid onto the eigen
        var centroidProj = Vector2.Dot(centroid, eigenVector);

        var retSplit1 = new List<Unit>();
        var retSplit2 = new List<Unit>();

        for (int i = 0; i < armyComplex.Count; i++)
        {

            if (Vector2.Dot(armyComplex[i], eigenVector) < centroidProj)
            {
                retSplit1.Add(armyUnits[i]);
            }
            else
            {
                retSplit2.Add(armyUnits[i]);
            }
        }

        return (retSplit1, retSplit2);
    }

    public static bool IsInRange(Transform self, Transform target, float range)
    {
        Vector3 offset = target.position - self.position;
        float distance = offset.magnitude;
        float trueAttackRange = range + self.lossyScale.x + target.lossyScale.x; // Takes into account the size of the army and the target

        return (distance <= trueAttackRange);
    }

    // This is a function that maps a value from one range to another
    public static float Map(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        return (value - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
    }
}

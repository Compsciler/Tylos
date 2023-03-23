using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Rename to TeamColorManager later if left as a singleton
public class TeamColorAssigner : Singleton<TeamColorAssigner>
{
    [SerializeField] List<Color> allTeamColorOptions;
    List<Color> availableTeamColors;

    void Awake()
    {
        availableTeamColors = new List<Color>(allTeamColorOptions);
    }

    public Color GetAndRemoveRandomColor()
    {
        if (availableTeamColors.Count == 0)
        {
            Debug.LogError("No more team colors available!");
            return Color.black;
        }
        
        int randomIndex = Random.Range(0, availableTeamColors.Count);
        Color randomColor = availableTeamColors[randomIndex];
        availableTeamColors.RemoveAt(randomIndex);
        return randomColor;
    }
}

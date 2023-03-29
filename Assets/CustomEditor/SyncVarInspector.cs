using UnityEditor;
using UnityEngine;
using Mirror;

/// <summary>
/// Custom inspector for NetworkBehaviour that displays all SyncVars
/// </summary>
[CustomEditor(typeof(NetworkBehaviour), true)]
public class SyncVarInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        NetworkBehaviour networkBehaviour = target as NetworkBehaviour;
        System.Type targetType = networkBehaviour.GetType();
        System.Reflection.FieldInfo[] fields = targetType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            SyncVarAttribute syncVarAttribute = System.Attribute.GetCustomAttribute(field, typeof(SyncVarAttribute)) as SyncVarAttribute;

            if (syncVarAttribute != null)
            {
                EditorGUILayout.LabelField(field.Name, field.GetValue(networkBehaviour).ToString());
            }
        }
    }
}

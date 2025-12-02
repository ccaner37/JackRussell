using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JackRussell.Player))]
public class PlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        JackRussell.Player player = (JackRussell.Player)target;

        if (GUILayout.Button("Trigger Homing Attack Enter"))
        {
            player.SetHomingAttackPlayerMaterial();
        }

        if (GUILayout.Button("Trigger Homing Attack Reach"))
        {
            player.ResetPlayerMaterial();
        }
    }
}
using JackRussell.Rails;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RailMeshGenerator))]
public class RailMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RailMeshGenerator generator = (RailMeshGenerator)target;

        if (GUILayout.Button("Generate Rail Mesh"))
        {
            generator.GenerateRailMesh();
        }
    }
}
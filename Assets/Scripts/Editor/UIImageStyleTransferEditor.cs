using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIImageStyleTransfer))]
public class UIImageStyleTransferEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UIImageStyleTransfer script = (UIImageStyleTransfer)target;
        if (GUILayout.Button("Run Style Transfer"))
        {
            script.StartTransfer();
        }
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VanGoghUIImageTransfer_Replicate))]
public class VanGoghUIImageTransferEditor_Replicate : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VanGoghUIImageTransfer_Replicate script = (VanGoghUIImageTransfer_Replicate)target;
        if (GUILayout.Button("Start Style Transfer"))
        {
            script.StartTransfer();
        }
    }
}

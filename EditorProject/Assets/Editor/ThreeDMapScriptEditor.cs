using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[UnityEditor.CustomEditor(typeof(ThreeDMapScript))]
public class ThreeDMapScriptEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ThreeDMapScript myTarget = (ThreeDMapScript)target;
        if (GUILayout.Button("Generate Map"))
        {
            myTarget.Load();
        }
    }
}

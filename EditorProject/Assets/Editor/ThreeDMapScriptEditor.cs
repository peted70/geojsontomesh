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
            EditorUtility.DisplayProgressBar("Loading Map Data..", "", 0.0f);
            myTarget.Load();
        }
    }
}

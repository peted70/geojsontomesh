using System;
using UnityEditor;
using UnityEngine;
using Interfaces;

[CustomEditor(typeof(ThreeDMapScript))]
public class ThreeDMapScriptEditor : Editor, IProgress, IUpdateHandler, IDialog
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ThreeDMapScript myTarget = (ThreeDMapScript)target;
        if (GUILayout.Button("Generate Map"))
        {
            EditorUtility.DisplayProgressBar("Loading Map Data..", "", 0.0f);
            myTarget.Load(this, this, this);
        }
    }

    public void HookUpdate(ActionDelegate action)
    {
        EditorApplication.update += DelegateUtility.Cast<EditorApplication.CallbackFunction>(action);
    }

    public void UnhookUpdate(ActionDelegate action)
    {
        EditorApplication.update -= DelegateUtility.Cast<EditorApplication.CallbackFunction>(action);
    }

    public void UpdateProgress(string info, float progress)
    {
        EditorUtility.DisplayProgressBar("Loading Map Data..", info, progress);
    }

    public void Clear()
    {
        EditorUtility.ClearProgressBar();
    }

    public void DisplayDialog(string title, string message)
    {
        EditorUtility.DisplayDialog(title, message, "OK");
    }
}

public static class DelegateUtility
{
    public static T Cast<T>(Delegate source) where T : class
    {
        return Cast(source, typeof(T)) as T;
    }

    public static Delegate Cast(Delegate source, Type type)
    {
        if (source == null)
            return null;

        Delegate[] delegates = source.GetInvocationList();

        if (delegates.Length == 1)
            return Delegate.CreateDelegate(type, delegates[0].Target, delegates[0].Method);

        Delegate[] delegatesDest = new Delegate[delegates.Length];
        for (int nDelegate = 0; nDelegate < delegates.Length; nDelegate++)
            delegatesDest[nDelegate] = Delegate.CreateDelegate(type,
                delegates[nDelegate].Target, delegates[nDelegate].Method);
        return Delegate.Combine(delegatesDest);
    }
}


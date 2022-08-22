using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrieAnimator))]
public class TrieInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TrieAnimator ta = (TrieAnimator)target;

        if (GUILayout.Button("Setup Trie"))
        {
            ta.Setup();
        }
    }
}

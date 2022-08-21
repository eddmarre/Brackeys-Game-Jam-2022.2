
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(Sound))]
public class SoundDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 100f;
    }
    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        Rect GetNewRect(float mult)
        {
            return new Rect(rect.x, rect.y, rect.width * mult, rect.height);
        }

        var clipProperty = property.FindPropertyRelative("clip");
        var targetProperty = property.FindPropertyRelative("target");
        var nameProperty = property.FindPropertyRelative("name");
        var volumeProperty = property.FindPropertyRelative("volume");
        var pitchProperty = property.FindPropertyRelative("pitch");
        var typeProperty = property.FindPropertyRelative("soundType");
        var loopProperty = property.FindPropertyRelative("loop");
        var loopTimeProperty = property.FindPropertyRelative("customLoopTime");

        EditorGUIUtility.wideMode = true;
        EditorGUIUtility.labelWidth = 30;
        EditorGUI.indentLevel = 0;

        rect.height /= 5f;
        rect.width /= 4f;

        rect.y += 4f;

        nameProperty.stringValue = EditorGUI.TextField(GetNewRect(2.5f), nameProperty.stringValue);
        rect.x += rect.width * 2.7f;

        EditorGUI.LabelField(GetNewRect(0.5f), "Loop: ");
        rect.x += rect.width * 0.5f;

        loopProperty.boolValue = EditorGUI.Toggle(rect, loopProperty.boolValue);

        rect.x = 16;
        rect.y += 22f;
        rect.width *= 4f;
        rect.width /= 6f;

        EditorGUI.LabelField(rect, "Target: ");
        rect.x += rect.width;

        targetProperty.objectReferenceValue = (GameObject)EditorGUI.ObjectField(GetNewRect(1.2f), targetProperty.objectReferenceValue, typeof(GameObject));
        rect.x += rect.width * 1.5f;

        clipProperty.objectReferenceValue = (AudioClip)EditorGUI.ObjectField(GetNewRect(2f), clipProperty.objectReferenceValue, typeof(AudioClip));
        rect.x += rect.width * 2.1f;

        typeProperty.enumValueIndex = (int)(SoundType)EditorGUI.EnumPopup(rect, (SoundType)Enum.GetValues(typeof(SoundType)).GetValue(typeProperty.enumValueIndex));
        rect.x += rect.width;

        rect.x = 16f;
        rect.y += 20f;
        rect.width *= 6f;
        rect.width /= 4f;

        EditorGUI.LabelField(rect, "Volume: ");
        rect.x += rect.width;

        volumeProperty.floatValue = EditorGUI.Slider(GetNewRect(3f), volumeProperty.floatValue, 0f, 1f);

        rect.x = 16f;
        rect.y += 18f;

        EditorGUI.LabelField(rect, "Pitch: ");
        rect.x += rect.width;

        pitchProperty.floatValue = EditorGUI.Slider(GetNewRect(3f), pitchProperty.floatValue, 0.1f, 4f);
        //rect.x += rect.width * 3f;

        

		if (loopProperty.boolValue)
		{
            rect.x = 16f;
            rect.y += 18f;

            EditorGUI.LabelField(rect, "Loop Time: ");
            rect.x += rect.width;
            
            float clipTime = ((AudioClip)clipProperty.objectReferenceValue).length;
            loopTimeProperty.floatValue = EditorGUI.Slider(GetNewRect(3f), loopTimeProperty.floatValue, clipTime, clipTime * 10f);
		}
    }
}


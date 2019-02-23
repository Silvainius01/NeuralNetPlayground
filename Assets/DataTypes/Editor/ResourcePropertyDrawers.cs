using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ResourceValue))]
public class ResourceValuePD : PropertyDrawer
{
	int lastIndex = 0;
	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);

		var valueProp = property.FindPropertyRelative("value");
		var nameProp = property.FindPropertyRelative("resource").FindPropertyRelative("m_name");

		if (!Mathc.ArrayContains(ref GameManagerEditor.availableResources, nameProp.stringValue, out lastIndex)) 
			lastIndex = 0;

		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var nameRect = new Rect(position.x+2, position.y, 135, position.height);	// min = x+2		|| max = x+137
		var amount = new Rect(nameRect.xMax + 7, position.y, 135, position.height); // min = prevMax+7	|| max = x+279

		lastIndex = EditorGUI.Popup(nameRect, lastIndex, GameManagerEditor.availableResources);
		nameProp.stringValue = GameManagerEditor.availableResources[lastIndex];
		valueProp.floatValue = EditorGUI.FloatField(amount, valueProp.floatValue);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}
}

[CustomPropertyDrawer(typeof(t_BuildingTypeLimit))]
public class BuildingTypeLimitPD : PropertyDrawer
{
	int lastIndex = 0;
	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);

		var valueProp = property.FindPropertyRelative("max");
		var nameProp = property.FindPropertyRelative("type");

		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var nameRect = new Rect(position.x + 2, position.y, 135, position.height);
		var amount = new Rect(position.x + nameRect.width + 7, position.y, 135, position.height);

		valueProp.intValue = EditorGUI.IntField(amount, valueProp.intValue);
		EditorGUI.Popup(nameRect, nameProp.enumValueIndex, nameProp.enumNames);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}
}

[CustomPropertyDrawer(typeof(BuildingValueInt))]
public class BuildingValueIntPD : PropertyDrawer
{
	int lastIndex = 0;
	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);

		var valueProp = property.FindPropertyRelative("value");
		var typeProp = property.FindPropertyRelative("building");//.FindPropertyRelative("m_buildingType");
		var nameProp = typeProp.FindPropertyRelative("m_name");
		typeProp = typeProp.FindPropertyRelative("m_buildingType");

		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var nameRect = new Rect(position.x, position.y, 93, position.height);			// min = x						|| max = min+93 = x+93
		var valueRect = new Rect(nameRect.xMax + 7, position.y, 93, position.height);	// min = prevMax+7 = x+100		|| max = min+93 = x+193
		var typeRect = new Rect(valueRect.xMax + 7, position.y, 79, position.height);	// min = prevMax+7 = x+200		|| max = min+79 = x+279
		// final 

		EditorGUI.TextField(nameRect, nameProp.stringValue);
		EditorGUI.IntField(typeRect, valueProp.intValue);
		EditorGUI.Popup(valueRect, typeProp.enumValueIndex, typeProp.enumNames);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using static NeuralNetwork.Editor;

public class EditorPrefsUtil
{
	public static Color GetColor(string name, Color defaultValue)
	{
		if (EditorPrefs.HasKey(name))
		{
			defaultValue.r = EditorPrefs.GetFloat(name + "_r");
			defaultValue.g = EditorPrefs.GetFloat(name + "_g");
			defaultValue.b = EditorPrefs.GetFloat(name + "_b");
			defaultValue.a = EditorPrefs.GetFloat(name + "_a");
		}
		return defaultValue;
	}
	public static void SetColor(string name, Color value)
	{
		EditorPrefs.SetBool(name, true); // This tells GetColor that this color exists.
		EditorPrefs.SetFloat(name + "_r", value.r);
		EditorPrefs.SetFloat(name + "_g", value.g);
		EditorPrefs.SetFloat(name + "_b", value.b);
		EditorPrefs.SetFloat(name + "_a", value.a);
	}

	public static Gradient GetGradient(string name, Gradient defaultValue)
	{
		if (EditorPrefs.HasKey(name))
		{
			int numColorKeys = EditorPrefs.GetInt(name + "_colorKeys");
			int numAlphaKeys = EditorPrefs.GetInt(name + "_alphaKeys");
			var colorKeys = new GradientColorKey[numColorKeys];
			var alphaKeys = new GradientAlphaKey[numAlphaKeys];

			for (int i = 0; i < numColorKeys; ++i)
			{
				colorKeys[i].color = GetColor(name + string.Format("_c{0}", i), Color.white);
				colorKeys[i].time = EditorPrefs.GetFloat(name + string.Format("_ct{0}", i), 0.0f);
			}
			for (int i = 0; i < numAlphaKeys; ++i)
			{
				alphaKeys[i].alpha = EditorPrefs.GetFloat(name + string.Format("_a{0}", i), 0.0f);
				alphaKeys[i].time = EditorPrefs.GetFloat(name + string.Format("_at{0}", i), 0.0f);
			}
			defaultValue.SetKeys(colorKeys, alphaKeys);
		}
		return defaultValue;
	}
	public static void SetGradient(string name, Gradient value)
	{
		int numColorKeys = value.colorKeys.Length;
		int numAlphaKeys = value.alphaKeys.Length;

		EditorPrefs.SetBool(name, true);
		EditorPrefs.SetInt(name + "_colorKeys", numColorKeys);
		EditorPrefs.SetInt(name + "_alphaKeys", numAlphaKeys);
		for (int i = 0; i < numColorKeys; ++i)
		{
			SetColor(name + string.Format("_c{0}", i), value.colorKeys[i].color);
			EditorPrefs.SetFloat(name + string.Format("_ct{0}", i), value.colorKeys[i].time);
		}
		for (int i = 0; i < numAlphaKeys; ++i)
		{
			EditorPrefs.SetFloat(name + string.Format("_a{0}", i), value.alphaKeys[i].alpha);
			EditorPrefs.SetFloat(name + string.Format("_at{0}", i), value.alphaKeys[i].time);
		}
	}
}

[CustomEditor(typeof(NeuralNetwork))]
public class NeuralNetworkEditor : Editor
{
	NeuralNetwork nn;
	bool showingGizmoOptions = false;

	float nodeSize;
	float nSpace;
	float lSpace;
	Color color;
	Gradient gradient;
	SerializedProperty editorProp;
	static Dictionary<int, Vector3> lastPositions = new Dictionary<int, Vector3>();

	public void OnEnable()
	{
		nn = (NeuralNetwork)target;
		CleanEditorUtil(nn);

		NeuralNetwork.Editor.nodeSize = EditorPrefs.GetFloat("nn_NodeSize", NeuralNetwork.Editor.nodeSize);
		NeuralNetwork.Editor.nodeSpacing = EditorPrefs.GetFloat("nn_NodeSpace", NeuralNetwork.Editor.nodeSpacing);
		NeuralNetwork.Editor.layerSpacing = EditorPrefs.GetFloat("nn_LayerSpace", NeuralNetwork.Editor.layerSpacing);
		NeuralNetwork.Editor.nodeColor = EditorPrefsUtil.GetColor("nn_NodeColor", NeuralNetwork.Editor.nodeColor);
		NeuralNetwork.Editor.axonGradient = EditorPrefsUtil.GetGradient("nn_NodeSize", NeuralNetwork.Editor.axonGradient);

		UpdateGizmoDataAndPosition();
	}

	public void OnDisable()
	{
		EditorPrefs.SetFloat("nn_NodeSize", NeuralNetwork.Editor.nodeSize);
		EditorPrefs.SetFloat("nn_NodeSpace", NeuralNetwork.Editor.nodeSpacing);
		EditorPrefs.SetFloat("nn_LayerSpace", NeuralNetwork.Editor.layerSpacing);
		EditorPrefsUtil.SetColor("nn_NodeColor", NeuralNetwork.Editor.nodeColor);
		EditorPrefsUtil.SetGradient("nn_NodeSize", NeuralNetwork.Editor.axonGradient); 
	}

	public override void OnInspectorGUI()
	{
		#region Gizmo Options
		if (GUILayout.Button(showingGizmoOptions ? "Hide Gizmo Options" : "Show Gizmo Options"))
		{
			showingGizmoOptions = !showingGizmoOptions;
			if(showingGizmoOptions)
			{
				nodeSize = NeuralNetwork.Editor.nodeSize;
				nSpace = nodeSpacing;
				lSpace = layerSpacing;
				color  = nodeColor;
				gradient  = axonGradient;
			}
		}
		if (showingGizmoOptions)
		{
			EditorGUI.indentLevel = 1;
			color = EditorGUILayout.ColorField("Node Color", color);
			gradient = EditorGUILayout.GradientField("Relation Gradient", gradient);
			nodeSize = EditorGUILayout.FloatField("Node Size", nodeSize);
			nSpace = EditorGUILayout.FloatField("Node Spacing", nSpace);
			lSpace = EditorGUILayout.FloatField("Layer Spacing", lSpace);
			if(GUILayout.Button("Reset"))
				ApplyGizmoSettings(Color.cyan, GetDefaultGradient(), 1.0f, 5.0f, 5.0f);
			if (GUILayout.Button("Apply"))
				ApplyGizmoSettings(color, gradient, nodeSize, nSpace, lSpace);

			EditorGUI.indentLevel = 0;
		}
		#endregion

		GUILayout.Space(15);

		#region I/O Nodes
		int prevVal = 0;
		bool layersChanged = false;


		var editorProp = serializedObject.FindProperty("editorUtil");
		var layersProp = editorProp.FindPropertyRelative("nodeHeights");
		
		layersChanged |= NodeLayerMod(layersProp.GetArrayElementAtIndex(0), "Num Input Nodes");
		layersChanged |= NodeLayerMod(layersProp.GetArrayElementAtIndex(layersProp.arraySize-1), "Num Output Nodes");
		#endregion

		GUILayout.Space(5);

		#region Hidden Layers
		EditorGUI.indentLevel = 1;
		if(GUILayout.Button("Add Hidden Layer"))
		{
			layersChanged = true;
			layersProp.InsertArrayElementAtIndex(layersProp.arraySize-1);
			var p = layersProp.GetArrayElementAtIndex(layersProp.arraySize-1);
			if (p.intValue < 1)
				p.intValue = 1;
		}

		for(int i = 1; i < layersProp.arraySize-1; ++i)
		{
			var indexProp = layersProp.GetArrayElementAtIndex(i);
			layersChanged |= NodeLayerMod(indexProp, $"Hidden Layer {i.ToString()} Nodes", out bool remove);
			if (remove)
			{
				layersChanged = true;
				layersProp.DeleteArrayElementAtIndex(i--);
			}
		}
		EditorGUI.indentLevel = 0;

		#endregion

		GUILayout.Space(15);

		serializedObject.ApplyModifiedProperties();
		if(GUILayout.Button("Rebuild Network") || layersChanged)
		{
			ApplyNetworkChanges();
		}
		if(GUILayout.Button("Reroll Connections"))
		{
			nn.ConnectNetworkFresh();
			EditorUtility.SetDirty(nn);
		}
		GUILayout.Space(5);
		if(GUILayout.Button("Refresh Connections"))
		{
			nn.RefreshConnections();
			EditorUtility.SetDirty(nn);
		}

	}

	void UpdateGizmoDataAndPosition()
	{
		int id = nn.GetInstanceID();
		CacheNodePositionChange(nn, lastPositions.ContainsKey(id) ? lastPositions[id] : nn.transform.position);
		lastPositions[nn.GetInstanceID()] = nn.transform.position;
	}
	void UpdateGizmoData()
	{
		CacheGizmoDrawData(nn);
	}
	void ApplyGizmoSettings(Color color, Gradient gradient, float nodeSize, float nodeSpace, float layerSpace)
	{
		nodeColor = this.color = color;
		axonGradient = this.gradient = gradient;
		nodeSpacing = nSpace = nodeSpace;
		layerSpacing = lSpace = layerSpace;
		NeuralNetwork.Editor.nodeSize = this.nodeSize = nodeSize;
		UpdateGizmoData();
		EditorUtility.SetDirty(nn);
	}
	void ApplyNetworkChanges()
	{
		RefreshNetwork(nn);
		UpdateGizmoData();
		EditorUtility.SetDirty(nn);
	}

	bool NodeLayerMod(SerializedProperty nodeProp, string name)
	{
		int prevVal = nodeProp.intValue;
		EditorGUILayout.BeginHorizontal();
		nodeProp.intValue = Mathf.Max(EditorGUILayout.IntField(name, nodeProp.intValue), 1);

		if (GUILayout.Button(" + "))
			++nodeProp.intValue;

		EditorGUI.BeginDisabledGroup(prevVal <= 1);
		if (GUILayout.Button(" - "))
			--nodeProp.intValue;
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.EndHorizontal();
		return (prevVal != nodeProp.intValue);
	}
	bool NodeLayerMod(SerializedProperty nodeProp, string name, out bool remove)
	{
		int prevVal = nodeProp.intValue;
		EditorGUILayout.BeginHorizontal();
		nodeProp.intValue = Mathf.Max(EditorGUILayout.IntField(name, nodeProp.intValue), 1);

		if (GUILayout.Button(" + "))
			++nodeProp.intValue;

		EditorGUI.BeginDisabledGroup(prevVal <= 1);
		if (GUILayout.Button(" - "))
			--nodeProp.intValue;
		EditorGUI.EndDisabledGroup();

		remove = GUILayout.Button("Remove");
		EditorGUILayout.EndHorizontal();

		return (prevVal != nodeProp.intValue);
	}

	void OnSceneGUI()
	{
		if(nn != null && lastPositions[nn.GetInstanceID()] != nn.transform.position)
		{
			UpdateGizmoDataAndPosition();
		}
	}
}

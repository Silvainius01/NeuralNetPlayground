using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
	public static GameManager gameManager;
	public static string[] availableResources;

	public void OnEnable()
	{
	}

	public override void OnInspectorGUI()
	{
		gameManager = (GameManager)target;
		availableResources = gameManager.GetResourceNames().ToArray();
		base.OnInspectorGUI();
	}
}

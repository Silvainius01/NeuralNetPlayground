using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[System.Serializable]
public struct t_BuildingTypeLimit
{
	public int max;
	public BUILDING_TYPE type;
}

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	[Header("Gameboard")]
	public GraphMaker graphMaker;
	public GameObject nodePrefab;

	[Header("Resource Settings")]
	[SerializeField] List<ResourceData> m_resourceList;

	public ReadOnlyCollection<ResourceData> resourceList;
	public ReadOnlyDictionary<int, ResourceData> resourceDict;
	Dictionary<int, ResourceData> m_resourceDict = new Dictionary<int, ResourceData>();

	[Header("Building Settings")]
	[SerializeField] List<BuildingData> m_buildingList;
	[SerializeField] List<t_BuildingTypeLimit> m_maxBuildingTypesList;
	public ReadOnlyCollection<BuildingData> buildingList;
	public ReadOnlyDictionary<int, BuildingData> buildingDict;
	public ReadOnlyDictionary<BUILDING_TYPE, int> maxBuildingTypesDict;
	Dictionary<int, BuildingData> m_buildingDict = new Dictionary<int, BuildingData>();
	Dictionary<BUILDING_TYPE, int> m_maxBuildingTypesDict = new Dictionary<BUILDING_TYPE, int>(new BuildingTypeComparer());
	Dictionary<BUILDING_TYPE, List<BuildingData>> m_BuildingsByType = new Dictionary<BUILDING_TYPE, List<BuildingData>>(new BuildingTypeComparer());

	[Header("Node Settings")]
	[SerializeField] List<ResourceValue> expansionCost;

	public int numNodes { get { return nodes.Count; } }

	List<Player> players;
	List<NodeController> nodes;

	public void Awake()
	{
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		}
		instance = this;

		// Init graph
		if (graphMaker == null)
			graphMaker = new GameObject("Graph Maker").AddComponent<GraphMaker>();
		graphMaker.GenerateBoard(10, 10, 5.0f);

		InitializeResources();
		InitializeBuildings();

		// Create nodes
		nodes = new List<NodeController>(graphMaker.numPoints);
		for(int i = 0; i < graphMaker.graphPoints.Count; ++i)
		{
			nodes.Add(Instantiate(nodePrefab).GetComponent<NodeController>());
			var point = graphMaker.graphPoints[i];
			var node = nodes[nodes.Count - 1];
			node.transform.position = point.position;
			node.transform.SetParent(transform);
			node.Init(i, point);
		}

		// Init players
		players = new List<Player>(2);

		players.Add(new GameObject("Player 0").AddComponent<Player>());
		players[0].Init(0, Color.blue);
		nodes[0].SetOwner(players[0]);
		nodes[0].resourceCont.SetAllResourceRates(0.0f);
		nodes[0].buildingCont.AddBuilding("Farm", 1);
		nodes[0].buildingCont.AddBuilding("Lumberyard", 1);
		nodes[0].buildingCont.AddBuilding("Market", 1);

		players.Add(new GameObject("Player 1").AddComponent<Player>());
		players[1].Init(1, Color.red);
		nodes[nodes.Count - 1].SetOwner(players[1]);
		nodes[nodes.Count - 1].resourceCont.SetAllResourceRates(0.0f);
		nodes[nodes.Count - 1].buildingCont.AddBuilding("Farm", 1);
		nodes[nodes.Count - 1].buildingCont.AddBuilding("Lumberyard", 1);
		nodes[nodes.Count - 1].buildingCont.AddBuilding("Market", 1);
	}
	public void Update()
	{
		foreach(var p in players)
		{
			p.UpdatePlayer();
		}
	}

	void InitializeResources()
	{
		resourceList = new ReadOnlyCollection<ResourceData>(m_resourceList);
		resourceDict = new ReadOnlyDictionary<int, ResourceData>(m_resourceDict);

		foreach (var rData in m_resourceList)
		{
			rData.Init();
			m_resourceDict.Add(rData.key, rData);
		}
		foreach (var rv in expansionCost)
			rv.Init();
	}
	void InitializeBuildings()
	{
		buildingList = new ReadOnlyCollection<BuildingData>(m_buildingList);
		buildingDict = new ReadOnlyDictionary<int, BuildingData>(m_buildingDict);
		maxBuildingTypesDict = new ReadOnlyDictionary<BUILDING_TYPE, int>(m_maxBuildingTypesDict);

		foreach(var t in Mathc.GetEnumValues<BUILDING_TYPE>())
			m_BuildingsByType.Add(t, new List<BuildingData>());

		foreach (var bData in m_buildingList)
		{
			bData.Init();
			m_buildingDict.Add(bData.key, bData);
			m_BuildingsByType[bData.buildingType].Add(bData);
		}
		foreach (var t2 in m_maxBuildingTypesList)
			m_maxBuildingTypesDict.Add(t2.type, t2.max);

	}

	#region Node Functions

	public NodeController GetNodeFromIndex(int index)
	{
		if (Mathc.ValueIsBetween(index, -1, nodes.Count, true))
			return nodes[index];
		return null;
	}
	
	public bool CanPlayerClaimEmptyNode(Player player)
	{
		foreach (var rv in expansionCost)
			if (player.GetCurrentResourcePool(rv.resource) < rv.value)
				return false;
		return true;
	}
	public bool CanPlayerClaimNode(NodeController node, Player player)
	{
		if (node == null || node.IsOwned())
			return false;
		return CanPlayerClaimEmptyNode(player);
	}
	public bool TransferNodeToPlayer(NodeController node, Player player)
	{
		node.SetOwner(player);
		foreach (var rv in expansionCost)
			player.AddResourcesToPool(rv.resource, -rv.value);
		return true;
	}

	public bool AttemptToBuyBuilding(Player player, NodeController node, BuildingData building)
	{
		if (node.buildingCont.CanSupportBuilding(building) && player.CanAffordBuilding(building))
		{
			foreach (var rv in building.buildingCostROC)
				player.AddResourcesToPool(rv.resource, -rv.value);
			return true;
		}
		return false;
	}
	public BuildingData GetRandomBuildingOfType(BUILDING_TYPE type)
	{
		int max = m_BuildingsByType[type].Count;
		if (max > 0)
			return m_BuildingsByType[type][UnityEngine.Random.Range(0, max)];
		return null;
	}
	#endregion

	#region Resource Functions
	public float GetBaseExpansionRate(ResourceData r)
	{
		return 100.0f;
	}
	public float GetBaseNodeIncomeRate(ResourceData r)
	{
		return 0.0f;
	}

	public float GetTradeAmount(ResourceData tradedResource, ResourceData desiredResource, float numTR)
	{
		//float rate = baseResourceData[tradedResource].baseBankValue / baseResourceData[desiredResource].baseExchangeRate;
		return numTR * 0.5f;
	}

	public ResourceData GetResourceFromName(string name)
	{
		int k = name.GetHashCode();
		return resourceDict.ContainsKey(k) ? resourceDict[k] : null;
	}

	#endregion

	public BuildingData GetBuildingFromName(string name)
	{
		int k = name.GetHashCode();
		return buildingDict.ContainsKey(k) ? buildingDict[k] : null;
	}

#if UNITY_EDITOR
	#region Editor Utility
	List<string> resourceNames = new List<string>(3);
	public List<string> GetResourceNames()
	{
		resourceNames.Clear();
		resourceNames.Capacity = m_resourceList.Count;
		foreach (var r in m_resourceList)
			resourceNames.Add(r.name);
		return resourceNames;
	}
	#endregion
#endif
}

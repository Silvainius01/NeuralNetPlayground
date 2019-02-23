//using System;
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

	[Header("Player Settings")]
	public int gameSpeed = 1;
	public int numPlayersToCreate = 2;
	public Vector2Int boardDimensions;
	[SerializeField] PlayerNetworkManager bestNetwork;
	[SerializeField] List<Color> playerColors = new List<Color>();
	public int maxActionsPerTurn = 6;
	public bool showActionsTaken = false;
	public int numNodes { get { return nodes.Count; } }

	int winningScore = 100;
	uint currentTurn = 0;
	public List<Player> players;
	List<NodeController> nodes;
	List<PlayerNetworkManager> playerNetworks = new List<PlayerNetworkManager>();

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
		graphMaker.GenerateBoard(boardDimensions.x, boardDimensions.y, 2.5f);
		winningScore = graphMaker.graphPoints.Count;

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
		players = new List<Player>(playerNetworks.Count);

		bestNetwork.Init(null);
		for(int i = 0; i < numPlayersToCreate; ++i)
		{
			players.Add(new GameObject($"Player {i.ToString()}").AddComponent<Player>());
			playerNetworks.Add(PlayerNetworkManager.Copy($"Player {i.ToString()} Networks", bestNetwork));
			players[i].Init(i, playerColors[i], playerNetworks[i]);
			if (i > 0) playerNetworks[i].MutateNetworksAsexual(0.15f);
		}

		StartGame();
	}
	public void Update()
	{
		for (int i = 0; i < gameSpeed; ++i)
		{
			int playerWon = -1;
			int playersAlive = 0;

			currentTurn++;
			foreach (var p in players)
			{
				p.UpdatePlayer();
				if (p.numOwnedNodes > 0)
				{
					playersAlive++;
					playerWon = p.id;
				}
			}

			if (playersAlive == 1 && playerWon >= 0)
			{
				string msg = $"Player {playerWon} won after {currentTurn} turns\nPlayers Mutated: ";
				var winner = playerNetworks[playerWon];
				foreach (var p in players)
					if (p.id != playerWon)
					{
						playerNetworks[p.id].CopyNetworkConnectionsFrom(winner);
						playerNetworks[p.id].MutateNetworksAsexual(0.15f);
						msg += $"{p.id} ";
					}
				Debug.Log(msg);
				SaveCurrentBest(winner);
				StartGame();
			}
		}
	}

	void StartGame()
	{
		foreach (var node in nodes)
		{
			node.Reset();
			node.resourceCont.SetResourceRate(GetRandomResource(), UnityEngine.Random.Range(1.0f, 10.0f));
		}

		int index = 0;
		HashSet<int> illegalIndexs = new HashSet<int>();

		currentTurn = 0;
		foreach (var p in players)
		{
			index = Random.Range(0, nodes.Count);
			while (illegalIndexs.Contains(index))
				index = Random.Range(0, nodes.Count);

			p.Reset();
			nodes[index].SetOwner(p);
			nodes[index].buildingCont.SetBuildingCount("Farm", 1);
			nodes[index].buildingCont.SetBuildingCount("Lumberyard", 1);
			nodes[index].buildingCont.SetBuildingCount("Market", 1);

			illegalIndexs.Add(index);
			foreach (var c in nodes[index].connections)
				illegalIndexs.Add(c.Value.index);
		}
	}

	void BreedNetworks(PlayerNetworkManager winner, PlayerNetworkManager loser)
	{
			loser.CopyNetworkConnectionsFrom(winner);
			loser.MutateNetworksAsexual(0.15f);
			SaveCurrentBest(winner);
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
			if (player.GetResourcePool(rv.resource) < rv.value)
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
		if (node == null)
			return false;

		//Debug.Log($"Player {player.id} expanded. Previous owner: {node.owner?.id}");

		node.owner?.OnNodeStolen(player);

		node.SetOwner(player);
		foreach (var rv in expansionCost)
			player.AddResourcesToPool(rv.resource, -rv.value);

		// Notify players that someone expanded.

		foreach (var p in players)
			p.OnPlayerExpanded(player, node);

		node.playerDistanceDict[player.id] = new System.Tuple<int, uint>(0, player.lastExpansion);
		return true;
	}

	public bool AttemptToBuyBuilding(Player player, NodeController node, BuildingData building)
	{
		if (player.CanAffordBuilding(building) && node.buildingCont.AddBuilding(building,1))
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

	public static int TriangleNumber(int num)
	{
		return (num * (num + 1)) / 2;
	}

	public int DistanceToNearestOwnedNode(NodeController start, Player askingPlayer)
	{
		if (!start.IsOwnedBy(askingPlayer))
			return 0;

		int currDist = 0;
		HashSet<int> evaldNodes = new HashSet<int>();
		Queue<NodeController> q = new Queue<NodeController>(4);

		q.Enqueue(start);
		foreach (var c in start.connections)
			q.Enqueue(c.Value);

		while(q.Count > 0)
		{
			int endIndex = q.Count;

			for(int i = 0; i < endIndex; ++i)
			{
				var current = q.Dequeue();
				if (!evaldNodes.Contains(current.index))
				{
					if (!current.IsOwnedBy(askingPlayer))
						return currDist;
					evaldNodes.Add(current.index);
					foreach (var c in current.connections)
						q.Enqueue(c.Value);
				}
			}
			currDist++;
		}

		return currDist;
	}
	public int DistanceToNearestPlayerNode(NodeController start, Player targetPlayer)
	{
		// return cached value if this player has not expanded since we last checked
		if (start.playerDistanceDict.ContainsKey(targetPlayer.id))
		{
			if (start.playerDistanceDict[targetPlayer.id].Item2 >= targetPlayer.lastExpansion)
				return start.playerDistanceDict[targetPlayer.id].Item1;
		}


		int rv = 0;
		if (start.IsOwnedBy(targetPlayer))
			rv = 0;
		else if (targetPlayer.borderNodes.ContainsKey(start.index))
			rv = 1;
		else
		{
			int smallestDist = int.MaxValue;
			foreach (var kvp in targetPlayer.borderNodes)
			{
				var pNode = kvp.Value;
				int dist = DistanceToNode(start, pNode);
				if (dist < smallestDist)
				{
					smallestDist = dist;
					if (dist == 1)
						break;
				}
			}
			// return +1 because we are doing distance to border nodes,
			// which means closest owned node must be one more jump.
			rv = smallestDist + 1;
		}
		start.playerDistanceDict[targetPlayer.id] = new System.Tuple<int, uint>(rv, currentTurn);
		return rv;
	}
	public int DistanceToNode(NodeController start, NodeController end)
	{
		var diff = start.boardPosition - end.boardPosition;
		return Mathf.Abs(diff.x) + Mathf.Abs(diff.y); // num jumps to node
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

	public ResourceData GetRandomResource()
	{
		return resourceList[UnityEngine.Random.Range(0, resourceList.Count)];
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

	public void SaveCurrentBest(PlayerNetworkManager winner)
	{
		bestNetwork.CopyNetworkConnectionsFrom(winner);
		bestNetwork.completionTime = winner.completionTime;
	}
	#endregion
#endif
}

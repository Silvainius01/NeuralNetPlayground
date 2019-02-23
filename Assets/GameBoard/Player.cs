using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	public Color color;
	public int id { get; private set; }
	public int numOwnedNodes { get { return territoryStats.x; } }
	public int numBorderNodes { get { return territoryStats.y; } }
	public uint expansionRate { get; private set; }
	public uint lastExpansion { get; private set; }

	[SerializeField] Vector2Int territoryStats = new Vector2Int();
	Dictionary<int, NodeController> ownedNodes;
	Dictionary<int, NodeController> m_borderNodes;

	public ReadOnlyDictionary<int, NodeController> borderNodes;

	[SerializeField] List<ResourceValue> m_resourcePoolsList;
	Dictionary<int, ResourceValue> m_resourcePoolsDict;
	public ReadOnlyCollection<ResourceValue> resourcePoolsList;
	public ReadOnlyDictionary<int, ResourceValue> resourcePoolsDict;

	[SerializeField] List<ResourceValue> m_resourceRatesList;
	Dictionary<int, ResourceValue> m_resourceRatesDict;
	public ReadOnlyCollection<ResourceValue> resourceRatesList;
	public ReadOnlyDictionary<int, ResourceValue> resourceRatesDict;

	Queue<int> undevelopedNodes = new Queue<int>();

	PlayerNetworkManager nnManager;


	public void Init(int playerID, Color playerColor, PlayerNetworkManager nnManager)
	{
		id = playerID;
		color = playerColor;
		ownedNodes = new Dictionary<int, NodeController>(GameManager.instance.numNodes);

		m_borderNodes = new Dictionary<int, NodeController>(GameManager.instance.numNodes);
		borderNodes = new ReadOnlyDictionary<int, NodeController>(m_borderNodes);

		// Init resource pools
		m_resourcePoolsList = new List<ResourceValue>(GameManager.instance.resourceList.Count);
		m_resourcePoolsDict = new Dictionary<int, ResourceValue>(GameManager.instance.resourceList.Count);
		resourcePoolsList = new ReadOnlyCollection<ResourceValue>(m_resourcePoolsList);
		resourcePoolsDict = new ReadOnlyDictionary<int, ResourceValue>(m_resourcePoolsDict);

		m_resourceRatesList = new List<ResourceValue>(GameManager.instance.resourceList.Count);
		m_resourceRatesDict = new Dictionary<int, ResourceValue>(GameManager.instance.resourceList.Count);
		resourceRatesList = new ReadOnlyCollection<ResourceValue>(m_resourceRatesList);
		resourceRatesDict = new ReadOnlyDictionary<int, ResourceValue>(m_resourceRatesDict);

		this.nnManager = nnManager;
		this.nnManager.Init(this);

		Reset();
	}
	public void Reset()
	{
		nnManager.Reset();
		undevelopedNodes.Clear();
		SetAllResourceRates(0.0f);
		SetAllResourcePools(0.0f);
	}

		List<float> desires = new List<float>(4);
	public void UpdatePlayer()
	{
		// Update resource pools
		//foreach (var kvp in ownedNodes)
		foreach (var rType in GameManager.instance.resourceList)
			AddResourcesToPool(rType, GetResourceRate(rType) * Time.deltaTime);

		nnManager.EvaluateThreatLevel();

		int actions = 0;
		for (; actions < GameManager.instance.maxActionsPerTurn; ++actions)
		{
			NodeController expansionNode = null;
			NodeController developNode = null;
			desires.Clear();

			while (undevelopedNodes.Count > 0 && !ownedNodes.ContainsKey(undevelopedNodes.Peek()))
				undevelopedNodes.Dequeue();
			if (undevelopedNodes.Count > 0)
			{
				developNode = ownedNodes[undevelopedNodes.Peek()];
				foreach (var f in nnManager.EvaluateDevelopment(developNode))
					desires.Add(f);
			}
			else desires.AddRange(new float[3] { -1, -1, -1 });

			if (GameManager.instance.CanPlayerClaimEmptyNode(this))
				desires.Add(nnManager.EvaluateExpansion(out expansionNode));
			else desires.Add(0.0f);

			int actionDesire = -1;
			float highest = 0.0f;
			for(int i = 0; i < desires.Count; ++i)
			{
				if (desires[i] > highest)
				{
					actionDesire = i;
					highest = desires[i];
				}
			}

			switch(actionDesire)
			{
				case -1:
					break;
				case 3:
					GameManager.instance.TransferNodeToPlayer(expansionNode, this);
					break;
				default:
					var building = GameManager.instance.buildingList[actionDesire];
					GameManager.instance.AttemptToBuyBuilding(this, developNode, building);
					if (!developNode.buildingCont.CanSupportBuildingType(BUILDING_TYPE.ECONOMIC))
						undevelopedNodes.Dequeue();
					break;
			}
			
			// Do nothing if no actions can take place.
			if (actionDesire == -1)
				break;
		}

		if (GameManager.instance.showActionsTaken)
			Debug.Log("Player" + id + " took " + actions + " actions");

		nnManager.completionTime++;
		territoryStats.x = ownedNodes.Count;
		territoryStats.y = m_borderNodes.Count;
	}

	#region Node Functions
	public void AddNode(NodeController node)
	{
		if (ownedNodes.ContainsKey(node.index))
			return;

		ownedNodes[node.index] = node;

		if (node.buildingCont.CanSupportBuildingType(BUILDING_TYPE.ECONOMIC))
			undevelopedNodes.Enqueue(node.index);

		// Find and add any new border nodes
		foreach (var n in node.connections.Values)
			if (!n.IsOwnedBy(this))
			{
				m_borderNodes[n.index] = n;
				n.SetNearestOwned(GameManager.instance.DistanceToNearestOwnedNode(n, this));
			}

		// Remove this node from border nodes
		if (m_borderNodes.ContainsKey(node.index))
			m_borderNodes.Remove(node.index);
	}
	public void RemoveNode(NodeController node)
	{
		if (!ownedNodes.ContainsKey(node.index))
			return;
		
		ownedNodes.Remove(node.index);

		// Remove any unconnected border nodes
		foreach (var n in node.connections.Values)
			if (!IsNodeConnected(n) && m_borderNodes.ContainsKey(n.index))
				m_borderNodes.Remove(n.index);

		// If this node is still connected to our territory, add it to borders.
		if (IsNodeConnected(node))
		{
			m_borderNodes[node.index] = node;
			node.SetNearestOwned(GameManager.instance.DistanceToNearestOwnedNode(node, this));
		}
	}

	public NodeController GetRandomOwnedNode()
	{
		if (ownedNodes.Count < 1)
			return null;
		return Mathc.GetRandomValueFromDict(ref ownedNodes);
	}
	public NodeController GetRandomBorderNode()
	{
		if (m_borderNodes.Count < 1)
			return null;
		return Mathc.GetRandomValueFromDict(ref m_borderNodes);
	}
	#endregion

	public float GetResourcePool(ResourceData resource)
	{
		if (m_resourcePoolsDict.ContainsKey(resource.key))
			return m_resourcePoolsDict[resource.key].value;
		return 0.0f;
	}
	public void AddResourcesToPool(ResourceData type, float amt)
	{
		if (m_resourcePoolsDict.ContainsKey(type.key))
			m_resourcePoolsDict[type.key].SetValue(m_resourcePoolsDict[type.key].value + amt);
		else AddResourcePool(type, amt);
	}
	public void SetResourcePool(ResourceData type, float amt)
	{
		if (m_resourcePoolsDict.ContainsKey(type.key))
			m_resourcePoolsDict[type.key].SetValue(amt);
		else AddResourcePool(type, amt);
	}
	public void SetAllResourcePools(float amt)
	{
		foreach (var rData in GameManager.instance.resourceList)
			SetResourcePool(rData, amt);
	}
	bool AddResourcePool(ResourceData resource, float amount)
	{
		if (resourcePoolsDict.ContainsKey(resource.key))
			return false;

		ResourceValue rv = new ResourceValue(resource, amount);
		m_resourcePoolsList.Add(rv);
		m_resourcePoolsDict.Add(resource.key, rv);
		return true;
	}

	public float GetResourceRate(ResourceData rData)
	{
		if (m_resourceRatesDict.ContainsKey(rData.key))
			return m_resourceRatesDict[rData.key].value;
		return 0.0f;
	}
	public void AddResourceRate(ResourceData rData, float rate)
	{
		// Avoids copying value-type
		if (m_resourceRatesDict.ContainsKey(rData.key))
			m_resourceRatesDict[rData.key].SetValue(rate + m_resourceRatesDict[rData.key].value);
		else AddResourceRateInternal(rData, rate);
	}
	public void SetResourceRate(ResourceData rData, float rate)
	{
		// Avoids copying value-type
		if (m_resourceRatesDict.ContainsKey(rData.key))
			m_resourceRatesDict[rData.key].SetValue(rate);
		else AddResourceRateInternal(rData, rate);
	}
	public void SetAllResourceRates(float rate)
	{
		foreach (var rData in GameManager.instance.resourceList)
		{
			SetResourceRate(rData, rate);
		}
	}
	bool AddResourceRateInternal(ResourceData resource, float rate)
	{
		if (resourceRatesDict.ContainsKey(resource.key))
			return false;

		ResourceValue rv = new ResourceValue(resource, rate);
		m_resourceRatesList.Add(rv);
		m_resourceRatesDict.Add(resource.key, rv);
		return true;
	}
	
	/// <summary> Returns true if this node is connected to a node we own. </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	public bool IsNodeConnected(NodeController node)
	{
		foreach (var index in node.connections.Keys)
			if (ownedNodes.ContainsKey(index))
				return true;
		return false;
	}

	public bool CanAffordBuilding(BuildingData building)
	{
		foreach (var rv in building.buildingCostROC)
			if (GetResourcePool(rv.resource) < rv.value)
				return false;
		return true;
	}

	public void OnPlayerExpanded(Player expandingPlayer, NodeController claimedNode)
	{
		if(expandingPlayer.id != id)
		{
			foreach(var kvp in m_borderNodes)
			{
				int dist = GameManager.instance.DistanceToNode(kvp.Value, claimedNode);
				if (dist < kvp.Value.nearestOwnedDist)
					kvp.Value.SetNearestOwned(dist);
			}
		}
		else
		{
			expansionRate = nnManager.completionTime - lastExpansion;
			lastExpansion = nnManager.completionTime;
		}
	}

	public void OnNodeStolen(Player aggressor)
	{
		nnManager.playerAttacksDict[aggressor.id]++;
	}
}

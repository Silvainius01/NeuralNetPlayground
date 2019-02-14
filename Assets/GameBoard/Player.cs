using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	public int id { get; private set; }
	public Color color;
	public Vector2Int territoryStats = new Vector2Int();
	Dictionary<int, NodeController> ownedNodes;
	Dictionary<int, NodeController> borderNodes;

	[SerializeField] List<ResourceValue> m_resourcePoolsList;
	Dictionary<int, ResourceValue> m_resourcePoolsDict;

	Queue<int> undevelopedNodes = new Queue<int>();

	public ReadOnlyCollection<ResourceValue> resourcePoolsList;
	public ReadOnlyDictionary<int, ResourceValue> resourcePoolsDict;

	public void Init(int playerID, Color playerColor)
	{
		id = playerID;
		color = playerColor;
		ownedNodes = new Dictionary<int, NodeController>(GameManager.instance.numNodes);
		borderNodes = new Dictionary<int, NodeController>(GameManager.instance.numNodes);

		// Init resource pools
		m_resourcePoolsList = new List<ResourceValue>(GameManager.instance.resourceList.Count);
		m_resourcePoolsDict = new Dictionary<int, ResourceValue>(GameManager.instance.resourceList.Count);

		resourcePoolsList = new ReadOnlyCollection<ResourceValue>(m_resourcePoolsList);
		resourcePoolsDict = new ReadOnlyDictionary<int, ResourceValue>(m_resourcePoolsDict);
	}

	public void UpdatePlayer()
	{
		// Update resource pools
		foreach (var kvp in ownedNodes)
			foreach (var rType in GameManager.instance.resourceList)
				AddResourcesToPool(rType, kvp.Value.resourceCont.GetResourceRate(rType) * Time.deltaTime);

		territoryStats.x = ownedNodes.Count;
		territoryStats.y = borderNodes.Count;
		
		if(undevelopedNodes.Count > 0)
		{
			// Remove nodes until we find one we own.
			while (undevelopedNodes.Count > 0 && !ownedNodes.ContainsKey(undevelopedNodes.Peek()))
				undevelopedNodes.Dequeue();

			// Attempt to purchase a random economic building.
			var node = ownedNodes[undevelopedNodes.Peek()];
			GameManager.instance.AttemptToBuyBuilding(this, node, GameManager.instance.GetRandomBuildingOfType(BUILDING_TYPE.ECONOMIC));

			// Remove node if we have fully developed it.
			if (!node.buildingCont.CanSupportBuildingType(BUILDING_TYPE.ECONOMIC))
				undevelopedNodes.Dequeue();
		}
		else if (GameManager.instance.CanPlayerClaimEmptyNode(this))
		{
			GameManager.instance.TransferNodeToPlayer(GetRandomBorderNode(), this);
		}
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
				borderNodes[n.index] = n;

		// Remove this node from border nodes
		if (borderNodes.ContainsKey(node.index))
			borderNodes.Remove(node.index);
	}
	public void RemoveNode(NodeController node)
	{
		if (!ownedNodes.ContainsKey(node.index))
			return;
		
		ownedNodes.Remove(node.index);

		// Remove any unconnected border nodes
		foreach (var n in node.connections.Values)
			if (!IsNodeConnected(n) && borderNodes.ContainsKey(n.index))
				borderNodes.Remove(n.index);

		// If this node is still connected to our territory, add it to borders.
		if (IsNodeConnected(node))
			borderNodes[node.index] = node;
	}

	public NodeController GetRandomOwnedNode()
	{
		if (ownedNodes.Count < 1)
			return null;
		return Mathc.GetRandomValueFromDict(ref ownedNodes);
	}
	public NodeController GetRandomBorderNode()
	{
		if (borderNodes.Count < 1)
			return null;
		return Mathc.GetRandomValueFromDict(ref borderNodes);
	}
	#endregion

	public float GetCurrentResourcePool(ResourceData resource)
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

	bool AddResourcePool(ResourceData resource, float amount)
	{
		if (resourcePoolsDict.ContainsKey(resource.key))
			return false;

		ResourceValue rv = new ResourceValue(resource, amount);
		m_resourcePoolsList.Add(rv);
		m_resourcePoolsDict.Add(resource.key, rv);
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
			if (GetCurrentResourcePool(rv.resource) < rv.value)
				return false;
		return true;
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class NodeController : MonoBehaviour
{
	public int index;
	[SerializeField] SpriteRenderer sprite;
	public Dictionary<int, NodeController> connections;

	public Player owner { get; private set; }
	GameManager gameManager;
	GraphMaker.GraphPoint graphPoint;

	public int nearestOwnedDist { get; private set; }
	public Vector2Int boardPosition { get { return graphPoint.boardPos; } }

	public NodeResourceCont resourceCont;
	public NodeBuildingCont buildingCont;

	public Color color
	{
		get { return sprite.color; }
		set { sprite.color = value; }
	}
	Color defaultColor;

	public Dictionary<int, System.Tuple<int, uint>> playerDistanceDict = new Dictionary<int, System.Tuple<int, uint>>();
	public void Init(int index, GraphMaker.GraphPoint point, Color startColor)
	{
		owner = null;
		color = startColor;
		defaultColor = startColor;
		sprite.color = color;
		graphPoint = point;
		transform.position = point.position;
		this.index = index;
		gameManager = GameManager.instance;
		resourceCont = new NodeResourceCont(this);
		buildingCont = new NodeBuildingCont(this);
		connections = new Dictionary<int, NodeController>(point.connections.Count);
		foreach (var c in point.connections)
		{
			ConnectToNode(gameManager.GetNodeFromIndex(c.index));
		}

		Reset();
	}

	public void Reset()
	{
		buildingCont.RemoveAllBuildings();
		resourceCont.SetAllResourceRates(0.0f);
		resourceCont.RemoveFromOwnerResourceRate();
		nearestOwnedDist = gameManager.graphMaker.dimensions.x + gameManager.graphMaker.dimensions.y;
		
		foreach (var p in GameManager.instance.players)
			playerDistanceDict[p.id] = new System.Tuple<int, uint>(int.MaxValue, 0);

		SetOwner(null);
	}

	void ConnectToNode(NodeController node)
	{
		if (node == null || IsConnectedTo(node))
			return;

		connections.Add(node.index, node);
		node.ConnectToNode(this);
	}

	public void SetOwner(Player player)
	{
		if (owner != null)
		{
			owner.RemoveNode(this);
			resourceCont.RemoveFromOwnerResourceRate();
		}
		if (player != null)
		{
			owner = player;
			color = player.color;
			player.AddNode(this);
			resourceCont.AddToOwnerResourceRate();
		}
		else
		{
			owner = null;
			color = defaultColor;
		}
	}

	public NodeController GetRandomConnection()
	{
		return Mathc.GetRandomValueFromDict(ref connections);
	}

	public bool IsOwned()
	{
		return owner != null;
	}
	public bool IsOwnedBy(Player player)
	{
		if (owner == null)
			return false;
		return player.id == owner.id;
	}
	public bool IsConnectedTo(NodeController node)
	{
		return connections.ContainsKey(node.index);
	}

	public void SetNearestOwned(int v)
	{
		nearestOwnedDist = v;
	}
}

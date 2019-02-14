using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class NodeController : MonoBehaviour
{
	public int index;
	[SerializeField] SpriteRenderer sprite;
	public Dictionary<int, NodeController> connections;

	Player owner;
	GameManager gameManager;
	GraphMaker.GraphPoint graphPoint;

	public NodeResourceCont resourceCont;
	public NodeBuildingCont buildingCont;

	public Color color
	{
		get { return sprite.color; }
		set { sprite.color = value; }
	}

	public void Init(int index, GraphMaker.GraphPoint point)
	{
		owner = null;
		color = Color.grey;
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
		if(owner!=null)
			owner.RemoveNode(this);
		if (player != null)
		{
			owner = player;
			color = player.color;
			player.AddNode(this);
		}
		else
		{
			owner = null;
			color = Color.grey;
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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Basic node type. Simply sends and receives values. </summary>
[System.Serializable]
public class Node
{
	int id;
	int receivedInputs = 0;
	float value = 0.0f;
	public float currentValue { get { return value / receivedInputs; } }
	[SerializeField] List<Axon> axons = new List<Axon>();

	public Node(int id)
	{
		this.id = id;
	}
	public Node(int id, params Node[] connectedNodes)
	{
		this.id = id;
		foreach (var node in connectedNodes)
		{
			axons.Add(new Axon(0.0f, node));
		}
	}
	
	public void FlushValue()
	{
		value = 0;
		receivedInputs = 0;
	}
	public void SendOutput()
	{
		foreach (var axon in axons)
			axon.TransmiteValue(currentValue);
		FlushValue();
	}
	public void ReceiveInput(float input)
	{
		// A node sets its value to the average of all received values.
		++receivedInputs;
		value += ProcessValue(input);
	}

	public void RemoveConnection(Node n)
	{
		for (int i = 0; i < axons.Count; ++i)
			if (axons[i].receivingNode == n)
			{
				axons.RemoveAt(i);
				break;
			}
	}
	public void RemoveAllConnections()
	{
		axons.Clear();
	}
	public void ConnectNode(Node n, float weight)
	{
		axons.Add(new Axon(weight, n));
	}
	public void RefreshConnection(Node n)
	{
		float weight = 0.0f;
		foreach (var axon in axons)
			if (axon.receivingNode.id == n.id)
			{
				weight = axon.weight;
				RemoveConnection(axon.receivingNode);
				break;
			}
		ConnectNode(n, weight);
	}

	protected virtual float ProcessValue(float input)
	{
		return input;
	}

	/// <summary> Copies connection WEIGHTS top down. </summary>
	/// <param name="n"></param>
	public void CopyWeightsFrom(Node n)
	{
		int mIndex = n.axons.Count > axons.Count ? axons.Count : n.axons.Count;
		for (int i = 0; i < mIndex; ++i)
		{
			axons[i] = new Axon(n.axons[i].weight, axons[i].receivingNode);
		}
	}

#if UNITY_EDITOR
	public Vector3 pos;

	public void DrawConnections(Gradient relationGradient)
	{
		foreach (var axon in axons)
		{
			Gizmos.color = relationGradient.Evaluate(Mathc.NormalizeBetween(axon.weight, -1, 1));
			Gizmos.DrawLine(pos, axon.receivingNode.pos);
		}
	}

	public void MutateConnections(float chance)
	{
		foreach (var axon in axons)
			axon.Mutate(chance);
	}
	public void InheritConnections(Node parent)
	{
		int mIndex = axons.Count > parent.axons.Count ? parent.axons.Count : axons.Count;
		for(int i = 0; i < axons.Count; ++i)
		{
			if (Random.value <= 0.5f)
				axons[i].weight = parent.axons[i].weight;
		}
	}
#endif
}

[System.Serializable]
public class Axon
{
	public float weight;
	public Node receivingNode;

	public Axon(float weight, Node receivingNode)
	{
		this.weight = weight;
		this.receivingNode = receivingNode;
	}
	public void TransmiteValue(float value)
	{
		receivingNode.ReceiveInput(value * weight);
	}

#if UNITY_EDITOR
	public void Mutate(float chance)
	{
		if (Random.value < chance)
		{
			weight += Mathc.Random.Marsaglia(true);
			weight = Mathf.Clamp(weight, -1, 1);
		}
	}
#endif
}

[System.Serializable]
public class NodeListWrapper : IEnumerable<Node>
{
	public List<Node> nodeList = new List<Node>();

	public int Count { get { return nodeList.Count; } }
	public Node this[int i]
	{
		get { return nodeList[i]; }
		set { nodeList[i] = value; }
	}

	public NodeListWrapper(List<Node> list)
	{
		nodeList = list;
	}
	public void Add(Node n)
	{
		nodeList.Add(n);
	}

	public IEnumerator<Node> GetEnumerator()
	{
		return nodeList.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return nodeList.GetEnumerator();
	}
	
	public static implicit operator List<Node>(NodeListWrapper nlw)
	{
		return nlw.nodeList;
	}
	public static implicit operator NodeListWrapper(List<Node> list)
	{
		return new NodeListWrapper(list);
	}
}

public partial class NeuralNetwork : MonoBehaviour
{
	bool isBuilt;
	bool isConnected;


	[SerializeField] protected List<NodeListWrapper> nodeLayers = new List<NodeListWrapper>();

	NodeListWrapper inputLayer { get { return nodeLayers[0]; } }
	NodeListWrapper outputLayer { get { return nodeLayers[nodeLayers.Count - 1]; } }

	public int numLayers { get { return nodeLayers.Count; } }
	public int numInputs { get { return nodeLayers[0].Count; } }
	public int numOutputs { get { return nodeLayers[nodeLayers.Count - 1].Count; } }

	/// <summary> Builds a new network where all nodes in one layer connect to all other nodes in the next with random weights. </summary>
	/// <param name="inputHeight">How many input nodes?</param> 
	/// <param name="outputHeight">How many output nodes?</param>
	/// <param name="hiddenLayerDim">Num hidden layers (x) and height of the layers (y)</param>
	public void BuildDefaultNetwork(params int[] layerHeights)
	{
		int currId = 0;
		nodeLayers.Clear();

		for(int i = 0; i < layerHeights.Length; ++i)
		{
			nodeLayers.Add(new List<Node>());
			for (int j = 0; j < layerHeights[i]; ++j)
				nodeLayers[i].Add(new Node(currId++));
		}

		isBuilt = true;
		ConnectNetworkFresh();
	}

	/// <summary> Fixes any broken references or connections that have occured. One case is recompilation of code. </summary>
	public void RefreshConnections()
	{
		for (int i = 0; i < nodeLayers.Count - 1; ++i)
			foreach (var n0 in nodeLayers[i])
				foreach (var n1 in nodeLayers[i + 1])
					n0.RefreshConnection(n1);
	}
	public void ConnectNetworkFresh()
	{
		for (int i = 0; i < nodeLayers.Count - 1; ++i)
			foreach (var n0 in nodeLayers[i])
			{
				n0.RemoveAllConnections();
				foreach (var n1 in nodeLayers[i + 1])
					n0.ConnectNode(n1, Random.value * 2 - 1);
			}
		isConnected = true;
	}

	public float[] Evaluate(params float[] inputs)
	{
		if (inputs.Length != numInputs)
		{
			Debug.LogError("Num inputs does not match node count!!");
			return null;
		}

		float[] retval = new float[numOutputs];

		// Give the network the inputs, then send them to next layer.
		for (int i = 0; i < numInputs; ++i)
		{
			inputLayer[i].ReceiveInput(inputs[i]);
			inputLayer[i].SendOutput();
		}

		// Send inputs layer to layer.
		for(int i = 1; i < nodeLayers.Count-1; ++i)
			foreach (var node in nodeLayers[i])
				node.SendOutput();

		for (int i = 0; i < outputLayer.Count; ++i)
		{
			retval[i] = outputLayer[i].currentValue;
			outputLayer[i].FlushValue();
		}

		return retval;
	}


	public bool CopyConnectionsFrom(NeuralNetwork network)
	{
		if(!IsCompatibleWith(network))
			return false;

		for (int i = 0; i < nodeLayers.Count; ++i)
			for (int j = 0; j < nodeLayers[i].Count; ++j)
				nodeLayers[i][j].CopyWeightsFrom(network.nodeLayers[i][j]);
		return true;
	}
	public void CopyNetworkFrom(NeuralNetwork network)
	{
		nodeLayers.Clear();
		int[] layerHeights = new int[network.nodeLayers.Count];
		for (int i = 0; i < network.nodeLayers.Count; ++i)
			layerHeights[i] = network.nodeLayers[i].Count;
		BuildDefaultNetwork(layerHeights);
		CopyConnectionsFrom(network);

#if UNITY_EDITOR
		editorUtil.owner = this;
		editorUtil.nodeHeights = new List<int>(layerHeights);
		editorUtil.CacheGizmoDrawData();
#endif
	}

	public void MutateAsexual(float mutationProb)
	{
		for (int i = 0; i < nodeLayers.Count; ++i)
			foreach (var n0 in nodeLayers[i])
				n0.MutateConnections(mutationProb);
	}
	public void MutateSexual(NeuralNetwork partner, float mutationProb)
	{
		if (!IsCompatibleWith(partner))
			return;
		for (int i = 0; i < nodeLayers.Count; ++i)
			for (int j = 0; j < nodeLayers.Count; ++i)
				nodeLayers[i][j].InheritConnections(partner.nodeLayers[i][j]);
		MutateAsexual(mutationProb);
	}
	public bool IsCompatibleWith(NeuralNetwork partner)
	{
		if (nodeLayers.Count != partner.nodeLayers.Count)
			return false;
		for (int i = 0; i < nodeLayers.Count; ++i)
			if (nodeLayers[i].Count != partner.nodeLayers[i].Count)
				return false;
		return true;
	}
}

partial class NeuralNetwork
{
#if UNITY_EDITOR
	[System.Serializable]
	public class Editor
	{
		[Header("Network Options")]
		public bool copyValues;
		public NeuralNetwork copyTarget;
		public NeuralNetwork owner;
		
		[Header("Gizmo Settings")]
		public bool gizmoSettingsSet = false;
		public List<int> nodeHeights = new List<int>() { 1, 1 };

		Vector3 displayPos;
		List<Vector3> layerStartList = new List<Vector3>();

		public static Color nodeColor = Color.cyan;
		public static Gradient axonGradient = GetDefaultGradient();
		public static float nodeSize = 1;
		public static float nodeSpacing = 5;
		public static float layerSpacing = 5;
		public static Gradient GetDefaultGradient()
		{
			Gradient rv = new Gradient();
			rv.SetKeys(
				new GradientColorKey[2] {
					new GradientColorKey(Color.red, 0.0f),
					new GradientColorKey(Color.green, 1.0f) },
				new GradientAlphaKey[3] {
					new GradientAlphaKey(1.0f, 0.0f),
					new GradientAlphaKey(0.0f, 0.5f),
					new GradientAlphaKey(1.0f, 1.0f) }
				);
			return rv;
		}

		public static void CacheGizmoDrawData(NeuralNetwork target)
		{
			target.editorUtil.CacheGizmoDrawData();
		}
		public static void CacheGizmoDrawData(NeuralNetwork target, Vector3 pos)
		{
			target.editorUtil.CacheGizmoDrawData();
			target.editorUtil.CacheNodeTransformPos(pos);
		}
		public static void CacheNodePositionChange(NeuralNetwork target, Vector3 pos)
		{
			target.editorUtil.CacheNodeTransformPos(pos);
		}
		public static void RefreshNetwork(NeuralNetwork target)
		{
			target.editorUtil.BuildInspectorNetwork();
		}
		public static void CleanEditorUtil(NeuralNetwork target)
		{
			if (target.editorUtil == null)
			{
				target.editorUtil = new Editor();
				target.editorUtil.owner = target;
			}
			else if (target.editorUtil.owner == null)
				target.editorUtil.owner = target;
		}

		public void CacheGizmoDrawData()
		{
			if (owner.numLayers <= 0)
				return;

			float xPos = 0.0f;
			float layerHeight = 0.0f;
			float startX = 0.0f;
			layerStartList.Clear();
			layerStartList.Capacity = owner.numLayers;

			float GetLayerHeight(int height)
			{
				float rv = 0.0f;
				rv = nodeSize * height * 2;
				rv += nodeSpacing * (height - 1);
				rv = (rv / 2.0f) - nodeSize;
				return rv;
			};

			// Input layer start height
			layerHeight = GetLayerHeight(owner.nodeLayers[0].Count);
			// Network starting X
			startX = nodeSize * owner.numLayers * 2;
			startX += layerSpacing * (owner.numLayers - 1);
			startX = -((startX / 2.0f) - nodeSize);

			layerStartList.Add(new Vector3(startX, layerHeight));

			// Determines the starting position for node[0] for each layer
			for(int i = 1; i < owner.nodeLayers.Count; ++i)
			{
				var layer = owner.nodeLayers[i];
				layerHeight = GetLayerHeight(layer.Count);
				xPos = startX + (nodeSize * 2 * i) + (layerSpacing * i);
				layerStartList.Add(new Vector3(xPos, layerHeight));
			}

			for (int i = 0; i < layerStartList.Count; ++i)
				layerStartList[i] += owner.transform.position;

			// Node draw positions
			for(int i = 0; i < owner.nodeLayers.Count; ++i)
				for(int j = 0; j < owner.nodeLayers[i].Count; ++j)
				{
					owner.nodeLayers[i][j].pos = layerStartList[i];
					owner.nodeLayers[i][j].pos.y = layerStartList[i].y - (nodeSize * 2 * j) - (nodeSpacing * j);
				}

			gizmoSettingsSet = true;
		}
		void CacheNodeTransformPos(Vector3 previousPos)
		{
			foreach(var layer in owner.nodeLayers)
				foreach(var node in layer)
					node.pos += (-previousPos) + owner.transform.position;
		}
		public void BuildInspectorNetwork()
		{
			owner.BuildDefaultNetwork(nodeHeights.ToArray());
		}
	}

	[SerializeField] Editor editorUtil = new Editor();

	void OnDrawGizmos()
	{
		DisplayNetworkGizmo();
	}

	void DisplayNetworkGizmo()
	{
		if (!editorUtil.gizmoSettingsSet)
		{
			Editor.CleanEditorUtil(this);

			if (!isBuilt)
				Editor.RefreshNetwork(this);
			else Editor.CacheGizmoDrawData(this);

			Debug.LogError("Cannot display network debug, gizmo data not set!");
			return;
		}

		DrawConnections();
		DrawNodes();
	}

	void DrawNodes()
	{
		Gizmos.color = Editor.nodeColor;
		foreach (var layer in nodeLayers)
			foreach (var node in layer)
				Gizmos.DrawSphere(node.pos, Editor.nodeSize);
	}
	void DrawConnections()
	{
		foreach (var layer in nodeLayers)
			foreach (var node in layer)
				node.DrawConnections(Editor.axonGradient);
	}
#endif
}
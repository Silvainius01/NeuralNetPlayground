using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Basic node type. Simply sends and receives values. </summary>
[System.Serializable]
public class Node
{
	int receivedInputs = 0;
	float value = 0.0f;
	public float currentValue { get { return value / receivedInputs; } }
	List<Axon> axons = new List<Axon>();

	public Node()
	{
	}
	public Node(params Node[] connectedNodes)
	{
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
		value = 0; receivedInputs = 0;
	}
	public void ReceiveInput(float input)
	{
		// A node sets its value to the average of all received values.
		++receivedInputs;
		value += ProcessValue(input);
	}

	public void ConnectNode(Node n, float weight)
	{
		axons.Add(new Axon(weight, n));
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

public class NeuralNetwork : MonoBehaviour
{
	bool isBuilt;
	bool isConnected;

	public int numLayers { get { return 2 + hiddenLayers.Count; } }
	protected List<Node> inputLayer = new List<Node>();
	protected List<List<Node>> hiddenLayers = new List<List<Node>>();
	protected List<Node> outputLayer = new List<Node>();

#if UNITY_EDITOR
	public void BuildDefaultNetwork()
	{
		BuildDefaultNetwork(numInputNodes, numOutputNodes, hiddenLayerNodes);
	}
#endif
	/// <summary> Builds a new network where all nodes in one layer connect to all other nodes in the next with random weights. </summary>
	/// <param name="inputHeight">How many input nodes?</param>
	/// <param name="outputHeight">How many output nodes?</param>
	/// <param name="hiddenLayerDim">Num hidden layers (x) and height of the layers (y)</param>
	public void BuildDefaultNetwork(int inputHeight, int outputHeight, List<int> hiddenLayerHeights)
	{
		if (inputLayer == null)
			inputLayer = new List<Node>();
		inputLayer.Clear();
		inputLayer.Capacity = inputHeight;
		for (int i = 0; i < inputHeight; ++i)
			inputLayer.Add(new Node());

		if (hiddenLayers == null)
			hiddenLayers = new List<List<Node>>();
		hiddenLayers.Clear();
		hiddenLayers.Capacity = hiddenLayerHeights.Count;
		for (int i = 0; i < hiddenLayerHeights.Count; ++i)
		{
			hiddenLayers.Add(new List<Node>(hiddenLayerHeights[i]));
			for (int j = 0; j < hiddenLayerHeights[i]; ++j)
				hiddenLayers[i].Add(new Node());
		}

		if (outputLayer == null)
			outputLayer = new List<Node>();
		outputLayer.Clear();
		outputLayer.Capacity = outputHeight;
		for (int i = 0; i < outputHeight; ++i)
			outputLayer.Add(new Node());

		isBuilt = true;
		ConnectNetwork();
	}

	/// <summary> Connects the whole network with fresh axons, with random values. </summary>
	public void ConnectNetwork()
	{
		if (hiddenLayers.Count > 0)
		{
			// Connect input to layer0
			foreach (var n0 in inputLayer)
				foreach (var n1 in hiddenLayers[0])
					n0.ConnectNode(n1, Random.value * 2 - 1);
			// Connect hidden layers
			for (int layerIndex = 0; layerIndex < hiddenLayers.Count - 1; ++layerIndex)
				for (int nodeIndex0 = 0; nodeIndex0 < hiddenLayers[layerIndex].Count; ++nodeIndex0)
					for (int nodeIndex1 = 0; nodeIndex1 < hiddenLayers[layerIndex + 1].Count; ++nodeIndex1)
						hiddenLayers[layerIndex][nodeIndex0].ConnectNode(hiddenLayers[layerIndex + 1][nodeIndex1], Random.value);
			// Connect last hidden layer to out put layer
			foreach (var n0 in hiddenLayers[hiddenLayers.Count - 1])
				foreach (var n1 in outputLayer)
					n0.ConnectNode(n1, Random.value * 2 - 1);
		}
		else
		{
			foreach (var n0 in inputLayer)
				foreach (var n1 in outputLayer)
					n0.ConnectNode(n1, Random.value * 2 - 1);
		}

		isConnected = true;
	}

	public float[] Evaluate(params float[] inputs)
	{
		if (inputs.Length != inputLayer.Count)
		{
			Debug.LogError("Num inputs does not match node count!!");
			return null;
		}

		float[] retval = new float[outputLayer.Count];

		// Give the network the inputs, then send them to next layer.
		for (int i = 0; i < inputLayer.Count; ++i)
		{
			inputLayer[i].ReceiveInput(inputs[i]);
			inputLayer[i].SendOutput();
		}

		// Send inputs layer to layer.
		foreach (var layer in hiddenLayers)
			foreach (var node in layer)
				node.SendOutput();

		for (int i = 0; i < outputLayer.Count; ++i)
		{
			retval[i] = outputLayer[i].currentValue;
			outputLayer[i].FlushValue();
		}

		return retval;
	}

	public void CopyFrom(NeuralNetwork network)
	{
		int mIndex = inputLayer.Count < network.inputLayer.Count ? inputLayer.Count : network.inputLayer.Count;
		for (int i = 0; i < mIndex; ++i)
		{
			inputLayer[i].CopyWeightsFrom(network.inputLayer[i]);
		}

		mIndex = hiddenLayers.Count < network.hiddenLayers.Count ? hiddenLayers.Count : network.hiddenLayers.Count;
		for (int i = 0; i < mIndex; ++i)
		{
			int nIndex = hiddenLayers[i].Count < network.hiddenLayers[i].Count ? hiddenLayers[i].Count : network.hiddenLayers[i].Count;
			for (int j = 0; j < nIndex; ++j)
				hiddenLayers[i][j].CopyWeightsFrom(network.hiddenLayers[i][j]);
		}

		mIndex = outputLayer.Count < network.outputLayer.Count ? outputLayer.Count : network.outputLayer.Count;
		for (int i = 0; i < mIndex; ++i)
		{
			outputLayer[i].CopyWeightsFrom(network.outputLayer[i]);
		}
	}

#if UNITY_EDITOR
	[Header("Network Options")]
	public bool copyValues;
	public NeuralNetwork copyTarget;

	public void MutateAsexual(float mutationProb)
	{
		foreach (var node in inputLayer)
			node.MutateConnections(mutationProb);
		foreach (var layer in hiddenLayers)
			foreach (var node in layer)
				node.MutateConnections(mutationProb);
		foreach (var node in outputLayer)
			node.MutateConnections(mutationProb);
	}
	public void MutateSexual(NeuralNetwork partner, float mutationProb)
	{
		if (!IsCompatibleWith(partner))
			return;
		for (int i = 0; i < inputLayer.Count; ++i)
			inputLayer[i].InheritConnections(partner.inputLayer[i]);
		for (int i = 0; i < hiddenLayers.Count; ++i)
			for (int j = 0; j < hiddenLayers[j].Count; ++j)
				hiddenLayers[i][j].InheritConnections(partner.hiddenLayers[i][j]);
		for (int i = 0; i < outputLayer.Count; ++i)
			outputLayer[i].InheritConnections(partner.outputLayer[i]);
		MutateAsexual(mutationProb);
	}

	public bool IsCompatibleWith(NeuralNetwork partner)
	{
		if (inputLayer.Count != partner.inputLayer.Count)
			return false;
		if (outputLayer.Count != partner.outputLayer.Count)
			return false;
		if (hiddenLayers.Count != partner.hiddenLayers.Count)
			return false;
		for (int i = 0; i < hiddenLayers.Count; ++i)
			if (hiddenLayers[i].Count != partner.hiddenLayers[i].Count)
				return false;
		return true;
	}

	#region Gizmo Stuff
	[Header("Gizmo Settings")]
	public bool build;
	public bool draw;
	public bool gizmoSettingsSet = false;
	public int numInputNodes;
	public List<int> hiddenLayerNodes;
	public int numOutputNodes;
	public Color nodeColor;
	public Gradient relationGradient;

	Vector3 displayPos;
	public float circleRadius;
	public float spaceBetweenNodes;
	public float spaceBetweenLayers;
	List<Vector3> layerStartList = new List<Vector3>();

	public void CacheGizmoDrawData()
	{
		float xPos = 0.0f;
		float layerHeight = 0.0f;
		float startX = 0.0f;
		layerStartList.Clear();
		layerStartList.Capacity = numLayers - 1;

		// Input layer start height
		layerHeight = GetLayerHeight(inputLayer.Count);
		// Network starting X
		startX = circleRadius * numLayers * 2;
		startX += spaceBetweenLayers * (numLayers - 1);
		startX = -((startX / 2.0f)  - circleRadius);

		layerStartList.Add(new Vector3(startX, layerHeight));

		// Hidden layer starting heights
		for (int i = 0; i < hiddenLayers.Count; ++i)
		{
			var layer = hiddenLayers[i];
			layerHeight = GetLayerHeight(layer.Count);
			xPos = startX + ((circleRadius * 2) * (i + 1)) + (spaceBetweenLayers * (i + 1));
			layerStartList.Add(new Vector3(xPos, layerHeight));
		}
		// Output layer start height
		layerHeight = GetLayerHeight(outputLayer.Count);
		xPos = startX + ((circleRadius * 2) * (numLayers - 1)) + (spaceBetweenLayers * (numLayers - 1));
		layerStartList.Add(new Vector3(xPos, layerHeight));

		for (int i = 0; i < layerStartList.Count; ++i)
			layerStartList[i] += transform.position;

		// Set node draw positions.
		for (int i = 0; i < inputLayer.Count; ++i)
		{
			inputLayer[i].pos = layerStartList[0];
			inputLayer[i].pos.y = layerStartList[0].y - ((circleRadius * 2) * i) - (spaceBetweenNodes * i);
		}
		for (int i = 0; i < hiddenLayers.Count; ++i)
		{
			for (int j = 0; j < hiddenLayers[i].Count; ++j)
			{
				hiddenLayers[i][j].pos = layerStartList[i + 1];
				hiddenLayers[i][j].pos.y = layerStartList[i + 1].y - ((circleRadius * 2) * j) - (spaceBetweenNodes * j);
			}
		}
		for (int i = 0; i < outputLayer.Count; ++i)
		{
			outputLayer[i].pos = layerStartList[layerStartList.Count - 1];
			outputLayer[i].pos.y = layerStartList[layerStartList.Count - 1].y - ((circleRadius * 2) * i) - (spaceBetweenNodes * i);
		}

		gizmoSettingsSet = true;
	}

	float GetLayerHeight(int nodeCount)
	{
		float rv = 0.0f;
		rv = circleRadius * nodeCount * 2;
		rv += spaceBetweenNodes * (nodeCount - 1);
		rv = (rv / 2.0f) - circleRadius;
		return rv;
	}

	public void OnDrawGizmos()
	{
		if (!gizmoSettingsSet)
			CacheGizmoDrawData();
		if(build)
		{
			BuildDefaultNetwork(numInputNodes, numOutputNodes, hiddenLayerNodes);
			CacheGizmoDrawData();
			build = false;
		}
		if(copyValues)
		{
			CopyFrom(copyTarget);
			CacheGizmoDrawData();
			copyValues = false;
		}
		if(draw)
		{
			DisplayNetworkGizmo();
		}
	}

	public void DisplayNetworkGizmo()
	{
		if (!gizmoSettingsSet)
		{
			Debug.LogError("Cannot display network debug, gizmo data not set!");
			return;
		}

		DrawConnections();
		DrawNodes();
	}

	void DrawNodes()
	{
		Gizmos.color = nodeColor;
		foreach (var node in inputLayer)
			Gizmos.DrawSphere(node.pos, circleRadius);
		foreach (var layer in hiddenLayers)
			foreach (var node in layer)
				Gizmos.DrawSphere(node.pos, circleRadius);
		foreach (var node in outputLayer)
			Gizmos.DrawSphere(node.pos, circleRadius);
	}
	void DrawConnections()
	{
		foreach (var node in inputLayer)
			node.DrawConnections(relationGradient);
		foreach (var layer in hiddenLayers)
			foreach (var node in layer)
				node.DrawConnections(relationGradient);
		foreach (var node in outputLayer)
			node.DrawConnections(relationGradient);
	}
	#endregion
#endif
}

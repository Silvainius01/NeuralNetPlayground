using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Basic node type. Simply sends and receives values. </summary>
[System.Serializable]
public class Neuron
{
	int id;
	int receivedInputs = 0;
	float value = 0.0f;
	public float currentValue { get { return ProcessValue(value + bias); } }
	[SerializeField] float bias = 0.0f;
	[SerializeField] List<Axon> axons = new List<Axon>();

	public Neuron(int id)
	{
		this.id = id;
	}
	public Neuron(int id, float bias)
	{
		this.id = id;
		this.bias = bias;
	}
	public Neuron(int id, params Neuron[] connectedNodes)
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

	public bool RemoveConnection(Neuron n)
	{
		for (int i = 0; i < axons.Count; ++i)
			if (axons[i].receivingNode == n)
			{
				axons[i].receivingNode = null;
				axons.RemoveAt(i);
				return true;
			}
		return false;
	}
	public void RemoveAllConnections()
	{
		axons.Clear();
	}
	public void ConnectNode(Neuron n, float weight)
	{
		axons.Add(new Axon(weight, n));
	}
	public bool RefreshConnection(Neuron n)
	{
		float weight = 0.0f;
		foreach (var axon in axons)
			if (axon.receivingNode.id == n.id)
			{
				weight = axon.weight;
				RemoveConnection(axon.receivingNode);
				ConnectNode(n, weight);
				return true;
			}
		return false;
	}
	/// <summary>
	/// This function is weird. it takes in two nodes, and "gives" its connection to 'newConnection', and then disconnects itself from 'connectedNode'.
	/// </summary>
	/// <param name="connectedNode">The node that is currently connected</param>
	/// <param name="newConnection">The node you want to be connected instead of this one</param>
	/// <param name="replaceWeight">If you want to overwrite the current connection weight, set this to true.</param>
	/// <param name="weightVal">If overwriting the current weight, this value is used as the new weight.</param>
	/// <returns></returns>
	public bool ReplaceConnection(Neuron connectedNode, Neuron newConnection, bool replaceWeight, float weightVal = 0.0f)
	{
		foreach(var axon in axons)
			if(axon.receivingNode.id == connectedNode.id)
			{
				float w = axon.weight;
				RemoveConnection(axon.receivingNode);
				newConnection.ConnectNode(connectedNode, replaceWeight ? weightVal : w);
				return true;
			}
		return false;
	}

	protected virtual float ProcessValue(float value)
	{
		return value / receivedInputs;
	}

	/// <summary> Copies connection WEIGHTS top down. </summary>
	/// <param name="n"></param>
	public void CopyWeightsFrom(Neuron n)
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

	public void MutateAsexual(float chance)
	{
		foreach (var axon in axons)
			axon.MutateAsexual(chance);
		if(Random.value < chance)
			bias += bias * Mathc.Random.Marsaglia(true);
	}
	public void MutateSexual(Neuron parent)
	{
		//int mIndex = axons.Count > parent.axons.Count ? parent.axons.Count : axons.Count;
		for(int i = 0; i < axons.Count; ++i)
			axons[i].MutateSexual(parent.axons[i]);
		bias = Mathc.Random.GetMarsagliaBetween(bias, parent.bias);
	}
#endif
}

[System.Serializable]
public class Axon
{
	public float weight;
	public Neuron receivingNode;

	public Axon(float weight, Neuron receivingNode)
	{
		this.weight = weight;
		this.receivingNode = receivingNode;
	}
	public void TransmiteValue(float value)
	{
		receivingNode.ReceiveInput(value * weight);
	}

#if UNITY_EDITOR
	public void MutateAsexual(float chance)
	{
		if (Random.value < chance)
		{
			weight += Mathc.Random.Marsaglia(true);
			weight = Mathf.Clamp(weight, -1, 1);
		}
	}
	public void MutateSexual(Axon axon)
	{
		// Modify the value of the weight by getting the mean of the two axons, then allowing that value to shift a bit.
		weight = Mathc.Random.GetMarsagliaBetween(weight, axon.weight);
	}
#endif
}

[System.Serializable]
public class NeuronListWrapper : IEnumerable<Neuron>
{
	public List<Neuron> nodeList = new List<Neuron>();

	public int Count { get { return nodeList.Count; } }
	public Neuron this[int i]
	{
		get { return nodeList[i]; }
		set { nodeList[i] = value; }
	}

	public NeuronListWrapper(List<Neuron> list)
	{
		nodeList = list;
	}
	public void Add(Neuron n)
	{
		nodeList.Add(n);
	}
	public void RemoveAt(int index)
	{
		nodeList.RemoveAt(index);
	}

	public IEnumerator<Neuron> GetEnumerator()
	{
		return nodeList.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return nodeList.GetEnumerator();
	}
	
	public static implicit operator List<Neuron>(NeuronListWrapper nlw)
	{
		return nlw.nodeList;
	}
	public static implicit operator NeuronListWrapper(List<Neuron> list)
	{
		return new NeuronListWrapper(list);
	}
}

public partial class NeuralNetwork : MonoBehaviour
{
	bool isBuilt;
	bool isConnected;
	
	[SerializeField] protected List<NeuronListWrapper> nodeLayers = new List<NeuronListWrapper>();

	NeuronListWrapper inputLayer { get { return nodeLayers[0]; } }
	NeuronListWrapper outputLayer { get { return nodeLayers[nodeLayers.Count - 1]; } }

	public int layerCount { get { return nodeLayers.Count; } }
	public int inputCount { get { return nodeLayers[0].Count; } }
	public int ouputCount { get { return nodeLayers[nodeLayers.Count - 1].Count; } }
	public int nodeCount { get; private set; }

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
			nodeLayers.Add(new List<Neuron>());
			for (int j = 0; j < layerHeights[i]; ++j)
				nodeLayers[i].Add(new Neuron(currId++));
		}

		nodeCount = currId;
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
		if (inputs.Length != inputCount)
		{
			Debug.LogError("Num inputs does not match node count!!");
			return null;
		}

		float[] retval = new float[ouputCount];

		// Give the network the inputs, then send them to next layer.
		for (int i = 0; i < inputCount; ++i)
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

	public string MutateAsexual(float mutationProb, float newNodeProb, float newLayerProb, bool randomWeights)
	{
		string retval = "";
		for (int i = 0; i < nodeLayers.Count; ++i)
			foreach (var n0 in nodeLayers[i])
				n0.MutateAsexual(mutationProb);
		if (layerCount > 2 && Random.value < newNodeProb)
		{
			int i = Random.Range(1, layerCount - 1);
			AddNodeToLayer(i, randomWeights);
			retval += $"(N{i})";
		}
		if(Random.value < newLayerProb)
		{
			int index = Random.Range(1, layerCount);
			int numNodes = nodeLayers[index].Count;
			if(randomWeights)
			{
				foreach (var l in nodeLayers)
					if (l.Count > numNodes)
						numNodes = l.Count;
			}
			numNodes = randomWeights ? Random.Range(1, numNodes + 1) : numNodes;
			AddLayer(index, numNodes, false, randomWeights);
			retval += $"(L{index} {numNodes})";
		}
		return retval;
	}
	public void MutateSexual(NeuralNetwork partner, float mutationProb)
	{
		if (!IsCompatibleWith(partner))
			return;
		for (int i = 0; i < nodeLayers.Count; ++i)
			for (int j = 0; j < nodeLayers.Count; ++i)
				nodeLayers[i][j].MutateSexual(partner.nodeLayers[i][j]);
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
	
	public bool RemoveNodeFromLayer(int layerIndex)
	{
		if (!Mathc.ValueIsBetween(layerIndex, -1, layerCount, true))
			return false;

		var layer = nodeLayers[layerIndex];
		var neuron = layer[layer.Count - 1];

		if (layerIndex > 0)
			foreach (var n in nodeLayers[layerIndex - 1])
				n.RemoveConnection(neuron);
		neuron.RemoveAllConnections();

		layer.RemoveAt(layer.Count - 1);
		nodeCount--;

		return true;
	}
	public bool AddNodeToLayer(int layer, bool randomWeights, float weightVal = 0.0f, float biasVal = 0.0f)
	{
		if (!Mathc.ValueIsBetween(layer, -1, layerCount, true))
			return false;

		Neuron newNode = new Neuron(nodeCount++, randomWeights ? Random.value * 2 - 1 : biasVal);
		nodeLayers[layer].Add(newNode);

		if (layer > 0)
			foreach (var node in nodeLayers[layer - 1])
				node.ConnectNode(newNode, randomWeights ? Random.value * 2 - 1 : weightVal);
		if(layer < layerCount-1)
			foreach(var node in nodeLayers[layer + 1])
				newNode.ConnectNode(node, randomWeights ? Random.value * 2 - 1 : weightVal);
		return true;
	}
	/// <summary>  </summary>
	/// <param name="index"></param>
	/// <param name="numNodes"></param>
	/// <param name="replaceAxonWeights">
	/// If you do not want to change the inputs going into this layer, set to false.
	/// NOTE: If the layer does not contain the same number of nodes as the one before it, THIS OPTION WILL DO NOTHING!!
	/// </param>
	/// <param name="randomWeights"></param>
	/// <param name="weightVal"></param>
	/// <param name="biasVal"></param>
	/// <returns></returns>
	public bool AddLayer(int index, int numNodes, bool replaceAxonWeights, bool randomWeights, float weightVal = 1.0f, float biasVal = 0.0f)
	{
		if (!Mathc.ValueIsBetween(index, 0, layerCount, true))
			return false;

		// If the added layer and previous layer do not have the same number of nodes, copying connections can't be done.
		//Debug.Log($"{numNodes} != {nodeLayers[index-1].Count}");
		if (numNodes != nodeLayers[index-1].Count)
			replaceAxonWeights = true;

		NeuronListWrapper newLayer = new List<Neuron>(numNodes);

		// Create and insert newLayer
		for (int i = 0; i < numNodes; ++i)
			newLayer.Add(new Neuron(nodeCount++, randomWeights ? Random.value * 2 - 1 : biasVal));
		nodeLayers.Insert(index, newLayer);

		var prevLayer = nodeLayers[index - 1];
		var nextLayer = nodeLayers[index + 1];

		// If possible, copy prevLayer->nextLayer connections to corresponding newLayer->nextLayer connections
		if(!replaceAxonWeights)
		{
			//Debug.Log("Copying connections");
			for(int i = 0; i < prevLayer.Count; ++i)
			{
				var n0 = prevLayer[i];
				var n1 = newLayer[i];
				foreach (var n2 in nextLayer)
					n0.ReplaceConnection(n2, n1, false);
			}
		}
		// If not copying, remove prevLayer's connections, and then connect newLayer to nextLayer.
		else
		{
			//Debug.Log("Replacing connections");
			foreach (var n0 in prevLayer)
				n0.RemoveAllConnections();
			foreach(var n0 in newLayer)
				foreach(var n1 in nextLayer)
					n0.ConnectNode(n1, randomWeights ? Random.value * 2 - 1 : weightVal);
		}


		// Connect prevLayer to newLayer
		foreach (var n0 in prevLayer)
			foreach (var n1 in newLayer)
				n0.ConnectNode(n1, randomWeights ? Random.value * 2 - 1 : weightVal);

		return true;
	}
	public bool RemoveLayer(int index, bool replaceConnections)
	{
		if (!Mathc.ValueIsBetween(index, 0, layerCount-1, true))
			return false;

		var currLayer = nodeLayers[index];
		var prevLayer = nodeLayers[index - 1];
		var nextLayer = nodeLayers[index + 1];

		if (prevLayer.Count != currLayer.Count)
			replaceConnections = true;

		if(!replaceConnections)
		{
			for(int i = 0; i < currLayer.Count; ++i)
			{
				var n0 = currLayer[i];
				var n1 = prevLayer[i];
				n1.RemoveAllConnections();
				foreach (var n2 in nextLayer)
					n0.ReplaceConnection(n2, n1, false);
			}
		}
		else
		{
			foreach(var n0 in prevLayer)
			{
				n0.RemoveAllConnections();
				foreach (var n1 in nextLayer)
					n0.ConnectNode(n1, Random.value * 2 - 1);
			}
		}

		nodeLayers.RemoveAt(index);
	
		return true;
	}
}

public partial class NeuralNetwork
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
			if (owner.layerCount <= 0)
				return;

			float xPos = 0.0f;
			float layerHeight = 0.0f;
			float startX = 0.0f;
			layerStartList.Clear();
			layerStartList.Capacity = owner.layerCount;

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
			startX = nodeSize * owner.layerCount * 2;
			startX += layerSpacing * (owner.layerCount - 1);
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
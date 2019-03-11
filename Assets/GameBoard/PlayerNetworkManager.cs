using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetworkManager : MonoBehaviour
{
	public uint completionTime = 0;
	public NeuralNetwork nnExpansion;
	public NeuralNetwork nnDevelopment;
	public NeuralNetwork nnThreatLevel;

	[HideInInspector] public float[] developmentInputs;
	[HideInInspector] public float[] expansionInputs;
	[HideInInspector] public float[] threatInputs;
	[HideInInspector] public Dictionary<int, int> playerAttacksDict = new Dictionary<int, int>();

	bool expansionDesiresUpdated = false;
	Dictionary<int, float> nodeExpansionDesires = new Dictionary<int, float>();
	
	Player player;
	public Player threatPlayer { get; private set; }

	ResourceData rdFood, rdWood, rdGold;

	public void Init(Player p)
	{
		player = p;
		developmentInputs = new float[nnDevelopment.inputCount];
		expansionInputs = new float[nnExpansion.inputCount];
		threatInputs = new float[nnThreatLevel.inputCount];


		nnDevelopment.RefreshConnections();
		nnExpansion.RefreshConnections();
		nnThreatLevel.RefreshConnections();

		rdFood = GameManager.instance.GetResourceFromName("Food");
		rdWood = GameManager.instance.GetResourceFromName("Wood");
		rdGold = GameManager.instance.GetResourceFromName("Gold");

		foreach (var pl in GameManager.instance.players)
			playerAttacksDict[pl.id] = 0;
	}
	public void Reset()
	{
		completionTime = 0;
		nodeExpansionDesires.Clear();
		foreach (var pl in GameManager.instance.players)
			playerAttacksDict[pl.id] = 0;
		threatPlayer = null;
	}

	public float[] EvaluateDevelopment(NodeController node)
	{
		for (int i = 0; i < developmentInputs.Length; ++i)
		{
			developmentInputs[i] = 0.0f;
			if (i % 2 == 0)
				developmentInputs[i] = player.resourceRatesList[i / 2].value;
			else developmentInputs[i] = player.resourcePoolsList[i / 2].value;
		}

		return nnDevelopment.Evaluate(developmentInputs);

		//var outputs = nnDevelopment.Evaluate(developmentInputs);

		//int highest = 0;
		//for (int i = 0; i < outputs.Length; ++i)
		//	if (outputs[i] > outputs[highest])
		//		highest = i;

		//var building = GameManager.instance.buildingList[highest];
		//return GameManager.instance.AttemptToBuyBuilding(player, node, building);
	}

	/// <summary>
	/// Takes into acount our current resource pools/rates, the nodes resource rates, the distance to nearest player, and the distance to the most threatening player.
	/// </summary>
	/// <param name="desiredNode"></param>
	/// <returns></returns>
	public float EvaluateExpansion(out NodeController desiredNode)
	{
		int iIndex = 0;
		float bestval = float.MinValue;

		foreach(var res in GameManager.instance.resourceList)
		{
			expansionInputs[iIndex] = player.GetResourcePool(res);
			expansionInputs[++iIndex] = player.GetResourceRate(res);
		}

		desiredNode = null;
		foreach (var kvp in player.borderNodes)
		{
			int i = iIndex + 1;
			for(int j = 0; j < GameManager.instance.resourceList.Count; ++j, ++i)
			{
				var res = GameManager.instance.resourceList[j];
				expansionInputs[i] = kvp.Value.resourceCont.GetResourceRate(res);
			}

			expansionInputs[i] = kvp.Value.nearestOwnedDist;
			expansionInputs[++i] = GameManager.instance.DistanceToNearestPlayerNode(kvp.Value, threatPlayer);
			expansionInputs[++i] = kvp.Value.buildingCont.GetBuildingTypeCount(BUILDING_TYPE.ECONOMIC);

			float val = nnExpansion.Evaluate(expansionInputs)[0];
			if (val > bestval)
			{
				bestval = val;
				desiredNode = kvp.Value;
			}
		}
		return bestval;
	}

	public void EvaluateThreatLevel()
	{
		threatPlayer = null;
		float maxThreat = float.MinValue;
		foreach(var p in GameManager.instance.players)
		{
			if (p.numOwnedNodes <= 0)
				continue;
			threatInputs[0] = p.numOwnedNodes;
			threatInputs[1] = player.numOwnedNodes / p.numOwnedNodes;
			threatInputs[2] = p.expansionRate;
			threatInputs[3] = playerAttacksDict[p.id];
			threatInputs[4] = DistFromPlayer(p); // How close they are
			float threatLevel = nnThreatLevel.Evaluate(threatInputs)[0];
			if(threatLevel > maxThreat)
			{
				maxThreat = threatLevel;
				threatPlayer = p;
			}
		}
	}

	//public float EvaluateResourceDesire(ResourceData rData, NodeController node, NeuralNetwork nn)
	//{
	//	resourceDesireInputs[0] = player.GetResourceRate(rData);
	//	resourceDesireInputs[1] = player.GetResourcePool(rData);
	//	resourceDesireInputs[2] = node.resourceCont.GetResourceRate(rData);
	//	return nn.Evaluate(resourceDesireInputs)[0];
	//}

	public void CopyNetworkConnectionsFrom(PlayerNetworkManager pnm)
	{
		nnExpansion.CopyConnectionsFrom(pnm.nnExpansion);
		nnDevelopment.CopyConnectionsFrom(pnm.nnDevelopment);
		nnThreatLevel.CopyConnectionsFrom(pnm.nnThreatLevel);
	}
	public void CopyNetworksFrom(PlayerNetworkManager pnm)
	{
		nnExpansion.CopyNetworkFrom(pnm.nnExpansion);
		nnDevelopment.CopyNetworkFrom(pnm.nnDevelopment);
		nnThreatLevel.CopyNetworkFrom(pnm.nnThreatLevel);
	}
	public string MutateNetworksAsexual(float mutationChance, float newNodeChance, float newLayerChance, bool randomWeights)
	{
		string retval = "";

		retval += MutateNetworkInternal(nnExpansion, "E", mutationChance, newNodeChance, newLayerChance, randomWeights);
		retval += MutateNetworkInternal(nnDevelopment, "D", mutationChance, newNodeChance, newLayerChance, randomWeights);
		retval += MutateNetworkInternal(nnThreatLevel, "T", mutationChance, newNodeChance, newLayerChance, randomWeights);
		return retval;
	}
	string MutateNetworkInternal(NeuralNetwork nn, string prefix, float mutationChance, float newNodeChance, float newLayerChance, bool randomWeights)
	{
		string rv = nn.MutateAsexual(mutationChance, newNodeChance, newLayerChance, randomWeights);
		if (rv.Length > 0)
			rv = $"[{prefix}:{rv}]";
		return rv;
	}

	public static PlayerNetworkManager Copy(string name, PlayerNetworkManager orig)
	{
		PlayerNetworkManager pnm = new GameObject(name).AddComponent<PlayerNetworkManager>();

		pnm.nnDevelopment = new GameObject("Development").AddComponent<NeuralNetwork>();
		pnm.nnDevelopment.transform.parent = pnm.transform;

		pnm.nnExpansion = new GameObject("Expansion").AddComponent<NeuralNetwork>();
		pnm.nnExpansion.transform.parent = pnm.transform;

		pnm.nnThreatLevel = new GameObject("ThreatLevel").AddComponent<NeuralNetwork>();
		pnm.nnThreatLevel.transform.parent = pnm.transform;

		pnm.CopyNetworksFrom(orig);
		return pnm;
	}

	int DistFromPlayer(Player p)
	{
		int dist = int.MaxValue;
		foreach(var kvp in player.borderNodes)
		{
			int test = GameManager.instance.DistanceToNearestPlayerNode(kvp.Value, p);
			if (test < dist)
			{
				dist = test;
				if (dist == 0)
					return 0;
			}
		}
		return dist + 1;
	}
}

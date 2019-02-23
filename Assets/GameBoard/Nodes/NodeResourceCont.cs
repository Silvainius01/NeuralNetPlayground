using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class NodeController
{
	public class NodeResourceCont
	{
		bool isAdded = false;
		NodeController parentNode;
		Dictionary<int, ResourceValue> resourceRates = new Dictionary<int, ResourceValue>();

		public NodeResourceCont(NodeController pNode)
		{
			parentNode = pNode;
		}

		public void AddResourceRate(ResourceData rData, float rate)
		{
			// Avoids copying value-type
			if (resourceRates.ContainsKey(rData.key))
				resourceRates[rData.key].SetValue(rate + resourceRates[rData.key].value);
			else resourceRates.Add(rData.key, new ResourceValue(rData, rate));

			UpdateOwnerResourceRate(rData, rate);
		}
		public void SetResourceRate(ResourceData rData, float rate)
		{
			// Avoids copying value-type
			float oldValue = 0.0f;

			if (resourceRates.ContainsKey(rData.key))
			{
				oldValue = resourceRates[rData.key].value;
				resourceRates[rData.key].SetValue(rate);
			}
			else resourceRates.Add(rData.key, new ResourceValue(rData, rate));

			UpdateOwnerResourceRate(rData, rate - oldValue);
		}
		public void SetAllResourceRates(float rate)
		{
			foreach (var rData in GameManager.instance.resourceList)
			{
				SetResourceRate(rData, rate);
			}
		}

		public float GetResourceRate(ResourceData rData)
		{
			if (resourceRates.ContainsKey(rData.key))
				return resourceRates[rData.key].value;
			return 0.0f;
		}

		public void AddToOwnerResourceRate()
		{
			if (parentNode.owner == null || isAdded)
				return;
			isAdded = true;
			foreach (var kvp in resourceRates)
				parentNode.owner.AddResourceRate(kvp.Value.resource, kvp.Value.value);
		}
		public void RemoveFromOwnerResourceRate()
		{
			if (parentNode.owner == null || !isAdded)
				return;
			isAdded = false;
			foreach (var kvp in resourceRates)
				parentNode.owner.AddResourceRate(kvp.Value.resource, -kvp.Value.value);
		}
		void UpdateOwnerResourceRate(ResourceData rData, float rateDiff)
		{
			if(parentNode.IsOwned())
			{
				parentNode.owner.AddResourceRate(rData, rateDiff);
			}
		}

		public float GetEconomicValue(Player p)
		{
			//	Each resource has two values for players. How much you have, and how much you make.
			//	A node, however, only has one value, and that is how much it can produce.
			//	Thus, we must come up with a solution that can evaluate how extra produvction affects a players wealth.
			//	AFAIK, There are six possible resource states:
			//		1) High pool, high rate			<- Great	(0)
			//		2) Low pool, high rate			<- Good		(1)
			//		3) High pool, low rate			<- Okay		(2)
			//		4) low pool, low rate			<- Bad		(4)
			//		5) high pool, negative rate		<- Okay		(3)
			//		6) low pool, negative rate		<- Oh fuck	(5)
			// By giving these states a desire rating, we can determine how badly we need/want the resource in question.
			// From there, we can then determine how aquiring the resource will affect our economy. 

			return 0.0f;
		}
	}
}

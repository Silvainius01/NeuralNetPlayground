using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class NodeController
{
	public class NodeResourceCont
	{
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
		}
		public void SetResourceRate(ResourceData rData, float rate)
		{
			// Avoids copying value-type
			if (resourceRates.ContainsKey(rData.key))
				resourceRates[rData.key].SetValue(rate);
			else resourceRates.Add(rData.key, new ResourceValue(rData, rate));
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
	}
}

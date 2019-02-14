using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public enum BUILDING_TYPE { ECONOMIC, HOUSING, SPECIALTY }
public struct BuildingTypeComparer : IEqualityComparer<BUILDING_TYPE>
{
	public bool Equals(BUILDING_TYPE x, BUILDING_TYPE y)
	{
		return x == y;
	}

	public int GetHashCode(BUILDING_TYPE obj)
	{
		return (int)obj;
	}
}

[System.Serializable]
public class BuildingValue<T>
{
	public T value;
	public BuildingData building;

	public BuildingValue(BuildingData data, T value)
	{
		this.value = value;
		building = data;
	}

	public void SetValue(T value)
	{
		this.value = value;
	}
}

[System.Serializable]
public class BuildingData
{
	[SerializeField] string m_name;
	[SerializeField] BUILDING_TYPE m_buildingType;
	[SerializeField] List<ResourceValue> buildingCostList;
	[SerializeField] List<ResourceValue> buildingIncomeList;

	Dictionary<int, int> buildingCostDict;
	Dictionary<int, int> buildingIncomeDict;

	public int key { get; private set; }
	public string name { get { return m_name; } }
	public BUILDING_TYPE buildingType { get { return m_buildingType; } }
	public ReadOnlyCollection<ResourceValue> buildingCostROC;
	public ReadOnlyCollection<ResourceValue> buildingIncomeROC;

	public void Init()
	{
		key = name.GetHashCode();
		buildingCostDict = new Dictionary<int, int>(buildingCostList.Count);
		buildingCostROC = new ReadOnlyCollection<ResourceValue>(buildingCostList);
		buildingIncomeDict = new Dictionary<int, int>(buildingIncomeList.Count);
		buildingIncomeROC = new ReadOnlyCollection<ResourceValue>(buildingIncomeList);
		//buildingCostList = buildingIncomeList = null;

		for (int i = 0; i < buildingCostROC.Count; ++i)
		{
			buildingCostROC[i].resource.Init();
			buildingCostDict.Add(buildingCostROC[i].resource.key, i);
		}
		for (int i = 0; i < buildingIncomeROC.Count; ++i)
		{
			buildingIncomeROC[i].resource.Init();
			buildingIncomeDict.Add(buildingIncomeROC[i].resource.key, i);
		}
	}

	public bool ContainsResourceInCost(ResourceData rData)
	{
		return buildingCostDict.ContainsKey(rData.key);
	}
	public bool ContainsResourceInIncome(ResourceData rData)
	{
		return buildingIncomeDict.ContainsKey(rData.key);
	}

	public bool ContainsResourceInCost(ResourceData rData, out float value)
	{
		int index = 0;
		if (buildingCostDict.TryGetValue(rData.key, out index))
		{
			value = buildingCostList[index].value;
			return true;
		}
		value = 0.0f;
		return false;
	}
	public bool ContainsResourceInIncome(ResourceData rData, out float value)
	{
		int index = 0;
		if (buildingIncomeDict.TryGetValue(rData.key, out index))
		{
			value = buildingIncomeList[index].value;
			return true;
		}
		value = 0.0f;
		return false;
	}
}

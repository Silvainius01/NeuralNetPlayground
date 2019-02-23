using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingValueInt : BuildingValue<int>
{
	public BuildingValueInt(BuildingData data, int value) : base(data, value) { }
}

[System.Serializable]
public class NodeBuildingCont
{
	int maxBuildings = 1;
	NodeController parentNode;

	[SerializeField] List<BuildingValueInt> buildingsOnNodeList = new List<BuildingValueInt>();
	Dictionary<int, BuildingValue<int>> buildingsOnNode = new Dictionary<int, BuildingValue<int>>();
	Dictionary<BUILDING_TYPE, int> buildingTypeCount = new Dictionary<BUILDING_TYPE, int>(new BuildingTypeComparer());
	
	public NodeBuildingCont(NodeController pNode)
	{
		parentNode = pNode;

		var types = Mathc.GetEnumValues<BUILDING_TYPE>();
		foreach (var t in types)
			buildingTypeCount.Add(t, 0);
	}

	/// <summary> Adds passed building to node X times. Respects build limits. Returns true if building is added. </summary>
	public bool AddBuilding(string name, int count)
	{
		return AddBuilding(GameManager.instance.GetBuildingFromName(name), count);
	}
	/// <summary> Adds passed building to node X times. Respects build limits. Returns true if building is added. </summary>
	public bool AddBuilding(BuildingData bData, int count)
	{
		if (!CanSupportBuilding(bData))
			return false;

		if (!buildingsOnNode.ContainsKey(bData.key))
			AddBuildingInternal(bData);
		AttemptToSetBuildingCount(bData, buildingsOnNode[bData.key].value + count);
		return true;
	}
	/// <summary> Sets the count of a building to a given number. Ignores build limits. </summary>
	public void SetBuildingCount(string buildingName, int count)
	{
		SetBuildingCount(GameManager.instance.GetBuildingFromName(buildingName), count);
	}
	/// <summary> Sets the count of a building to a given number. Ignores build limits. </summary>
	public void SetBuildingCount(BuildingData bData, int count)
	{
		if (!buildingsOnNode.ContainsKey(bData.key))
			AddBuildingInternal(bData);
		AttemptToSetBuildingCount(bData, count);
	}

	public void RemoveAllBuildings()
	{
		foreach (var bvi in buildingsOnNodeList)
			SetBuildingCount(bvi.building, 0);
	}

	public int GetBuildingCount(BuildingData building)
	{
		if (buildingsOnNode.ContainsKey(building.key))
			return buildingsOnNode[building.key].value;
		return 0;
	}
	public int GetBuildingTypeCount(BUILDING_TYPE type)
	{
		return buildingTypeCount[type];
	}

	public bool CanSupportBuilding(BuildingData building)
	{
		return CanSupportBuildingType(building.buildingType);
	}
	public bool CanSupportBuildingType(BUILDING_TYPE type)
	{
		return buildingTypeCount[type] < GameManager.instance.maxBuildingTypesDict[type];
	}

	void AddBuildingInternal(BuildingData bData)
	{
		BuildingValueInt bv = new BuildingValueInt(bData, 0);
		buildingsOnNodeList.Add(bv);
		buildingsOnNode.Add(bData.key, bv);
	}
	void AttemptToSetBuildingCount(BuildingData bData, int count)
	{
		if (count < 0)
			count = 0;
		int diff = count - buildingsOnNode[bData.key].value;
		foreach(var rv in buildingsOnNode[bData.key].building.buildingIncomeROC)
			parentNode.resourceCont.AddResourceRate(rv.resource, rv.value * diff);
		buildingTypeCount[bData.buildingType] += diff;
		buildingsOnNode[bData.key].value = count;
	}

}

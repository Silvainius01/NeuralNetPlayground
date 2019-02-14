using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResourceValue
{
	bool inited;
	public float value;
	public ResourceData resource;

	public void SetValue(float v)
	{
		value = v;
	}

	public ResourceValue(ResourceData rData, float value)
	{
		this.value = value;
		resource = rData;
		inited = true;
	}

	public void Init()
	{
		if (!inited && resource != null)
			resource = GameManager.instance.GetResourceFromName(resource.name);
		inited = true;
	}
}

[System.Serializable]
public class ResourceData
{
	[SerializeField] string m_name;
	public string name { get { return m_name; } }
	public int key { get; private set; }

	public void Init()
	{
		key = name.GetHashCode();
	}
}

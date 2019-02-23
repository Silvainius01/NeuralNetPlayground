using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetTrainingManager : MonoBehaviour
{
	[System.Serializable]
	class InputData
	{
		public enum INPUT_MODE { BOOL_OFF, BOOL_ON, SCALE_TOP, SCALE_BOT }
		public enum INPUT_PRIORITY { LOW = 1, MODERATE, HIGH }

		public string name;
		public Vector2 valueRange;
		public INPUT_MODE mode;
		public INPUT_PRIORITY priority;


		/// <summary> Returns a -1 to 1 value based off defined input parameters </summary>
		/// <returns></returns>
		public float GetRandomValue()
		{
			if (mode <= INPUT_MODE.BOOL_ON)
			{
				bool V = Random.value < 0.5f;

				if (V)
				{
					if (mode == INPUT_MODE.BOOL_ON)
						return 1;
					return 0;
				}
				else if (mode == INPUT_MODE.BOOL_OFF)
					return 1;
				return 0;
			}

			float v = Random.Range(valueRange.x, valueRange.y);
			v = Mathc.NormalizeBetween(v, valueRange.x, valueRange.y) * 2 - 1;
			return v;
		}

		/// <summary> Returns how many points the passed value is worth. Assumes that the value has already been processed, and is ready to be put into a network. </summary>
		/// <param name="val">Should be between -1 and 1.</param>
		public float GetDataScore(float val)
		{
			return val * (float)(mode == INPUT_MODE.SCALE_BOT ? -1 : 1) * (float)priority;
		}
		public float GetDataScore(bool val)
		{
			return (val ? 0 : 1) * (int)priority;
		}
	}

#if UNITY_EDITOR
	[SerializeField] List<InputData> inputData = new List<InputData>();
	[SerializeField] List<NeuralNetwork> networks = new List<NeuralNetwork>();

	[Header("Training Options")]
	[SerializeField] bool evalMaxScore;
	[SerializeField] bool evalNetworks;
	[SerializeField] bool mutateNetworks;
	[SerializeField] bool generateDataSet;
	[SerializeField] bool useRandomData;
	[SerializeField] int numRoundsPerFrame;
	[SerializeField] float mutationChance;

	[Header("Results")]
	[SerializeField] int numRounds = 0;
	[SerializeField] float maxScore;
	[SerializeField] float lastDataScore;
	[SerializeField] float[] dataSet;
	[SerializeField] List<float> results = new List<float>();
	[SerializeField] List<float> winRates = new List<float>();

	void Start()
	{
		numRounds = 0;
		winRates.Capacity = networks.Count;
		foreach (var network in networks)
		{
			winRates.Add(0.0f);
			//network.BuildDefaultNetwork();
		}
	}

	void Update()
	{
		TrainNetworks();
	}

	float[] GenerateInputArray()
	{
		float[] retval = new float[inputData.Count];

		for (int i = 0; i < retval.Length; ++i)
			retval[i] = inputData[i].GetRandomValue();

		return retval;
	}

	float GetInputDataScore(ref float[] inputs)
	{
		float score = 0.0f;

		for (int i = 0; i < inputs.Length; ++i)
		{
			score += inputData[i].GetDataScore(inputs[i]);
		}

		return score;
	}

	float GetMaxDataScore()
	{
		float mScore = 0.0f;
		foreach (var input in inputData)
		{
			mScore += (int)input.priority;
		}
		return mScore;
	}

	void EvaluateNetworks()
	{
		float[] outputs = new float[networks.Count];
		if (useRandomData || generateDataSet)
		{
			dataSet = GenerateInputArray();
			generateDataSet = false;
		}
		float dataScore = GetInputDataScore(ref dataSet);

		results.Clear();
		results.Capacity = outputs.Length;
		for (int i = 0; i < networks.Count; ++i)
		{
			outputs[i] = networks[i].Evaluate(dataSet)[0];
			results.Add(outputs[i]*maxScore);
		}
		
		lastDataScore = 0;
		for (int i = 0; i < dataSet.Length; ++i)
		{
			lastDataScore += inputData[i].GetDataScore(dataSet[i]);
		}
	}

	void MutateNetworks(int exclude = -1)
	{
		for(int i = 0; i < networks.Count; ++i)
		{
			if (i == exclude)
				continue;
			networks[i].MutateAsexual(0.25f);
		}
	}

	void TrainNetworks()
	{
		int bestNetwork = 0;
		float bestScore = float.MaxValue;

		winRates.Clear();
		foreach (var n in networks)
			winRates.Add(0.0f);

		for(int i = 0; i < numRoundsPerFrame; ++i)
		{
			EvaluateNetworks();

			bestNetwork = 0;
			bestScore = float.MaxValue;
			for (int j = 0; j < results.Count; ++j)
			{
				float comp = Mathf.Abs(results[j] - lastDataScore);
				if (comp < bestScore)
				{
					bestScore = comp;
					bestNetwork = j;
				}
			}
			
			winRates[bestNetwork] += 1.0f;
		}
		for (int i = 0; i < winRates.Count; ++i)
			winRates[i] /= numRoundsPerFrame;

		bestNetwork = 0;
		bestScore = 0.0f;
		for(int i = 0; i < winRates.Count; ++i)
		{
			if(winRates[i] > bestScore)
			{
				bestNetwork = i;
				bestScore = winRates[i];
			}
		}

		for(int i = 0; i < networks.Count; ++i)
		{
			if (i != bestNetwork)
			{
				networks[i].CopyConnectionsFrom(networks[bestNetwork]);
				networks[i].MutateAsexual(mutationChance);
			}
		}

		numRounds++;
	}

	void OnDrawGizmos()
	{
		if (evalMaxScore)
		{
			maxScore = GetMaxDataScore();
			evalMaxScore = false;
		}
		if (evalNetworks)
		{
			EvaluateNetworks();
			evalNetworks = false;
		}
		if (mutateNetworks)
		{
			MutateNetworks();
			foreach (var network in networks)
			{
				//network.CacheGizmoDrawData();
			}
			mutateNetworks = false;
		}
	}
#endif
}
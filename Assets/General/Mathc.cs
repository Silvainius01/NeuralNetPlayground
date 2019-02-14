using UnityEngine;
using System;
using System.Collections.Generic;

public static class Mathc
{
	public class Line
	{
		public float m { get; private set; }
		public float b { get; private set; }
		private Vector2 p1, p2;

		public Vector2 start { get { return p1; } }
		public Vector2 end { get { return p2; } }

		public Line(Line l)
		{
			p1 = l.p1;
			p2 = l.p2;
			CalcLine();
		}
		public Line(Vector2 start, Vector2 end)
		{
			p1 = start;
			p2 = end;
			CalcLine();
		}
		public Line(float x1, float y1, float x2, float y2)
		{
			p1 = new Vector2(x1, y1);
			p2 = new Vector2(x2, y2);
			CalcLine();
		}

		public void SetStartPoint(Vector2 point)
		{
			if (point.x > p2.x)
			{
				p1 = p2;
				p2 = point;
			}
			else p1 = point;
			CalcLine();
		}
		public void SetEndPoint(Vector2 point)
		{
			if (point.x < p1.x)
			{
				p2 = p1;
				p1 = point;
			}
			else p2 = point;
			CalcLine();
		}

		void CalcLine()
		{
			m = GetMValue(p1, p2);
			b = GetBValue(p1, p2);
		}

		/// <summary>
		/// Return a point on a line using (y = mx + b). Does not use points as bounds.
		/// </summary>
		/// <param name="p1">Point one of the line</param>
		/// <param name="p2">Point two of the line</param>
		/// <param name="x">X-coordinate of the point you want</param>
		public static Vector2 GetPointOnLine(Vector2 p1, Vector2 p2, float x)
		{
			float m = GetMValue(p1, p2);

			if (p1.x > p2.x)
			{
				m = -m;
				return GetPointOnLine(m, x, p2.y - (m * p2.x));
			}
			return GetPointOnLine(m, x, p1.y - (m * p1.x));
		}

		/// <summary>
		/// Return a point on a line using (y = mx + b).
		/// </summary>
		/// <param name="p1">Point one of the line</param>
		/// <param name="p2">Point two of the line</param>
		/// <param name="x">X-coordinate of the point you want</param>
		public static Vector2 GetPointOnLine(Vector2 p1, Vector2 p2, float x, out bool betweenPoints)
		{
			float m = GetMValue(p1, p2);
			Vector2 retval = Vector2.zero;

			if (p1.x > p2.x)
			{
				m = -m;
				retval = GetPointOnLine(m, x, p2.y - (m * p2.x));
			}
			else retval = GetPointOnLine(m, x, p1.y - (m * p1.x));

			betweenPoints = VectorIsBetween(retval, p1, p2, false);
			return retval;
		}

		/// <summary>
		/// Return a point on a line using (x = (y-b) / m). Does not use points as bounds.
		/// </summary>
		/// <param name="y">Y-coord of the point you want.</param>
		/// <param name="p1">First point on the line.</param>
		/// <param name="p2">Second point on the line.</param>
		/// <returns></returns>
		public static Vector2 GetPointOnLine(float y, Vector2 p1, Vector2 p2)
		{
			float m = GetMValue(p1, p2);
			float b = 0;

			if (p2.x > p1.x)
			{
				m = -m;
				b = p2.y - (m * p2.x);
				return GetPointOnLine(m, (p2.y - b) / m, b);
			}

			b = p1.y - (m * p1.x);
			return GetPointOnLine(m, (p1.y - b) / m, b);
		}

		/// <summary>
		/// Return a point on a line using (x = (y-b) / m).
		/// </summary>
		/// <param name="y">Y-coord of the point you want.</param>
		/// <param name="p1">First point on the line.</param>
		/// <param name="p2">Second point on the line.</param>
		/// <returns></returns>
		public static Vector2 GetPointOnLine(float y, Vector2 p1, Vector2 p2, out bool betweenPoints)
		{
			float m = GetMValue(p1, p2);
			float b = 0;
			Vector2 retval = Vector2.zero;

			if (p2.x > p1.x)
			{
				m = -m;
				b = p2.y - (m * p2.x);
				retval = GetPointOnLine(m, (p2.y - b) / m, b);
			}
			else
			{
				b = p1.y - (m * p1.x);
				retval = GetPointOnLine(m, (p1.y - b) / m, b);
			}

			betweenPoints = VectorIsBetween(retval, p1, p2, false);
			return retval;
		}

		/// <summary>
		/// Return a point on a line using (y = mx + b)
		/// </summary>
		/// <param name="m">Slope of the line</param>
		/// <param name="x">X-coordinate of the point you want</param>
		/// <param name="b">Height of the line at x == 0</param>
		public static Vector2 GetPointOnLine(float m, float x, float b)
		{
			return new Vector2(x, (m * x) + b);
		}

		/// <summary>
		///  Returns the intersect point between two lines. Returns (0,0) if lines do not intersect.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <param name="p4"></param>
		/// <param name="intersected"></param>
		/// <returns></returns>
		public static Vector2 FindIntersectionPoint(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out bool intersected)
		{
			intersected = false;


			// Organize points based on the greatest X value
			if (p2.x < p1.x)
			{
				Vector2 t = p1;
				p1 = p2;
				p2 = t;
			}
			if (p4.x < p3.x)
			{
				Vector2 t = p3;
				p3 = p4;
				p4 = t;
			}

			// If the line is completely to the right of the other, they don't intersect.
			if (p2.x < p3.x && p2.x < p4.x)
				return Vector2.zero;
			// If the lione is completely to the left of the other, they don't intersect
			else if (p3.x < p1.x && p4.x < p1.x)
				return Vector2.zero;

			// Organize points based on the greatest Y value
			if (p2.y < p1.y)
			{
				Vector2 t = p1;
				p1 = p2;
				p2 = t;
			}
			if (p4.y < p3.y)
			{
				Vector2 t = p3;
				p3 = p4;
				p4 = t;
			}

			// If both points are above the highest point, they can't intersect
			if (p2.y < p3.y && p2.y < p4.y)
				return Vector2.zero;
			// If both points are below the lowest point, they can't intersect.
			else if (p3.y < p1.y && p4.y < p1.y)
				return Vector2.zero;

			// Y = MX + B
			float line1M = GetMValue(p1, p2); // Slope of L1
			float line2M = GetMValue(p3, p4); // Slope of L2
			float line1B = p1.y - (line1M * p1.x); // B value of L1
			float line2B = p3.y - (line2M * p3.x); // B value of L2
			Vector2 intersectPoint;

			if (float.IsInfinity(line1M))
				intersectPoint = GetPointOnLine(line2M, p1.x, line2B);
			else if (float.IsInfinity(line2M))
				intersectPoint = GetPointOnLine(line1M, p3.x, line1B);
			else
				intersectPoint = GetPointOnLine(line1M, (line2B - line1B) / (line1M - line2M), line1B);
			
			intersected = VectorIsBetween(intersectPoint, p1, p2, false) && VectorIsBetween(intersectPoint, p3, p4, false);
			return intersectPoint;
		}

		/// <summary>
		/// Returns the m value of a line (y = mx + b)
		/// </summary>
		public static float GetMValue(Vector2 p1, Vector2 p2)
		{
			return (p2.y - p1.y) / (p2.x - p1.x);
		}

		public static float GetBValue(Vector2 p1, Vector2 p2)
		{
			float m = GetMValue(p1, p2);

			if(p1.x > p2.x)
			{
				m = -m;
				return ((m * p2.x) - p2.y) * -1;
			}
			return ((m * p1.x) - p1.y) * -1;
		}

		// y = mx + b
		// y - b = mx;
		// -b = mx - y
		// b = -1(mx - y);
	}
	public class Random
	{
		/// I used Colin Green's implementation for code reference:
		/// https://github.com/colgreen/Redzen/blob/master/Redzen/Random/Double/ZigguratGaussianDistribution.cs
		/// For info on how the Ziggurat works:
		/// https://en.wikipedia.org/wiki/Ziggurat_algorithm#Generating_the_tables
		/// https://blogs.mathworks.com/cleve/2015/05/18/the-ziggurat-random-normal-generator/
		/// For those interested in the mathematical origins of r and v:
		/// http://cis.poly.edu/~mleung/CS909/s04/Ziggurat.pdf
		/// https://courses.cs.washington.edu/courses/cse591n/07wi/papers/fpl05_dul98.pdf
		/// CLOSED BECAUSE I THINK I FUCKED IT UP
		private class Ziggurat
		{
			private static Ziggurat _instance;
			public static Ziggurat instance
			{
				get
				{
					if (_instance == null)
						_instance = new Ziggurat();
					return _instance;
				}
			}

			List<double> rVals = new List<double>();

			const int numRects = 128;
			const int iHalfMax = int.MaxValue / 2;
			// xCoord of the right edge of the base rect. Calculated for 128 rects.
			private const double r = 3.4426198558966519;
			// Area of each rectangle. Calculated for 128 rects.
			private const double v = 9.91256303526217e-3;
			// Scale factor to convert from "infinite" int to a [0,1] double
			private const double scaleFactor = 1.0 / (int.MaxValue - 1);
			// Normalization factor for GaussianCurve
			private static readonly double c = Mathf.Pow(1 / TWO_PI, 0.5f);

			// 1+((1/(2p) ^ 0.5) * x) = 1


			// Area A divided by the height of B0
			readonly double V_DIV_Y0;

			// coordnates of the bottom rights of rectangles.
			readonly double[] xCoord;
			readonly double[] yCoord;
			// The percent of each segment that's within the distribution. 
			// A value of 0 indicates 0% and int.MaxValue 100%. 
			// An integer allows some floating operations to be replaced by interger ones.
			readonly int[] xComp;

			public Ziggurat()
			{
				// Allocate coordnate arrays.
				// +1 here because it prevents a test case for the top box.
				xCoord = new double[numRects + 1];
				yCoord = new double[numRects];
				xComp = new int[numRects];

				// Bottom rect pos
				xCoord[0] = r;
				yCoord[0] = GaussianCurve(r);

				// Second rect pos, it has allllmost the same coords.
				xCoord[1] = r;
				yCoord[1] = yCoord[0] + (v / r);

				for (int i = 2; i < numRects; i++)
				{
					xCoord[i] = InverseGaussianCurve(yCoord[i - 1]);
					yCoord[i] = yCoord[i - 1] + (v / xCoord[i]);
				}

				// The top box is zero!
				xCoord[numRects] = 0.0;

				// The area of R0 as a proportion of v.
				// R0's area is v + the rest of the unboxed curve (dsitribuion tail).
				// xComp[0] is the probability that a sample point is within the box part of the segment.
				xComp[0] = (int)(((r * yCoord[0]) / v) * (double)int.MaxValue);

				V_DIV_Y0 = v / yCoord[0];

				for (int i = 1; i < numRects - 1; i++)
					xComp[i] = (int)((xCoord[i + 1] / xCoord[i]) * (double)int.MaxValue);
				xComp[numRects - 1] = 0;

				// Make sure that the top edge of R127 is damn close to zero
				Debug.Assert(Math.Abs(1.0 - yCoord[numRects - 1]) < 1e-10);
			}

			public double NormalizedSample()
			{
				return c * BaseSample();
			}

			public double BaseSample()
			{
				double value = 0.0f;
				do
				{
					int rIndex = UnityEngine.Random.Range(0, numRects);
					value = SampleRect(rIndex);
				} while (double.IsNaN(value));

				return value;
			}

			public double BaseSample(double mean, double deviation)
			{
				double value = 0.0f;
				do
				{
					int rIndex = UnityEngine.Random.Range(0, numRects);
					value = SampleRect(rIndex);
				} while (double.IsNaN(value));

				return mean + (value * deviation);
			}

			double SampleRect(int rIndex)
			{
				int rVal = UnityEngine.Random.Range(0, int.MaxValue);
				int sign = rVal < iHalfMax ? -1 : 1;

				if (rIndex == 0)
				{
					if (rVal < xComp[0]) // within base rect
						return rVal * scaleFactor * V_DIV_Y0 * sign;
					return SampleTail(rVal) * sign; // In tail
				}
				if (rVal < xComp[rIndex]) // in rect[i]
					return rVal * scaleFactor * xCoord[rIndex] * sign;

				// Outside rect.
				double x = rVal * scaleFactor * xCoord[rIndex];
				double y = UnityEngine.Random.Range(0, int.MaxValue) * scaleFactor;
				if (yCoord[rIndex - 1] + ((yCoord[rIndex] - yCoord[rIndex - 1]) * y) < GaussianCurve(x))
					return x * sign;
				return double.NaN;
			}

			double SampleTail(int rVal1)
			{ 
				double x, y;

				do
				{
					x = -Math.Log(rng.NextDouble() + 0.0000001) / v;
					y = -Math.Log(rng.NextDouble() + 0.0000001);
				} while (y + y < x * x);
				return x + v;
			}

			double GaussianCurve(double x)
			{
				return Math.Exp(-(x * x) / 2);
			}
			double InverseGaussianCurve(double y)
			{
				return Math.Sqrt(-2.0 * Math.Log(y));
			}
		}

		/// <summary> Normalizing factor </summary>
		private static readonly float nf = 3f;
		public static System.Random rng = new System.Random();
		private static bool hasSpareRand = false;
		private static float spareMarsagliaRand;

		// Number probability values by tenths (probablity that any number will start with X)
		// 0.0 : (23.9%)
		// 0.1 : (22.4%)
		// 0.2 : (19.9%)
		// 0.3 : (12.8%)
		// 0.4 : (08.4%)
		// 0.5 : (06.7%)
		// 0.6 : (03.1%)  
		// 0.7 : (01.5%)
		// 0.8 : (00.8%)
		// 0.9 : (00.5%)
		// 1.0 : (00.1%)

		static readonly float[] probabilities = new float[11]{ .236f, .216f, .180f, .138f, .096f, .062f, .036f, .019f, .009f, .004f, .0027f };
			/// <summary> Returns a number that a Marsaglia result will be equal to or less than at a desired probability. </summary>
			/// <param name="desiredProb"> Input as 0.XXX </param>
			/// <returns></returns>
		public static float GetProbabilityThreshold(float desiredProb) //20
		{
			float thresh = 0.0f;

			int i = 0;
			for(; i < 11; i++)
			{
				float t = thresh + probabilities[i]; 
				if (t > desiredProb)
					break;
				else if (t == desiredProb)
					return thresh;
				else thresh = t;
			}
			thresh = desiredProb - thresh;
			thresh /= probabilities[i];
			thresh /= 10;
			thresh += (0.1f*i);
			return thresh;
		}

		/// <summary> Return a random, normally distributed number that is between 0 and 1 </summary>
		/// <param name="negOne2One"> Make the range -1 to 1 instead </param>
		/// <returns></returns>
		public static float Marsaglia(bool negOne2One)
		{
			if(hasSpareRand)
			{
				hasSpareRand = false;
				if (negOne2One)
					return spareMarsagliaRand;
				return Mathf.Abs(spareMarsagliaRand);
			}
			float x, y, s;
			do
			{
				x = UnityEngine.Random.value * 2 - 1;
				y = UnityEngine.Random.value * 2 - 1;
				s = x * x + y * y;
			} while (s >= 1 || s == 0.0f);

			hasSpareRand = true;
			s = Mathf.Sqrt((-2 * Mathf.Log(s)) / s);
			spareMarsagliaRand = NormalizeBetween(y * s, -nf, nf) * 2 - 1;

			if (negOne2One)
				return NormalizeBetween(x * s, -nf, nf) * 2 - 1;
			return Mathf.Abs(NormalizeBetween(x * s, -nf, nf) * 2 - 1);
		}
		//// <summary> Returns a normally distributed value -1 and 1. </summary>
		//public static float zigguratValue { get { return (float)zigguratValueD; } }
		//// <summary> Returns a normally distributed value between -1 and 1 </summary>
		//public static double zigguratValueD { get { return Ziggurat.instance.BaseSample(); } }

		public static TKey KeyFromDict<TKey, TValue>(IDictionary<TKey, TValue> dict)
		{
			var keyList = dict.Keys;
			var keyEnumerator = keyList.GetEnumerator();
			int rIndex = UnityEngine.Random.Range(0, keyList.Count+1);

			keyEnumerator.Reset();
			for (int i = 0; i < rIndex; ++i)
				keyEnumerator.MoveNext();

			return keyEnumerator.Current;
		}
	}

	/// <summary> I.E. 90 degrees </summary>
	public const float HALF_PI = Mathf.PI / 2;
	/// <summary> I.E. 45 degrees </summary>
    public const float QUARTER_PI = Mathf.PI / 4;
	/// <summary> I.E. 22.5 degrees </summary>
	public const float EIGTH_PI = Mathf.PI / 8;
	/// <summary> I.E. 360 degrees  </summary>
	public const float TWO_PI = Mathf.PI * 2;
	public const float E = 2.7182818284f;
	public const float ONE_THIRD = 1 / 3;

	/// <summary> Returns a random KeyValuePair from a dicitonary. NOTE: creates a new list every time. not reccommened for large or unchanging dictionaries. </summary>
	public static KeyValuePair<TKey, TVal> GetRandomKVPFromDict<TKey, TVal>(ref Dictionary<TKey, TVal> dict)
	{
		var k = GetRandomKeyFromDict(ref dict);
		var v = dict[k];
		return new KeyValuePair<TKey, TVal>(k, v);
	}
	/// <summary> Returns a random key from a dicitonary. NOTE: creates a new list every time. not reccommened for large or unchanging dictionaries. </summary>
	public static TKey GetRandomKeyFromDict<TKey, TVal>(ref Dictionary<TKey, TVal> dict)
	{
		return System.Linq.Enumerable.ToList(dict.Keys)[UnityEngine.Random.Range(0, dict.Count)];
	}
	/// <summary> Returns a random value from a dicitonary. NOTE: creates a new list every time. not reccommened for large or unchanging dictionaries. </summary>
	public static TVal GetRandomValueFromDict<TKey, TVal>(ref Dictionary<TKey, TVal> dict)
	{
		return System.Linq.Enumerable.ToList(dict.Values)[UnityEngine.Random.Range(0, dict.Count)];
	}

	/// <summary> Returns an array filled with the declared values of an enum. </summary>
	public static T[] GetEnumValues<T>()
	{
		if (!typeof(T).IsEnum)
			throw new ArgumentException("Passed type is not an enum.");
		return (T[])Enum.GetValues(typeof(T));
	}

	public static float Truncate(this float val, int numPlaces)
	{
		double m = Math.Pow(10.0f, numPlaces);
		return (float)(Math.Truncate(m * val) / m);
	}

	/// <summary> Returns true if array[index].Equals() is true. </summary>
	public static bool ArrayContains<T>(ref T[] array, T value)
	{
		foreach (var val in array)
			if (val.Equals(value))
				return true;
		return false;
	}
	/// <summary> Returns true if array[index].Equals() is true. </summary>
	public static bool ArrayContains<T>(ref T[] array, T value, out int index)
	{
		for(int i = 0; i < array.Length; i++)
			if (array[i].Equals(value))
			{
				index = i;
				return true;
			}
		index = -1;
		return false;
	}

	/// <summary> Sets this transform to values defined by a matrix </summary>
	/// https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
	public static void FromMatrix(this Transform transform, Matrix4x4 matrix)
	{
		transform.localScale = matrix.ExtractScale();
		transform.rotation = matrix.ExtractRotation();
		transform.position = matrix.ExtractPosition();
	}

	/// <summary> Get rotation from a Matrix </summary>
	/// https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
	public static Quaternion ExtractRotation(this Matrix4x4 matrix)
	{
		Vector3 forward;
		forward.x = matrix.m02;
		forward.y = matrix.m12;
		forward.z = matrix.m22;

		Vector3 upwards;
		upwards.x = matrix.m01;
		upwards.y = matrix.m11;
		upwards.z = matrix.m21;

		return Quaternion.LookRotation(forward, upwards);
	}

	/// <summary> Get a position from a matrix </summary>
	/// https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
	public static Vector3 ExtractPosition(this Matrix4x4 matrix)
	{
		Vector3 position;
		position.x = matrix.m03;
		position.y = matrix.m13;
		position.z = matrix.m23;
		return position;
	}

	/// <summary> Get scale from a Matrix </summary>
	/// https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
	public static Vector3 ExtractScale(this Matrix4x4 matrix)
	{
		Vector3 scale;
		scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
		scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
		scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
		return scale;
	}

	/// <summary> Returns the value of 'child' if 'parent' was the origin. </summary>
	public static Quaternion GetRelativeRotation(Quaternion parent, Quaternion child)
	{
		return child * Quaternion.Inverse(parent);
	}

	/// <summary> Returns the value 'child' would have if 'parent' was the origin. </summary>
	public static Vector2 GetRelativePosition2D(Vector2 parent, Vector2 child)
	{
		return child - parent;
	}
	/// <summary> Returns the value 'child' would have if 'parent' was the origin. </summary>
	public static Vector3 GetRelativePostion3D(Vector3 parent, Vector3 child)
	{
		return child - parent;
	}

	/// <summary> Swaps two values if min is greater than max. </summary>
	public static bool Swap(ref float min, ref float max)
	{
		if (min > max)
		{
			min = min + max;
			max = min - max;
			min = min - max;
			return true;
		}
		return false;
	}

	/// <summary> Returns a vector between 'from' and 'to' if they are too far apart.  </summary>
	/// <param name="maxAngle">The max angle allowed in radians </param>
	/// <returns></returns>
	public static Vector2 GetVectorBetween(Vector2 from, Vector2 to, float maxAngle)
	{
		float ang1 = GetAngleFromVector(from); 
		float ang2 = GetAngleFromVector(to); 

		if (Mathf.Abs(ang1 - ang2) > maxAngle) 
		{
			if (ang1 < ang2)
				GetVectorFromAngle(ang1 - maxAngle);
			return GetVectorFromAngle(ang1 + maxAngle);
		}
		return to;
	}

	/// <summary> Returns a vector that points in a slightly different direction than the passed one. Uses Marsaglia to determine severity of change. </summary>
	/// <param name="maxNoise">The maximum angle in radians the vector can change by</param>
	public static Vector2 FuzzifyVector2(Vector2 vec, float maxNoise)
	{
		float weight = Random.Marsaglia(true) * (TWO_PI / maxNoise);
		return GetVectorFromAngle((GetAngleFromVector(vec) * (1 - weight)) + (Mathf.PI * weight));
	}

	/// <summary> Takes a list, then adds the desired capacity. </summary>
	/// <param name="fill"> Fills the added capacity with default values. </param>
	public static void AddCapacityToList<T>(ref List<T> list, int amount, bool fill = true)
	{
		int pCap = list.Capacity;
		list.Capacity = list.Capacity + amount;

		if (fill)
		{
			for (int i = pCap; i < list.Capacity; ++i)
				list.Add(default(T));
		}
	}

	/// <summary> Returns a 0 to 1 value between two numbers. </summary>
	/// <returns> (val - min) / (max - min) </returns>
	public static float NormalizeBetween(float val, float min, float max)
	{
		if (max == min) return 0.0f;
		else if (max < min)
		{
			max = min + max;
			min = max - min;
			max = max - min;
		}
		return (Mathf.Clamp(val, min, max) - min) / (max - min);
	}
	/// <summary> Returns a 0 to 1 value between two numbers. </summary>
	/// <returns> (val - min) / (max - min) </returns>
	public static double NormalizeBetween(double val, double min, double max)
	{
		if (max - min == 0) return 0.0;
		return (Clamp(val, min, max) - min) / (max - min);
	}

	public static double Clamp(double val, double min, double max)
	{
		return Math.Max(Math.Min(val, max), min);
	}
	/// <summary> Clamps a vector into a box defined by min and max. </summary>
	/// <param name="min"> Lower left corner of the box </param>
	/// <param name="max"> Upper right corner of the box. </param>
	/// <returns></returns>
	public static Vector2 ClampVector(Vector2 val, Vector2 min, Vector2 max)
	{
		val.x = Mathf.Clamp(val.x, min.x, max.x);
		val.y = Mathf.Clamp(val.y, min.y, min.x);
		return val;
	}

	public static T GetClosestObject2D<T>(List<T> objects, Vector2 pos) where T : MonoBehaviour
    {
        int cIndex = -1;
        float dist = float.MaxValue;

        foreach (var obj in objects)
        {
            float comp = SqrDist2D((Vector2)obj.transform.position, pos);
            if (comp < dist)
            {
                dist = comp;
                cIndex = objects.IndexOf(obj);
            }
        }

        return cIndex > -1 ? objects[cIndex] : null;
    }

	public static T GetFarthestObject2D<T>(List<T> objects, Vector2 pos) where T : MonoBehaviour
	{
		int cIndex = -1;
		float dist = float.MinValue;

		foreach (var obj in objects)
		{
			float comp = SqrDist2D((Vector2)obj.transform.position, pos);
			if (comp > dist)
			{
				dist = comp;
				cIndex = objects.IndexOf(obj);
			}
		}

		return cIndex > -1 ? objects[cIndex] : null;
	}

	/// <summary> Returns the best perpindiuclar of aDir based on Dot product with tDir  </summary>
	/// <param name="aDir"> The direction you want the perpindiculars of. </param>
	/// <param name="tDir"> The direction you are comparing against. </param>
	/// <returns></returns>
	public static Vector2 BestPerpindicular(Vector2 aDir, Vector2 tDir)
    {
        Vector2 perp = new Vector2(aDir.y, -aDir.x);
        return Vector2.Dot(perp, tDir) > Vector2.Dot(-perp, tDir) ? perp : -perp;
    }

    /// <summary> Steps through any given enum either up or down. </summary>
    public static T EnumLooper<T>(T currentValue, bool stepUp, int maxValue, int minValue = 0) where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException("It's not an enum, fam. What the fuck?");

        int nextVal = Convert.ToInt32(currentValue) + (stepUp ? 1 : -1);
        nextVal = ReverseClamp(nextVal, minValue, maxValue);

        return (T)(object)nextVal;
    }

    /// <summary>  Will clamp a number between min and max. If value is greater than max, will return min and vice versa. </summary>
    public static int ReverseClamp(int value, int min, int max)
    {
        if (value > max) return min;
        if (value < min) return max;
        return value;
    }
	/// <summary>  Will clamp a number between min and max. If value is greater than max, will return min and vice versa. </summary>
	public static float ReverseClamp(float value, float min, float max)
	{
		if (value > max) return min;
		if (value < min) return max;
		return value;
	}

	/// <summary> 
	/// Returns Mod(value + (-min), max + (-min)) + min.
	/// See more about how this function behaves: https://www.desmos.com/calculator/jgbixokd86
	/// </summary>
	public static int RemClamp(int value, int min, int max, bool maxInclusive = false)
	{
		max += maxInclusive ? 1 : 0;
		return Mod(value + -min, max + -min) + min;
	}
	/// <summary> 
	/// Returns Mod(value + (-min), max + (-min)) + min.
	/// See more about how this function behaves: https://www.desmos.com/calculator/jgbixokd86
	/// </summary>
	public static float RemClamp(float value, float min, float max, bool maxInclusive = true)
	{
		max += maxInclusive ? 1 : 0;
		return Mod(value + -min, max + -min) + min;
	}

    /// <summary>
    /// Returns a list of type T that contains elements
    /// </summary>
    public static List<T> CreateList<T>(params T[] elements)
    {
        return new List<T>(elements);
    }

    /// <summary>
    /// Returns an empty list tht is the type of T, it will not contain passed object.
    /// </summary>
    public static List<T> CreateList<T>(T type)
    {
        return new List<T>();
    }

    /// <summary>
    /// Returns the normal perpindicular of a vector.
    /// </summary>
    public static Vector2 GetPerpindiuclarOf(Vector2 vector)
    {
        vector.Normalize();
        return new Vector2(-vector.y, vector.x);
    }

    /// <summary> Returns the closest point in locations relative to pos </summary>
    public static int FindClosestPointIn(List<Vector2> locations, Vector2 pos)
    {
        int cIndex = 0;
        float dist = float.MaxValue;
        for (int a = 0; a < locations.Count; a++)
        {
            float comp = SqrDist2D(pos, locations[a]);
            if (comp < dist)
            {
                cIndex = a;
                dist = comp;
            }
        }

        return cIndex;
    }

    /// <summary> Returns the closest point in locations relative to pos </summary>
    public static int FindClosestPointIn(Vector2[] locations, Vector2 to)
    {
        int cIndex = 0;
        float dist = float.MaxValue;
        for (int a = 0; a < locations.Length; a++)
        {
            float comp = SqrDist2D(to, locations[a]);
            if (comp < dist)
            {
                cIndex = a;
                dist = comp;
            }
        }

        return cIndex;
    }


    public static float Mod(float a, float b)
    {
        return (a % b + b) % b;
    }

    public static int Mod(int a, int b)
    {
        return (a % b + b) % b;
    }

    /// <summary>
    /// Turn a |0 to 360| angle to a |-180 to 180| angle
    /// </summary>
    public static float Angle360ToAngle180(float angle)
    {
        return angle > 180 ? -180 + (angle - 180) : angle;
    }

    /// <summary>
    /// Turn a |-180 - 180| angle to a |0 - 360| angle
    /// </summary>
    public static float Angle180ToAngle360(float angle)
    {
        return angle < 0 ? angle + 360 : angle;
    }

    /// <summary>
    /// Turn a |0 - 2pi| angle to |-pi to pi| angle.
    /// </summary>
    public static float Angle2PiToAnglePi(float angle)
    {
        return angle > Mathf.PI ? -Mathf.PI + (angle - Mathf.PI) : angle;
    }
    /// <summary> Turn a |-pi - pi| to a |0 - 2pi| angle. </summary>
    public static float AnglePiToAngle2Pi(float angle)
    {
        return angle < 0 ? angle + TWO_PI : angle;
    }

    /// <summary> Returns true if a - b is less than threshold </summary>
    public static bool Approximately(float a, float b, float threshold)
    {
        return Mathf.Abs(a - b) <= threshold;
    }


    /// <summary> Return the angle of a vector in radians. </summary>
    public static float GetAngleFromVector(Vector2 direction)
    {
        direction.Normalize();
        return Mathf.Atan2(direction.y, direction.x);
    }

    /// <summary> Return a Z rotation quaternion from a vector. </summary>
    public static Quaternion GetRotationFromVector(Vector2 direction)
    {
        return Quaternion.Euler(0, 0, GetAngleFromVector(direction));
    }

    /// <summary>
    /// Returns a normal vector corresponding to an angle in radians.
    /// </summary>
    /// <param name="angle">USE RADIANS</param>
    /// <returns></returns>
    public static Vector2 GetVectorFromAngle(float angle)
    {
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); ;
    }

    /// <summary>
    /// Returns a point on a cirle.
    /// </summary>
    /// <param name="angle">USE RADIANS</param>
    /// <returns></returns>
    public static Vector2 GetPointOnCircle(Vector2 pos, float radius, float angle)
    {
        return (GetVectorFromAngle(angle) * radius) + pos;
    }
    /// <summary> Return a random point on a circle </summary>
    public static Vector2 GetRandomPointOnCircle(Vector2 pos, float radius)
    {
        return GetPointOnCircle(pos, radius, UnityEngine.Random.Range(0, TWO_PI));
    }
    /// <summary> Return a random point between the unit circle and the radius) </summary>
    public static Vector2 GetRandomPointInCircle(Vector2 pos, float radius)
    {
        if (radius < 1.0f)
            return Vector2.zero;
        return GetPointOnCircle(pos, UnityEngine.Random.Range(1, radius), UnityEngine.Random.Range(0, TWO_PI));
    }

    /// <summary>
    /// Returns the midpoint between two vectors
    /// </summary>
    public static Vector2 GetMidPoint(Vector2 a, Vector2 b)
    {
        return new Vector2(GetMidValue(a.x, b.x), GetMidValue(a.y, b.y));
    }

    /// <summary>
    /// Returns the midpoint between two floats.
    /// </summary>
    public static float GetMidValue(float min, float max)
    {
		if(min > max)
			return ((min - max) / 2) + max;
		return ((max - min) / 2) + min;
    }

	/// <summary> Returns val is between min and max, or if it is equal to them. </summary>
	/// <param name="strict">If true, will return true only if val is between </param>
	public static bool ValueIsBetween(float val, float min, float max, bool strict)
    {
		if (strict)
			return val > min && val < max;
		return val >= min && val <= max;
    }

	/// <summary> Returns val is between min and max, or if it is equal to them. </summary>
	/// <param name="strict">If true, will return true only if val is between </param>
	public static bool ValueIsBetween(int val, int min, int max, bool strict)
	{
		if (strict)
			return val > min && val < max;
		return val >= min && val <= max;
	}


	public static float SqrDist2D(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude;
    }
	public static float SqrDist2D(Vector2 a, Vector3 b)
	{
		return (a - (Vector2)b).sqrMagnitude;
	}

	public static float SqrDist3D(Vector3 a, Vector3 b)
	{
		return (a - b).sqrMagnitude;
	}

	/// <summary> Returns true if point1 is between point2 and point3 (on both axis) </summary>
	/// <param name="strict">If true, will return true only if val is between </param>
	public static bool VectorIsBetween(Vector2 val, Vector2 min, Vector2 max, bool strict)
    {
		return ValueIsBetween(val.x, min.x, max.x, strict) && ValueIsBetween(val.y, min.y, max.y, strict);
    }

	/// <summary> Returns the smallest angle between A and B relative to a custom origin. RETURNS RADIANS </summary>
	public static float GetAngleBetween3D(Vector3 a, Vector3 b, Vector3 origin)
	{
		return Mod(Vector3.Angle(a - origin, b - origin), 360.0f) * Mathf.Deg2Rad;
	}

	/// <summary> Returns the smallest angle between A and B relative to a custom origin. RETURNS RADIANS </summary>
	public static float GetAngleBetween(Vector2 a, Vector2 b, Vector2 origin)
    {
		return Mod(Vector2.Angle(a - origin, b - origin), 360.0f) * Mathf.Deg2Rad;
	}
	/// <summary>
	/// Returns the angle between A and B. RETURNS RADIANS
	/// </summary>
	public static float GetAngleBetween(Vector2 a, Vector2 b)
	{
		return Mod(Vector2.Angle(a, b), 360.0f) * Mathf.Deg2Rad;
	}

	/// <summary>
	/// Returns the normalized direction of 'a' from 'b'
	/// </summary>
	public static Vector2 Direction2D(Vector2 to, Vector2 from)
    {
        return (to - from).normalized;
    }
    /// <summary>
    /// Returns the normalized direction of 'a' from 'b'.
    /// </summary>
    public static Vector2 Direction2D(Vector2 to, Vector3 from)
    {
        return Direction2D(to, (Vector2)from);
    }
    /// <summary>
    /// Returns the normalized direction of 'a' from 'b'.
    /// </summary>
    public static Vector2 Direction2D(Vector3 to, Vector2 from)
    {
        return Direction2D((Vector2)to, from);
    }
	/// <summary>
	/// Returns the normalized direction of 'a' from 'b'
	/// </summary>
	public static Vector2 Direction2D(Vector3 to, Vector3 from)
	{
		return (to - from).normalized;
	}

	/// <summary> Returns the normalized direction of 'a' from 'b'  </summary>
	public static Vector3 Direction3D(Vector2 to, Vector2 from)
	{
		return (to - from).normalized;
	}
	/// <summary>  Returns the normalized direction of 'a' from 'b'. </summary>
	public static Vector3 Direction3D(Vector2 to, Vector3 from)
	{
		return Direction2D((Vector3)to, from);
	}
	/// <summary> Returns the normalized direction of 'a' from 'b'. </summary>
	public static Vector3 Direction3D(Vector3 to, Vector2 from)
	{
		return Direction2D(to, (Vector3)from);
	}
	/// <summary> Returns the normalized direction of 'a' from 'b' </summary>
	public static Vector3 Direction3D(Vector3 to, Vector3 from)
	{
		return (to - from).normalized;
	}

	/// <summary>
	/// Returns true if the two vectors are within the distance defined by threshold.
	/// </summary>
	public static bool VectorApproxD(Vector2 p1, Vector2 p2, float threshold = 0.0001f)
    {
        return SqrDist2D(p1, p2) <= threshold * threshold;
    }

    /// <summary>
    /// Returns true if the two vectors are within an angular difference (with respect to 0,0) in degrees defined by threshold.
    /// </summary>
    public static bool VectorApproxA(Vector2 p1, Vector2 p2, float threshold = 0.0001f)
    {
        p1.Normalize();
        p2.Normalize();
        threshold *= Mathf.Deg2Rad;

        float ang1 = Mod(Mathf.Atan2(p1.y, p1.x), TWO_PI);
        float ang2 = Mod(Mathf.Atan2(p2.y, p2.x), TWO_PI);

        if (ang1 < ang2)
            return (ang2 - ang1) <= threshold;
        return (ang1 - ang2) <= threshold;
    }

    public static bool IsWithinBounds(Vector2 aPos, float width, float height, Vector2 bPos)
    {
        if (Mathf.Abs(aPos.x - bPos.x) <= width / 2)
            if (Mathf.Abs(aPos.y - bPos.y) <= height / 2)
                return true;
        return false;
    }
}

using UnityEngine;
using System.Collections.Generic;

public class GraphMaker : MonoBehaviour
{
    [System.Serializable]
    public class GraphPoint
    {
        [System.Serializable]
        public class ConnectionData
        {
            public int index;
            public float dist;

            public ConnectionData(int index, float distance)
            {
                this.index = index;
                dist = distance;
            }
        }
        [System.Serializable]
        public class NavData
        {
            public int pIndex;
            public bool wasTarget;
            public bool evaluated;
            public float tDist;

            public NavData(int index, float distance)
            {
                pIndex = index;
                tDist = distance;
                wasTarget = false;
                evaluated = false;
            }
        }
		
        public bool isBlocked = false;
        public Vector2 position = Vector2.zero;
		public Vector2Int boardPos = Vector2Int.zero;
        public NavData navData = null;
        public List<ConnectionData> connections = new List<ConnectionData>();

        public bool IsConnectedTo(int index)
        {
            foreach (var link in connections)
                if (link.index == index)
                    return true;
            return false;
        }
    }

    public bool scanGraph = false;
	[Header("Generation Settings")]
	[SerializeField] Vector2Int boardDimensions;
	[SerializeField] float squareSideSize = 5.0f;
	[Header("Generation Control")]
    [SerializeField] bool generateBoard;
	[SerializeField] bool generateBlocks;
	[SerializeField] bool generateDiagnols;
	[SerializeField] bool generateRandomDiagnols;
	[SerializeField] float diagChance = 0.2f;
	[SerializeField] float blockCreationChance = 0.2f;
	[Header("Graph Information")]
	public int numBlocks;
	public int numConnections;
	public int numPoints { get { return graphPoints.Count; }  }
    public List<GraphPoint> graphPoints = new List<GraphPoint>();
    [Header("Graph Debug NavData")]
    [SerializeField] bool navigate;
	[SerializeField] bool clearNavData;
	[SerializeField] int startIndex = 0;
	[SerializeField] int finalIndex = 0;
	[SerializeField] List<int> navPath = new List<int>();

	public Vector2Int dimensions { get { return boardDimensions; } }

    private void Awake()
    {
    }

    // Graph Internal Utility
    #region

    void AddPoint(Vector2 pos, Vector2Int boardPos)
    {
        GraphPoint newPoint = new GraphPoint();
        newPoint.position = pos;
		newPoint.boardPos = boardPos;
        graphPoints.Add(newPoint);
    }

    void ConnectPoints(int indexA, int indexB)
    {
        float dist = Vector2.Distance(graphPoints[indexA].position, graphPoints[indexB].position);

        foreach (var link in graphPoints[indexA].connections)
            if (link.index == indexB)
                return;

		numConnections++;
        graphPoints[indexA].connections.Add(new GraphPoint.ConnectionData(indexB, dist));
        graphPoints[indexB].connections.Add(new GraphPoint.ConnectionData(indexA, dist));
    }

    void DisconnectPoints(int indexA, int indexB)
    {
		bool success = false;
        foreach (var link in graphPoints[indexA].connections)
            if (link.index == indexB)
            {
				success = true;
                graphPoints[indexA].connections.Remove(link);
                break;
            }
        foreach (var link in graphPoints[indexB].connections)
            if (link.index == indexA)
            {
				success = true;
                graphPoints[indexB].connections.Remove(link);
                break;
            }
		if (success) numConnections--;
    }

    void ClearPointNavData()
    {
        foreach (var point in graphPoints)
            point.navData = new GraphPoint.NavData(0, float.MaxValue);
		navPath.Clear();
    }

    public Vector2 PointPos(int index)
    {
        return graphPoints[index].position;
    }

	#endregion

	#region Generation Functions

	public void GenerateBoard(int width, int height, float squareSize)
	{
		boardDimensions.x = width;
		boardDimensions.y = height;
		squareSideSize = squareSize;
		GenerateBoard();
	}
	public void GenerateBoard(Vector2Int dim, float squareSize)
	{
		boardDimensions = dim;
		squareSideSize = squareSize;
		GenerateBoard();
	}
	void GenerateBoard()
	{
		GeneratePoints();
		FinalizeConections();
		ClearPointNavData();
	}

    // index = col + (boardDimensions.y * row)
    void GeneratePoints()
    {
		Vector2 gameBoardScale = (Vector2)boardDimensions * squareSideSize;
		Vector2 startPos = (Vector2)transform.position - (gameBoardScale / 2) + new Vector2(squareSideSize/2, squareSideSize/2); // The lower right corner of the board.

		Debug.Log("Scale: " + gameBoardScale + "\nStartPos: " + startPos);

        if (graphPoints == null)
            graphPoints = new List<GraphPoint>();
        graphPoints.Clear();
		
        for (int x = 0; x < boardDimensions.x; ++x)
        {
            for (int y = 0; y < boardDimensions.y; ++y)
            {
				AddPoint(startPos + (new Vector2(x, y) * squareSideSize), new Vector2Int(x, y));
            }
        }

		numConnections = numBlocks = 0;
    }

	void FinalizeConections()
	{
		// index = col + (boardDimensions.y * row)
		int index = 0;
		float mDist = squareSideSize * squareSideSize;
		for (int r = 0; r < boardDimensions.y; r++)
		{
			for (int c = 0; c < boardDimensions.x; c++, index++)
			{
				int currIndex = c + (r * boardDimensions.y);
				if (c < boardDimensions.x - 1)
					ConnectPoints(currIndex, currIndex + 1);
				if (r < boardDimensions.y - 1)
					ConnectPoints(currIndex, c + ((r + 1) * boardDimensions.y));
				if (generateDiagnols)
				{
					if (r < boardDimensions.y - 1)
					{
						if (c != 0)
							ConnectPoints(currIndex, (c - 1) + ((r + 1) * boardDimensions.y));
						if (c < boardDimensions.x - 1)
							ConnectPoints(currIndex, (c + 1) + ((r + 1) * boardDimensions.y));
					}
				}
				else if (generateRandomDiagnols)
					RandomDiagConnections(currIndex, c, r);
			}
		}
	}

	void RandomDiagConnections(int currIndex, int c, int r)
	{
		if (r < boardDimensions.y - 1)
		{
			if (c != 0 && Random.value < diagChance)
				ConnectPoints(currIndex, (c - 1) + ((r + 1) * boardDimensions.y));
			if (c < boardDimensions.x - 1 && Random.value < diagChance)
				ConnectPoints(currIndex, (c + 1) + ((r + 1) * boardDimensions.y));
		}
	}

    void GenerateRandomBlocks()
    {
        foreach (var point in graphPoints)
            if (point.connections.Count > 2)
            {
                bool valid = true;
                
                foreach(var link in  point.connections)
                    if (graphPoints[link.index].connections.Count <= 2)
                        valid = false;

                if (valid && graphPoints.IndexOf(point) != 0 && graphPoints.IndexOf(point) != 99)
                {
                    float chance = Random.value;
                    if (Random.value <= blockCreationChance)
                    {
                        point.isBlocked = true;
                        Debug.Log("Connection count: " + point.connections.Count);
                        while (point.connections.Count > 0)
                            DisconnectPoints(graphPoints.IndexOf(point), point.connections[0].index);
						numBlocks++;
                    }
                }
            }
    }
    #endregion

    // Graph Navigation.
    #region

    void NavigateBetweenAstar(int indexA, int indexB)
    {
        int nextIndex = indexB;
        var q = new List<int>();
        int cIndex = 0;
        bool foundTarget = false;
        float dist = float.MaxValue;

        startIndex = indexA;
        finalIndex = indexB;

        if (startIndex == finalIndex)
            return;

        ClearPointNavData();
        graphPoints[indexA].navData = new GraphPoint.NavData(indexA, 0.0f);
        graphPoints[indexB].navData.wasTarget = true;

        foreach (var link in graphPoints[indexA].connections)
        {
            float comp = Mathc.SqrDist2D(PointPos(link.index), PointPos(indexB));
            if (comp < dist)
            {
                dist = comp;
                cIndex = graphPoints[indexA].connections.IndexOf(link);
            }
            else
                graphPoints[link.index].navData.evaluated = true;
        }

        graphPoints[indexA].navData.evaluated = true;
        dist = graphPoints[indexA].connections[cIndex].dist;
        cIndex = graphPoints[indexA].connections[cIndex].index;
        graphPoints[cIndex].navData = new GraphPoint.NavData(indexA, dist);
        q.Add(cIndex);

        while (q.Count > 0)
        {
            int curIndex = q[0];

            if (curIndex == indexB)
                foundTarget = true;
            else if (graphPoints[curIndex].navData.evaluated)
            {
                q.Remove(curIndex);
                continue;
            }

           cIndex = 0;
            dist = float.MaxValue;
            foreach (var link in graphPoints[curIndex].connections)
            {
                if (graphPoints[link.index].navData.evaluated)
                    continue;

                float comp = Mathc.SqrDist2D(PointPos(link.index), PointPos(indexB));
                if (comp < dist)
                {
                    dist = comp;
                    cIndex = graphPoints[curIndex].connections.IndexOf(link);
                }
                else
                    graphPoints[link.index].navData.evaluated = true;
            }

            dist = graphPoints[curIndex].connections[cIndex].dist + graphPoints[curIndex].navData.tDist;
            cIndex = graphPoints[curIndex].connections[cIndex].index;

           if (dist < graphPoints[cIndex].navData.tDist)
            graphPoints[cIndex].navData = new GraphPoint.NavData(curIndex, dist);

            if (!foundTarget)
                q.Add(cIndex);

            graphPoints[curIndex].navData.evaluated = true;
            q.Remove(curIndex);
        }

        // Record path.
        q.Clear();
        q.Add(nextIndex);
        for (int a = 0; a < graphPoints.Count && nextIndex != indexA; a++)
        {
            nextIndex = graphPoints[nextIndex].navData.pIndex;
            q.Add(nextIndex);
        }
        

        navPath.Clear();
        for (int a = 0; a < q.Count; a++)
            navPath.Add(q[q.Count - (a + 1)]);
    }

	List<GraphPoint.ConnectionData> links = new List<GraphPoint.ConnectionData>(4);
    void NavigateBetweenDijk(int indexA, int indexB)
    {
        int nextIndex = indexB;
        var q = new List<int>();
        bool foundTarget = false;
        float dist = float.MaxValue;

        startIndex = indexA;
        finalIndex = indexB;

        if (startIndex == finalIndex)
            return;

        ClearPointNavData();
        graphPoints[indexA].navData = new GraphPoint.NavData(indexA, 0.0f);
        graphPoints[indexA].navData.evaluated = true;
        graphPoints[indexB].navData.wasTarget = true;

		links.Clear();
		foreach (var link in graphPoints[indexA].connections)
			links.Add(link);
		while(links.Count > 0)
		{
			int rIndex = Random.Range(0, links.Count);
			var link = links[rIndex];
			graphPoints[link.index].navData = new GraphPoint.NavData(indexA, link.dist);
			q.Add(link.index);
			links.RemoveAt(rIndex);
		}

        while (q.Count > 0)
        {
            int curIndex = q[0];

            if (curIndex == indexB)
                foundTarget = true;
            else if (graphPoints[curIndex].navData.evaluated)
            {
                q.Remove(curIndex);
                continue;
            }
            
            dist = float.MaxValue;
			links.Clear();
			foreach (var link in graphPoints[curIndex].connections)
				links.Add(link);
			while (links.Count > 0)
			{
				int rIndex = Random.Range(0, links.Count);
				var link = links[rIndex];

				dist = link.dist + graphPoints[curIndex].navData.tDist;
				if (dist < graphPoints[link.index].navData.tDist)
					graphPoints[link.index].navData = new GraphPoint.NavData(curIndex, dist);
				if (!foundTarget && !graphPoints[link.index].navData.evaluated)
					q.Add(link.index);

				links.RemoveAt(rIndex);
			}

			graphPoints[curIndex].navData.evaluated = true;
            q.Remove(curIndex);
        }

        // Record path.
        q.Clear();
        q.Add(nextIndex);
        for (int a = 0; a < graphPoints.Count && nextIndex != indexA; a++)
        {
            nextIndex = graphPoints[nextIndex].navData.pIndex;
            q.Add(nextIndex);
        }


        navPath.Clear();
        for (int a = 0; a < q.Count; a++)
            navPath.Add(q[q.Count - (a + 1)]);
    }

    public List<int> GetPath(Vector2 startPos, Vector2 endPos)
    {
        List<int> retval = new List<int>();

        NavigateBetweenDijk(GetClosestPointTo(startPos), GetClosestPointTo(endPos));

        for (int a = 0; a < navPath.Count; a++)
            retval.Add(navPath[a]);

        return retval;

    } 

    /// <summary> Generate a random path with the closest point to 'pos' being the start.  </summary>
    public List<int> GetRandomPathFrom(Vector2 pos)
    {
        int indexA = GetClosestPointTo(pos);
        int indexB = Random.Range(0, graphPoints.Count - 1);
        List<int> retval = new List<int>();

        while(graphPoints[indexB].isBlocked)
            indexB = Random.Range(0, graphPoints.Count - 1); ;

        NavigateBetweenAstar(indexA, indexB);

        for (int a = 0; a < navPath.Count; a++)
            retval.Add(navPath[a]);

        return retval;
    }

    #endregion

    // Misc Public Graph Data
    #region

    public int GetClosestPointTo(Vector2 pos)
    {
        int cIndex = 0;
        float dist = float.MaxValue;
        foreach (var point in graphPoints)
        {
            if (point.isBlocked)
                continue;
            float comp = Mathc.SqrDist2D(point.position, pos);
            if (comp < dist)
            {
                dist = comp;
                cIndex = graphPoints.IndexOf(point);
            }
        }

        return cIndex;
    }

    /// <summary>  Returns the closest point to "pos" that is connected to "index"   </summary>
    int GetClosestConnectedPoint(int index, Vector2 pos)
    {
        int cIndex = 0;
        float dist = 0;
        foreach(var link in graphPoints[index].connections)
        {
            if (graphPoints[link.index].isBlocked)
                continue;
            float comp = Mathc.SqrDist2D(PointPos(link.index), pos);
            if(comp < dist)
            {
                dist = comp;
                cIndex = link.index;
            }
        }

        return cIndex;
    }

    public bool ScanGraphForBlocks()
    {
        bool foundBlock = false;
        foreach (var point in graphPoints)
            if (point.isBlocked)
            {
                while (point.connections.Count > 0)
                    DisconnectPoints(graphPoints.IndexOf(point), point.connections[0].index);
                foundBlock = true;
            }
        ClearPointNavData();
        return foundBlock;
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (generateBoard)
        {
            Awake();
			GenerateBoard();
            generateBoard = false;
        }

        if(generateBlocks)
        {
            scanGraph = true;
            generateBlocks = false;
            GenerateRandomBlocks();
        }

        if (scanGraph)
        {
            //FinalizeConections();
            ScanGraphForBlocks();
            navPath.Clear();
            scanGraph = false;
        }

		if(clearNavData)
		{
			ClearPointNavData();
			clearNavData = false;
		}
        if(navigate)
        {
            NavigateBetweenDijk(startIndex, finalIndex);
			navigate = false;
        }

        if (graphPoints != null)
        {
            foreach (var point in graphPoints)
            {
                if (point.isBlocked)
                    Gizmos.color = Color.black;
                else if (point.navData.evaluated)
                    Gizmos.color = Color.cyan;
                else
                    Gizmos.color = Color.red;
                Gizmos.DrawSphere(point.position, 1.0f);
            }

            foreach (var point in graphPoints)
                if (point.connections != null)
                    foreach (var link in point.connections)
                    {
                        if (link.index < graphPoints.IndexOf(point))
                        {
                            if (graphPoints[link.index].navData.evaluated)
                                Gizmos.color = Color.cyan;
                            else
                                Gizmos.color = Color.magenta;
                            Gizmos.DrawLine(graphPoints[link.index].position, point.position);
                        }
                    }

            if (navPath.Count > 0)
            {
                Gizmos.color = Color.green;
                for (int a = 0; a < navPath.Count - 1; a++)
                    Gizmos.DrawLine(PointPos(navPath[a]), PointPos(navPath[a + 1]));
                foreach (var index in navPath)
                {
                    Gizmos.color = Color.green;
                    if (index == startIndex)
                        Gizmos.color = Color.blue;
                    else if (index == finalIndex)
                        Gizmos.color = Color.yellow;

                    Gizmos.DrawSphere(PointPos(index), 1.0f);
                }
            }
        }
    }

	// col + rowlength * row
	public GraphPoint GetGraphPoint(int x, int y){
		return graphPoints [y + boardDimensions.y * x];
	}

	public bool IsPosInGridPos(Vector2 pos, int gridX, int gridY){
		return pos.x >= gridX * squareSideSize && pos.x <= gridX * (squareSideSize + 1)
			&& pos.y >= gridY * squareSideSize && pos.y <= gridY * (squareSideSize + 1);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MeshFilter walls;
    public float wallHeight;

    public SquareGrid squareGrid;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    [Header("Colors to paint gizmos")]
    public Color wallColor;
    public Color spaceColor;
    public Color mediumPointColor;

    public void GenerateMesh(int[,] map, float squareSize)
    {
        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(map, squareSize);

        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        CreateWallMesh();
    }

    void CreateWallMesh()
    {
        CalculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();

        foreach(List<int> outline in outlines)
        {
            for(int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); //Top Left 0
                wallVertices.Add(vertices[outline[i + 1]]); //Top Right 1 
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); //Bottom Left 2
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); //Bottom Right 3

                //Create wall anti-clockwise to make visible from inside
                wallTriangles.Add(startIndex + 0); //TOP LEFT
                wallTriangles.Add(startIndex + 2); //BOTTOM LEFT
                wallTriangles.Add(startIndex + 3); //BOTTOM RIGHT

                wallTriangles.Add(startIndex + 3); //BOTTOM RIGHT
                wallTriangles.Add(startIndex + 1); //TOP RIGHT
                wallTriangles.Add(startIndex + 0); //TOP LEFT
            }
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.RecalculateNormals();

        walls.mesh = wallMesh;
    }

    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            // 1 points:
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            // 2 points:
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 3 point:
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                //In this case , none off the vertices can be an outline
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);
    }

    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            //Vertex index default is -1
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);

        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    void CalculateMeshOutlines()
    {
        //Follow an outline all the way back to itself and add to hash

        for(int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if(!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConectedOutlineVertex(vertexIndex);

                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);
                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);

        int nextVertexIndex = GetConectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
            FollowOutline(nextVertexIndex, outlineIndex);
    }

    int GetConectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> list = triangleDictionary[vertexIndex];

        for(int i = 0; i < list.Count; i++)
        {
            Triangle triangle = list[i];

            for(int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];

                if (vertexIndex != vertexB && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                        return vertexB;
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        // if vertex A and vertex B only share one triangle, they are an outline edge

        List<Triangle> trianglesVertexA = triangleDictionary[vertexA];

        int count = 0;

        for(int i = 0; i < trianglesVertexA.Count; i++)
        {
            if (trianglesVertexA[i].ContainsVertex(vertexB))
                count++;

            if (count > 1) break;
        }

        return count == 1;
    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (triangleDictionary.ContainsKey(vertexIndexKey))
            triangleDictionary[vertexIndexKey].Add(triangle);
        else
        {
            List<Triangle> list = new List<Triangle>();
            list.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, list);
        }
    }

    private void OnDrawGizmos()
    {
        if (squareGrid != null)
        {
            /*
            for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
            {
                for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
                {
                    Gizmos.color = (squareGrid.squares[x, y].topLeft.active) ? wallColor : spaceColor;
                    Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * 0.4f);

                    Gizmos.color = (squareGrid.squares[x, y].topRight.active) ? wallColor : spaceColor;
                    Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * 0.4f);

                    Gizmos.color = (squareGrid.squares[x, y].bottomRight.active) ? wallColor : spaceColor;
                    Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * 0.4f);

                    Gizmos.color = (squareGrid.squares[x, y].bottomLeft.active) ? wallColor : spaceColor;
                    Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * 0.4f);


                    Gizmos.color = mediumPointColor;
                    Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * 0.15f);

                    Gizmos.color = mediumPointColor;
                    Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * 0.15f);

                    Gizmos.color = mediumPointColor;
                    Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * 0.15f);

                    Gizmos.color = mediumPointColor;
                    Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * 0.15f);
                }
            }
            */
        }
    }


    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = vertexIndexA;
            vertices[1] = vertexIndexB;
            vertices[2] = vertexIndexC;
        }

        public int this[int i]
        {
            get{ return vertices[i]; }
        }

        public bool ContainsVertex(int vertex)
        {
            return (vertex == vertexIndexA) || (vertex == vertexIndexB) || (vertex == vertexIndexC);
        }
    }

    #region Classes used in mesh

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int [,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);

            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            
            ControllNode[,] controllNodes = new ControllNode[nodeCountX, nodeCountY];

            //Create all controll nodes to use in the squares grid
            for(int x = 0; x < nodeCountX; x++)
            {
                for(int y = 0; y < nodeCountY; y++)
                {
                    Vector3 position = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0, -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controllNodes[x, y] = new ControllNode(position, map[x, y] == 1, squareSize);
                }
            }

            //Squares use minimum 2 controll nodes in each direction, so the [,] dimension is -1
            squares = new Square[nodeCountX - 1, nodeCountY - 1];

            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controllNodes[x, y+1], controllNodes[x+1, y+1], controllNodes[x+1, y], controllNodes[x, y]);
                }
            }
        }
    }

    public class Square
    {
        public ControllNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;
        public int configuration = 0;

        public Square(ControllNode topLeft, ControllNode topRight, ControllNode bottomRight, ControllNode bottomLeft)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
            this.bottomLeft = bottomLeft;

            centerTop = topLeft.right;
            centerRight = bottomRight.above;
            centerBottom = bottomLeft.right;
            centerLeft = bottomLeft.above;

            if (topLeft.active)
                configuration += 8;
            if (topRight.active)
                configuration += 4;
            if (bottomRight.active)
                configuration += 2;
            if (bottomLeft.active)
                configuration += 1;
        }
    }

    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 position)
        {
            this.position = position;
        }
    }

    public class ControllNode : Node
    {
        public bool active;

        public Node above;
        public Node right;

        public ControllNode(Vector3 position, bool active, float squareSize) : base(position)
        {
            this.active = active;

            above = new Node(position + (Vector3.forward * squareSize / 2f));
            right = new Node(position + (Vector3.right * squareSize / 2f));
        }
    }

    #endregion
}

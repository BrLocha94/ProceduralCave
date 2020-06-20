using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    SquareGrid squareGrid;

    [Header("Colors to paint gizmos")]
    public Color wallColor;
    public Color spaceColor;
    public Color mediumPointColor;

    public void GenerateMesh(int [,] map, float squareSize)
    {
        squareGrid = new SquareGrid(map, squareSize);
    }

    private void OnDrawGizmos()
    {
        if (squareGrid != null)
        {
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
                    Vector3 position = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, -mapHeight / 2 + y * squareSize + squareSize / 2, 1f);
                    controllNodes[x, y] = new ControllNode(position, map[x, y] == 1, squareSize);
                }
            }

            //Squares use minimum 2 controll nodes in each direction, so the [,] dimension is -1
            squares = new Square[nodeCountX - 1, nodeCountY - 1];

            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controllNodes[x, y], controllNodes[x+1, y], controllNodes[x+1, y+1], controllNodes[x, y+1]);
                }
            }
        }
    }

    public class Square
    {
        public ControllNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;

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
            this.position = position;
            this.active = active;

            above = new Node(position + (Vector3.forward * squareSize / 2));
            right = new Node(position + (Vector3.right * squareSize / 2));
        }
    }

    #endregion
}

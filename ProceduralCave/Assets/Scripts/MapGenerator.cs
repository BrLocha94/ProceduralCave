using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Seed to be used on map generation")]
    public string seed;

    [Header("Enable this to ignore seed above and use and random value")]
    public bool useRandomSeed;

    [Header("Value used on random seed to generate map")]
    [Range(0, 100)]
    public int fillPercentage;

    [Header("Smooth map general params")]
    [Range(0, 8)]
    public int smoothness;
    [Range(0, 5)]
    public int smoothProcess;

    [Header("Region processing general params")]
    public bool aplyWallRegionProcess;
    public int wallRegionTolerence;
    public bool aplyRoomRegionProcess;
    public int roomRegionTolerence;

    [Header("Map dimensions")]
    public int width;
    public int height;
    public int borderSize;

    [Header("Colors to paint gizmos")]
    public Color wallColor;
    public Color spaceColor;

    private int[,] map;
    private int[,] borderedMap;

    public MeshGenerator meshGenerator;

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GenerateMap();
    }

    void GenerateMap()
    {
        map = new int[width, height];

        FillMap();

        for (int i = 0; i < smoothProcess; i++)
        {
            SmoothMap();
        }

        ProcessMap();

        CreateBorder();

        if (meshGenerator != null)
            meshGenerator.GenerateMesh(borderedMap, 1);
    }

    void ProcessMap()
    {
        if (aplyWallRegionProcess)
        {
            List<List<Coordinate>> wallRegions = GetRegions(1);

            foreach (List<Coordinate> region in wallRegions)
            {
                if (region.Count < wallRegionTolerence)
                {
                    foreach (Coordinate tile in region)
                    {
                        map[tile.tileX, tile.tileY] = 0;
                    }
                }
            }
        }

        if (aplyRoomRegionProcess)
        {
            List<List<Coordinate>> roomRegions = GetRegions(0);

            foreach (List<Coordinate> room in roomRegions)
            {
                if (room.Count < roomRegionTolerence)
                {
                    foreach (Coordinate tile in room)
                    {
                        map[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }
    }

    List<List<Coordinate>> GetRegions(int tileType)
    {
        List<List<Coordinate>> regions = new List<List<Coordinate>>();
        int[,] mapFlags = new int[width, height];

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if(mapFlags[x,y] == 0 && map[x,y] == tileType)
                {
                    List<Coordinate> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach(Coordinate tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    List<Coordinate> GetRegionTiles(int startX, int startY)
    {
        List<Coordinate> tiles = new List<Coordinate>();

        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Coordinate> queue = new Queue<Coordinate>();
        queue.Enqueue(new Coordinate(startX, startY));
        mapFlags[startX, startY] = 1; //Checked position

        while(queue.Count > 0)
        {
            Coordinate tile = queue.Dequeue();
            tiles.Add(tile);

            for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for(int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if(IsInMapRange(x, y) && (x == tile.tileX || y == tile.tileY))
                    {
                        if(mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;//Checked position
                            queue.Enqueue(new Coordinate(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    void CreateBorder()
    {
        borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        for(int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for(int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                else
                    borderedMap[x, y] = 1;
            }
        }
    }

    void SmoothMap()
    {
        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int wallCount = SurroundWallCount(x, y);

                if (wallCount > smoothness)
                    map[x, y] = 1;
                else if (wallCount < smoothness)
                    map[x, y] = 0;
                //If is equal to smoothness, stays the same to add some randomic effect
            }
        }
    }

    int SurroundWallCount(int x, int y)
    {
        int wallCount = 0;

        for(int i = x - 1; i <= x + 1; i++)
        {
            for(int j = y - 1; j <= y + 1; j++)
            {
                if (IsInMapRange(i, j))
                {
                    if (i != x || j != y)
                        wallCount += map[i, j];
                }
                //If on border, increase value to force wall proprety
                else
                    wallCount++;
            }
        }

        return wallCount;
    }

    void FillMap()
    {
        if(useRandomSeed == true)
            seed = Time.time.ToString();

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                //map edges always are walls
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    map[x, y] = 1;
                else
                    map[x, y] = (pseudoRandom.Next(0, 100) < fillPercentage) ? 1 : 0;
            }
        }
    }

    struct Coordinate
    {
        public int tileX;
        public int tileY;

        public Coordinate(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    private void OnDrawGizmos()
    {
        /*
        if (canDrawGizmos == true && map != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.color = (map[x, y] == 1) ? wallColor : spaceColor;
                    Vector3 position = new Vector3((-width / 2) + x + 0.5f, (-height / 2) + y + 0.5f);
                    Gizmos.DrawCube(position, Vector3.one);
                }
            }
        }
        */
    }
}

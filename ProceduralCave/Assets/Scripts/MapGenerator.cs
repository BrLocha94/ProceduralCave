using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
    public bool aplyRoomConection;

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

        List<List<Coordinate>> roomRegions = GetRegions(0);

        List<Room> validRooms = new List<Room>();

        foreach (List<Coordinate> room in roomRegions)
        {
            if (room.Count < roomRegionTolerence && aplyRoomRegionProcess)
            {
                foreach (Coordinate tile in room)
                {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }
            else
                validRooms.Add(new Room(room, map));
        }

        validRooms.Sort();
        validRooms[0].isMain = true;
        validRooms[0].isAcessibleFromMain = true;

        if(aplyRoomConection)
            ConnectClosestRooms(validRooms);
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceConnectionFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if(forceConnectionFromMainRoom == true)
        {
            foreach(Room room in allRooms)
            {
                if (room.isAcessibleFromMain == true)
                    roomListB.Add(room);
                else
                    roomListA.Add(room);
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coordinate bestTileA = new Coordinate();
        Coordinate bestTileB = new Coordinate();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (forceConnectionFromMainRoom == false)
            {
                possibleConnectionFound = false;
                if(roomA.connectedRooms.Count > 0)
                    continue;
            }

            foreach(Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConected(roomB)) continue;

                for(int i = 0; i < roomA.edgeTiles.Count; i++)
                {
                    for(int j = 0; j < roomB.edgeTiles.Count; j++)
                    {
                        Coordinate tileA = roomA.edgeTiles[i];
                        Coordinate tileB = roomB.edgeTiles[j];

                        int distance = (int)Mathf.Pow(tileA.tileX - tileB.tileX, 2) + (int)Mathf.Pow(tileA.tileY - tileB.tileY, 2);

                        if(distance < bestDistance || possibleConnectionFound == false)
                        {
                            bestDistance = distance;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if(possibleConnectionFound == true && forceConnectionFromMainRoom == false)
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
        }

        if (possibleConnectionFound == true && forceConnectionFromMainRoom == true)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (forceConnectionFromMainRoom == false)
            ConnectClosestRooms(allRooms, true);
    }

    void CreatePassage(Room roomA, Room roomB, Coordinate tileA, Coordinate tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        //Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 100);

        List<Coordinate> line = GetLine(tileA, tileB);

        foreach(Coordinate coordinate in line)
        {
            DrawCircle(coordinate, 2);
        }
    }

    void DrawCircle(Coordinate coord, int radio)
    {
        for (int x = -radio; x <= radio; x++)
        {
            for (int y = -radio; y <= radio; y++)
            {
                if (x * x + y * y <= radio * radio)
                {
                    int drawX = coord.tileX + x;
                    int drawY = coord.tileY + y;
                    if (IsInMapRange(drawX, drawY))
                        map[drawX, drawY] = 0;
                }
            }
        }
    }

    List<Coordinate> GetLine(Coordinate from, Coordinate to)
    {
        List<Coordinate> line = new List<Coordinate>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coordinate(x, y));

            if (inverted)
                y += step;
            else
                x += step;

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                    x += gradientStep;
                else
                    y += gradientStep;

                gradientAccumulation -= longest;
            }
        }
        return line;
    }

    Vector3 CoordToWorldPoint(Coordinate tile)
    {
        return new Vector3(-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY);
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

    class Room : IComparable<Room>
    {
        public List<Coordinate> tiles;
        public List<Coordinate> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;

        public bool isAcessibleFromMain = false;
        public bool isMain = false;

        public Room()
        {

        }

        public Room(List<Coordinate> roomTiles, int [,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;

            edgeTiles = new List<Coordinate>();
            connectedRooms = new List<Room>();

            foreach(Coordinate tile in tiles)
            {
                for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if(x == tile.tileX || y == tile.tileY)
                        {
                            if (map[x, y] == 1)
                                edgeTiles.Add(tile);
                        }
                    }
                }
            }
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }

        public void SetAcessibleFromMain()
        {
            if(isAcessibleFromMain == false)
            {
                isAcessibleFromMain = true;
                foreach (Room room in connectedRooms)
                {
                    room.SetAcessibleFromMain();
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isAcessibleFromMain)
                roomB.SetAcessibleFromMain();
            else if (roomB.isAcessibleFromMain)
                roomA.SetAcessibleFromMain();

            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomB);
        }

        public bool IsConected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
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

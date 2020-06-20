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

    [Header("Map dimensions")]
    public int width;
    public int height;

    [Header("Colors to paint gizmos")]
    public Color wallColor;
    public Color spaceColor;

    private int[,] map;

    bool canDrawGizmos = false;

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
        canDrawGizmos = false;

        map = new int[width, height];

        FillMap();

        for (int i = 0; i < smoothProcess; i++)
        {
            SmoothMap();
        }

        canDrawGizmos = true;
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
                if (i >= 0 && i < width && j >= 0 && j < height)
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

    private void OnDrawGizmos()
    {
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
    }
}

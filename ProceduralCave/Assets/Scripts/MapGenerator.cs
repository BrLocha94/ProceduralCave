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

    [Header("Map dimensions")]
    public int width;
    public int height;

    [Header("Colors to paint gizmos")]
    public Color wallColor;
    public Color spaceColor;

    private int[,] map;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        map = new int[width, height];

        FillMap();
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
                map[x, y] = (pseudoRandom.Next(0, 100) < fillPercentage) ? 1 : 0;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (map != null)
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

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Room IDs start at 10
public enum TileType
{
    Nothing = 0,
    Hallway = 1,
    Wall = 2,
    Door = 3
}

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField]
    [Range(10, 1000)]
    int RoomCount = 300;

    [SerializeField]
    [Range(1, 500)]
    int Radius = 50;

    [SerializeField]
    [Range(0, 2)]
    float MainRoomFrequency = 1f;

    [SerializeField]
    [Range(0, 1)]
    float RoomConnectionFrequency = 0.15f;

    int[,] Grid;
    GameObject DungeonMapTexture;

    RoomGenerator RoomGenerator;

    List<int> PrimaryRoomIDs;
    List<int> SecondaryRoomIDs;
    List<Room> Rooms;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        //Visual representation of the map data
        DungeonMapTexture = transform.Find("DungeonMapTexture").gameObject;

        RoomGenerator = transform.Find("RoomGenerator").GetComponent<RoomGenerator>();
        RoomGenerator.OnRoomsGenerated += RoomGenerator_OnRoomsGenerated;

        //Generates rooms and room connections
        RoomGenerator.Generate(RoomCount, Radius, MainRoomFrequency, RoomConnectionFrequency);
    }

    void RoomGenerator_OnRoomsGenerated()
    {
        //Create a copy of all Active rooms
        Rooms = RoomGenerator.Rooms;

        //Convert rooms into a grid of integers
        CreateGrid();
        AddWalls();

        //Draw a texture representation of our dungeon data
        DungeonMapTexture.GetComponent<MeshRenderer>().material.mainTexture = CreateMapTexture();
        DungeonMapTexture.transform.localScale = new Vector2(Grid.GetLength(0), Grid.GetLength(1));

        //RoomGenerator.ClearData ();
    }

    void Update()
    {
        //Generate a new dungeon
        if (Input.GetKeyDown(KeyCode.Space))
            SceneManager.LoadScene("main");
    }

    void CreateGrid()
    {
        PrimaryRoomIDs = new List<int>();
        SecondaryRoomIDs = new List<int>();

        //Get our boundaries and list of primary and secondary room IDs
        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].IsVisible)
            {
                if (Rooms[n].IsMainRoom)
                {
                    PrimaryRoomIDs.Add(Rooms[n].ID);
                }
                else
                {
                    SecondaryRoomIDs.Add(Rooms[n].ID);
                }
            }
        }

        //Add padding for walls
        int width = (int)(RoomGenerator.XMax - RoomGenerator.XMin) + 2;
        int height = (int)(RoomGenerator.YMax - RoomGenerator.YMin) + 2;

        Grid = new int[width, height];

        //Create initial grid with room IDs
        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].IsVisible)
            {
                int startx = (int)Rooms[n].TopLeft.x - (int)RoomGenerator.XMin;
                int starty = (int)Rooms[n].BottomRight.y - (int)RoomGenerator.YMin;
                int endx = startx + (int)Rooms[n].transform.localScale.x;
                int endy = starty + (int)Rooms[n].transform.localScale.y;

                for (int x = startx; x < endx; x++)
                {
                    for (int y = starty; y < endy; y++)
                    {
                        Grid[x + 1, y + 1] = Rooms[n].ID;
                    }
                }
            }
        }

        //Complete missing sections of map based on LineCasts created in Room Generator
        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].IsMainRoom)
            {
                for (int m = 0; m < Rooms[n].Connections.Count; m++)
                {

                    Vector2 p0 = Rooms[n].Connections[m].Line1.p0.Value;
                    Vector2 p1 = Rooms[n].Connections[m].Line1.p1.Value;
                    Vector2 p2 = Rooms[n].Connections[m].Line2.p0.Value;
                    Vector2 p3 = Rooms[n].Connections[m].Line2.p1.Value;

                    //flip values if line is going in opposite direction
                    if ((int)p0.x > (int)p1.x || (int)p0.y > (int)p1.y)
                    {
                        p0 = Rooms[n].Connections[m].Line1.p1.Value;
                        p1 = Rooms[n].Connections[m].Line1.p0.Value;
                    }
                    if ((int)p2.x > (int)p3.x || (int)p2.y > (int)p3.y)
                    {
                        p3 = Rooms[n].Connections[m].Line2.p0.Value;
                        p2 = Rooms[n].Connections[m].Line2.p1.Value;
                    }

                    //Adjust lines to grid coordinates
                    p0 = new Vector2(p0.x - RoomGenerator.XMin, p0.y - RoomGenerator.YMin);
                    p1 = new Vector2(p1.x - RoomGenerator.XMin, p1.y - RoomGenerator.YMin);
                    p2 = new Vector2(p2.x - RoomGenerator.XMin, p2.y - RoomGenerator.YMin);
                    p3 = new Vector2(p3.x - RoomGenerator.XMin, p3.y - RoomGenerator.YMin);

                    //Create the hallways in our grid
                    AddHallway(p0, p1);
                    AddHallway(p2, p3);
                }
            }
        }
    }

    void AddHallway(Vector2 p0, Vector2 p1)
    {
        //Vertical direction
        if ((int)p1.x == (int)p0.x)
        {
            for (int y = (int)p0.y; y < (int)p1.y; y++)
            {

                //if the tile is nothing then make it a hallway
                if (Grid[(int)p1.x, y] == (int)TileType.Nothing)
                {
                    Grid[(int)p1.x, y] = (int)TileType.Hallway;
                }

                //make hallways 3 units wide
                if ((int)p1.x < Grid.GetLength(0) - 2)
                {
                    if (Grid[(int)p1.x + 1, y] == (int)TileType.Nothing)
                    {
                        Grid[(int)p1.x + 1, y] = (int)TileType.Hallway;
                    }
                    if (Grid[(int)p1.x + 2, y] == (int)TileType.Nothing)
                    {
                        Grid[(int)p1.x + 2, y] = (int)TileType.Hallway;
                    }
                }
                else
                {
                    if (Grid[(int)p1.x - 1, y] == (int)TileType.Nothing)
                    {
                        Grid[(int)p1.x - 1, y] = (int)TileType.Hallway;
                    }
                    if (Grid[(int)p1.x - 2, y] == (int)TileType.Nothing)
                    {
                        Grid[(int)p1.x - 2, y] = (int)TileType.Hallway;
                    }
                }
            }
        }

        //Horizontal direction
        else if ((int)p1.y == (int)p0.y)
        {
            for (int x = (int)p0.x; x < (int)p1.x; x++)
            {

                //if the tile is nothing then make it a hallway
                if (Grid[x, (int)p1.y] == (int)TileType.Nothing)
                {
                    Grid[x, (int)p1.y] = (int)TileType.Hallway;
                }
                //make hallways 3 units wide
                if ((int)p1.y < Grid.GetLength(1) - 2)
                {
                    if (Grid[x, (int)p1.y + 1] == (int)TileType.Nothing)
                    {
                        Grid[x, (int)p1.y + 1] = (int)TileType.Hallway;
                    }
                    if (Grid[x, (int)p1.y + 2] == (int)TileType.Nothing)
                    {
                        Grid[x, (int)p1.y + 2] = (int)TileType.Hallway;
                    }

                }
                else
                {
                    if (Grid[x, (int)p1.y - 1] == (int)TileType.Nothing)
                    {
                        Grid[x, (int)p1.y - 1] = (int)TileType.Hallway;
                    }
                    if (Grid[x, (int)p1.y - 2] == (int)TileType.Nothing)
                    {
                        Grid[x, (int)p1.y - 2] = (int)TileType.Hallway;
                    }
                }
            }
        }
    }

    void AddWalls()
    {
        //Create a copy of current grid
        int[,] gridCopy = new int[Grid.GetLength(0), Grid.GetLength(1)];
        for (int x = 0; x < Grid.GetLength(0); x++)
        {
            for (int y = 0; y < Grid.GetLength(1); y++)
            {
                gridCopy[x, y] = Grid[x, y];
            }
        }

        //Process
        for (int x = 0; x < Grid.GetLength(0); x++)
        {
            for (int y = 0; y < Grid.GetLength(1); y++)
            {
                int val = gridCopy[x, y];

                //walls for primary rooms
                if (PrimaryRoomIDs.Contains(val))
                {
                    if (x > 0 && gridCopy[x - 1, y] != val && gridCopy[x - 1, y] != (int)TileType.Wall)
                    {
                        Grid[x - 1, y] = (int)TileType.Wall;
                    }
                    else if (x < gridCopy.GetLength(0) - 1 && gridCopy[x + 1, y] != val && gridCopy[x + 1, y] != (int)TileType.Wall)
                    {
                        Grid[x + 1, y] = (int)TileType.Wall;
                    }

                    if (y > 0 && gridCopy[x, y - 1] != val && gridCopy[x, y - 1] != (int)TileType.Wall)
                    {
                        Grid[x, y - 1] = (int)TileType.Wall;
                    }
                    else if (y < Grid.GetLength(1) - 1 && gridCopy[x, y + 1] != val && gridCopy[x, y + 1] != (int)TileType.Wall)
                    {
                        Grid[x, y + 1] = (int)TileType.Wall;
                    }
                }

                //Outside borders
                if (val == 0)
                {
                    if (x > 0 && gridCopy[x - 1, y] != (int)TileType.Nothing && gridCopy[x - 1, y] != (int)TileType.Wall)
                    {
                        Grid[x, y] = (int)TileType.Wall;
                    }
                    else if (x < Grid.GetLength(0) - 1 && gridCopy[x + 1, y] != (int)TileType.Nothing && gridCopy[x + 1, y] != (int)TileType.Wall)
                    {
                        Grid[x, y] = (int)TileType.Wall;
                    }

                    if (y > 0 && gridCopy[x, y - 1] != (int)TileType.Nothing && gridCopy[x, y - 1] != (int)TileType.Wall)
                    {
                        Grid[x, y] = (int)TileType.Wall;
                    }
                    else if (y < Grid.GetLength(1) - 1 && gridCopy[x, y + 1] != (int)TileType.Nothing && gridCopy[x, y + 1] != (int)TileType.Wall)
                    {
                        Grid[x, y] = (int)TileType.Wall;
                    }
                }
            }
        }

        //update grid copy
        for (int x = 0; x < Grid.GetLength(0); x++)
        {
            for (int y = 0; y < Grid.GetLength(1); y++)
            {
                gridCopy[x, y] = Grid[x, y];
            }
        }

        //Add doors based on connecting lines
        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].IsMainRoom)
            {

                for (int m = 0; m < Rooms[n].Connections.Count; m++)
                {

                    //Line 1
                    Vector2 p0 = Rooms[n].Connections[m].Line1.p0.Value;
                    Vector2 p1 = Rooms[n].Connections[m].Line1.p1.Value;

                    if ((int)p0.x > (int)p1.x || (int)p0.y > (int)p1.y)
                    {
                        p1 = Rooms[n].Connections[m].Line1.p0.Value;
                        p0 = Rooms[n].Connections[m].Line1.p1.Value;
                    }
                    p0 = new Vector2(p0.x - RoomGenerator.XMin, p0.y - RoomGenerator.YMin);
                    p1 = new Vector2(p1.x - RoomGenerator.XMin, p1.y - RoomGenerator.YMin);

                    AddDoor(p0, p1);

                    //Line 2
                    p0 = Rooms[n].Connections[m].Line2.p0.Value;
                    p1 = Rooms[n].Connections[m].Line2.p1.Value;

                    if ((int)p0.x > (int)p1.x || (int)p0.y > (int)p1.y)
                    {
                        p1 = Rooms[n].Connections[m].Line2.p0.Value;
                        p0 = Rooms[n].Connections[m].Line2.p1.Value;
                    }
                    p0 = new Vector2(p0.x - RoomGenerator.XMin, p0.y - RoomGenerator.YMin);
                    p1 = new Vector2(p1.x - RoomGenerator.XMin, p1.y - RoomGenerator.YMin);

                    AddDoor(p0, p1);
                }
            }
        }
    }

    void AddDoor(Vector2 p0, Vector2 p1)
    {

        //Vertical direction
        if ((int)p1.x == (int)p0.x)
        {
            for (int y = (int)p0.y; y < (int)p1.y; y++)
            {

                if ((int)p1.x < Grid.GetLength(0) - 2)
                {
                    if ((Grid[(int)p1.x, y] == (int)TileType.Wall) && (Grid[(int)p1.x + 1, y] == (int)TileType.Wall) && (Grid[(int)p1.x + 2, y] == (int)TileType.Wall))
                    {
                        Grid[(int)p1.x, y] = (int)TileType.Door;
                        Grid[(int)p1.x + 1, y] = (int)TileType.Door;
                        Grid[(int)p1.x + 2, y] = (int)TileType.Door;
                    }
                }
                else
                {
                    if ((Grid[(int)p1.x, y] == (int)TileType.Wall) && (Grid[(int)p1.x - 1, y] == (int)TileType.Wall) && (Grid[(int)p1.x - 2, y] == (int)TileType.Wall))
                    {
                        Grid[(int)p1.x, y] = (int)TileType.Door;
                        Grid[(int)p1.x - 1, y] = (int)TileType.Door;
                        Grid[(int)p1.x - 2, y] = (int)TileType.Door;
                    }
                }
            }
        }

        //Horizontal direction
        else if ((int)p1.y == (int)p0.y)
        {
            for (int x = (int)p0.x; x < (int)p1.x; x++)
            {

                if ((int)p1.y < Grid.GetLength(1) - 2)
                {

                    if ((Grid[x, (int)p1.y] == (int)TileType.Wall) && (Grid[x, (int)p1.y + 1] == (int)TileType.Wall) && (Grid[x, (int)p1.y + 2] == (int)TileType.Wall))
                    {
                        Grid[x, (int)p1.y] = (int)TileType.Door;
                        Grid[x, (int)p1.y + 1] = (int)TileType.Door;
                        Grid[x, (int)p1.y + 2] = (int)TileType.Door;
                    }

                }
                else
                {
                    if ((Grid[x, (int)p1.y] == (int)TileType.Wall) && (Grid[x, (int)p1.y - 1] == (int)TileType.Wall) && (Grid[x, (int)p1.y - 2] == (int)TileType.Wall))
                    {
                        Grid[x, (int)p1.y] = (int)TileType.Door;
                        Grid[x, (int)p1.y - 1] = (int)TileType.Door;
                        Grid[x, (int)p1.y - 2] = (int)TileType.Door;
                    }
                }
            }
        }
    }

    Texture2D CreateMapTexture()
    {
        int width = Grid.GetLength(0);
        int height = Grid.GetLength(1);

        var texture = new Texture2D(width, height);
        var pixels = new Color[width * height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                Color color = Color.white;

                if (PrimaryRoomIDs.Contains(Grid[x, y]))
                {
                    color = new Color(255 / 255f, 150 / 255f, 50 / 255f);
                }
                else if (SecondaryRoomIDs.Contains(Grid[x, y]))
                {
                    color = new Color(255 / 255f, 215 / 255f, 70 / 255f);
                }
                else if (Grid[x, y] == 1)
                {
                    color = Color.red;
                }
                else if (Grid[x, y] == 2)
                {
                    color = Color.black;
                }
                else if (Grid[x, y] == 3)
                {
                    color = Color.magenta;
                }

                pixels[x + y * width] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return texture;
    }
}
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    List<int> PrimaryRoomIDs;
    List<int> SecondaryRoomIDs;

    RoomGenerator RoomGenerator;
    GameObject DungeonMapTexture;
    List<Room> Rooms;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        DungeonMapTexture = transform.Find("DungeonMapTexture").gameObject;

        RoomGenerator = transform.Find("RoomGenerator").GetComponent<RoomGenerator>();
        RoomGenerator.OnRoomsGenerated += RoomGenerator_OnRoomsGenerated;

        RoomGenerator.Generate(RoomCount, Radius, MainRoomFrequency, RoomConnectionFrequency);
    }

    void RoomGenerator_OnRoomsGenerated()
    {
        Rooms = RoomGenerator.Rooms;
        CreateGrid();
        AddWalls();

        DungeonMapTexture.GetComponent<MeshRenderer>().material.mainTexture = CreateMapTexture();
        DungeonMapTexture.transform.localScale = new Vector2(Grid.GetLength(0), Grid.GetLength(1));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            SceneManager.LoadScene("main");
    }

    public void CreateGrid()
    {
        PrimaryRoomIDs = new List<int>();
        SecondaryRoomIDs = new List<int>();

        float minx = float.MaxValue;
        float maxx = float.MinValue;
        float miny = float.MaxValue;
        float maxy = float.MinValue;

        //Get our boundaries and list of primary and secondary room IDs
        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].IsVisible)
            {

                minx = Mathf.Min(minx, Rooms[n].TopLeft.x);
                maxx = Mathf.Max(maxx, Rooms[n].BottomRight.x);
                miny = Mathf.Min(miny, Rooms[n].BottomRight.y);
                maxy = Mathf.Max(maxy, Rooms[n].TopLeft.y);

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
        int width = (int)(maxx - minx) + 2;
        int height = (int)(maxy - miny) + 2;

        Grid = new int[width, height];

        //Create initial grid with room IDs
        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].IsVisible)
            {

                int startx = (int)Rooms[n].TopLeft.x - (int)minx;
                int starty = (int)Rooms[n].BottomRight.y - (int)miny;
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

        //Complete mission sections of map based on LineCasts created in Room Generator
        for (int n = 0; n < RoomGenerator.Lines.Count; n++)
        {

            Vector2 p0 = new Vector2(RoomGenerator.Lines[n].p0.Value.x - minx, RoomGenerator.Lines[n].p0.Value.y - miny);
            Vector2 p1 = new Vector2(RoomGenerator.Lines[n].p1.Value.x - minx, RoomGenerator.Lines[n].p1.Value.y - miny);

            if (RoomGenerator.Lines[n].p0.Value.x > RoomGenerator.Lines[n].p1.Value.x || RoomGenerator.Lines[n].p0.Value.y > RoomGenerator.Lines[n].p1.Value.y)
            {
                p1 = new Vector2(RoomGenerator.Lines[n].p0.Value.x - minx, RoomGenerator.Lines[n].p0.Value.y - miny);
                p0 = new Vector2(RoomGenerator.Lines[n].p1.Value.x - minx, RoomGenerator.Lines[n].p1.Value.y - miny);
            }

            //Vertical direction
            if (p1.x == p0.x)
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
            if (p1.y == p0.y)
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
    }

    public void AddWalls()
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

        //Secondary run to open up doorways
        //TODO: Only add 1 door per room connection
        for (int x = 0; x < Grid.GetLength(0); x++)
        {
            for (int y = 0; y < Grid.GetLength(1); y++)
            {

                int val = Grid[x, y];

                if (val == (int)TileType.Wall)
                {

                    if (x > 0 && x < gridCopy.GetLength(0) - 1)
                    {

                        //Check if wall is between two rooms
                        if (Grid[x - 1, y] != (int)TileType.Wall && Grid[x + 1, y] != (int)TileType.Wall)
                        {
                            if (PrimaryRoomIDs.Contains(Grid[x - 1, y]) || PrimaryRoomIDs.Contains(Grid[x + 1, y]))
                            {
                                if (Grid[x - 1, y] != 0 && Grid[x + 1, y] != 0)
                                    Grid[x, y] = (int)TileType.Door;
                            }
                        }
                        else if (Grid[x - 1, y] == (int)TileType.Wall)
                        {
                            if (x > 2 && Grid[x - 2, y] != (int)TileType.Wall && Grid[x + 1, y] != 0 && Grid[x + 1, y] != (int)TileType.Wall)
                            {
                                if (PrimaryRoomIDs.Contains(Grid[x - 2, y]))
                                {
                                    Grid[x, y] = (int)TileType.Door;
                                }
                            }
                        }
                        else if (Grid[x + 1, y] == (int)TileType.Wall)
                        {
                            if (x < gridCopy.GetLength(0) - 2 && Grid[x + 2, y] != (int)TileType.Wall && Grid[x - 1, y] != 0 && Grid[x - 1, y] != (int)TileType.Wall)
                            {
                                if (PrimaryRoomIDs.Contains(Grid[x + 2, y]))
                                {
                                    Grid[x, y] = (int)TileType.Door;
                                }
                            }
                        }

                    }

                    if (y > 0 && y < Grid.GetLength(1) - 1)
                    {

                        if (Grid[x, y - 1] != (int)TileType.Wall && Grid[x, y + 1] != (int)TileType.Wall)
                        {
                            if (PrimaryRoomIDs.Contains(Grid[x, y - 1]) || PrimaryRoomIDs.Contains(Grid[x, y + 1]))
                            {
                                if (Grid[x, y - 1] != 0 && Grid[x, y + 1] != (int)TileType.Nothing)
                                    Grid[x, y] = (int)TileType.Door;
                            }
                        }
                        else if (Grid[x, y - 1] == (int)TileType.Wall)
                        {
                            if (y > 2 && Grid[x, y - 2] != (int)TileType.Wall && Grid[x, y + 1] != 0 && Grid[x, y + 1] != (int)TileType.Wall)
                            {
                                if (PrimaryRoomIDs.Contains(Grid[x, y - 2]))
                                {
                                    Grid[x, y] = (int)TileType.Door;
                                }
                            }
                        }
                        else if (Grid[x, y + 1] == (int)TileType.Wall)
                        {
                            if (y < gridCopy.GetLength(1) - 2 && Grid[x, y + 2] != (int)TileType.Wall && Grid[x, y - 1] != 0 && Grid[x, y - 1] != (int)TileType.Wall)
                            {
                                if (PrimaryRoomIDs.Contains(Grid[x, y + 2]))
                                {
                                    Grid[x, y] = (int)TileType.Door;
                                }
                            }
                        }
                    }
                }


                //Fill in miossing corner tiles
                if (val == (int)TileType.Nothing)
                {
                    if (x < Grid.GetLength(0) - 1 && y < Grid.GetLength(1) - 1)
                    {
                        if (gridCopy[x + 1, y] == (int)TileType.Wall && gridCopy[x, y + 1] == (int)TileType.Wall)
                        {
                            Grid[x, y] = (int)TileType.Wall;
                        }
                    }
                    if (x > 0 && y > 0)
                    {
                        if (gridCopy[x - 1, y] == (int)TileType.Wall && gridCopy[x, y - 1] == (int)TileType.Wall)
                        {
                            Grid[x, y] = (int)TileType.Wall;
                        }
                    }
                    if (x > 0 && y < Grid.GetLength(1) - 1)
                    {
                        if (gridCopy[x - 1, y] == (int)TileType.Wall && gridCopy[x, y + 1] == (int)TileType.Wall)
                        {
                            Grid[x, y] = (int)TileType.Wall;
                        }
                    }
                    if (x < Grid.GetLength(0) - 1 && y > 0)
                    {
                        if (gridCopy[x + 1, y] == (int)TileType.Wall && gridCopy[x, y - 1] == (int)TileType.Wall)
                        {
                            Grid[x, y] = (int)TileType.Wall;
                        }
                    }
                }
            }
        }

        //Third run to adjust door sizes
        for (int x = 0; x < Grid.GetLength(0); x++)
        {
            for (int y = 0; y < Grid.GetLength(1); y++)
            {

                int val = Grid[x, y];

                if (val == 3)
                {

                    int startx = x;
                    int starty = y;
                    int endx = x;
                    int endy = y;

                    while (startx > 0 && Grid[startx, y] == (int)TileType.Door)
                    {
                        startx--;
                    }
                    while (starty > 0 && Grid[x, starty] == (int)TileType.Door)
                    {
                        starty--;
                    }
                    while (endx < Grid.GetLength(0) && Grid[endx, y] == (int)TileType.Door)
                    {
                        endx++;
                    }
                    while (endy < Grid.GetLength(1) && Grid[x, endy] == (int)TileType.Door)
                    {
                        endy++;
                    }

                    startx++;
                    starty++;

                    int height = endy - starty;
                    int width = endx - startx;

                    if (height > width)
                    {

                        if (height > 3)
                        {

                            int remaining = height - 3;
                            int half = remaining / 2;
                            int count = 0;

                            for (int y1 = starty; y1 < endy; y1++)
                            {
                                if (half > 0)
                                {
                                    Grid[x, y1] = (int)TileType.Wall;
                                    half--;
                                }
                                else if (count < 3)
                                {
                                    count++;
                                }
                                else
                                {
                                    Grid[x, y1] = (int)TileType.Wall;
                                }
                            }
                        }


                    }
                    else
                    {

                        if (width > 3)
                        {

                            int remaining = width - 3;
                            int half = remaining / 2;
                            int count = 0;

                            for (int x1 = startx; x1 < endx; x1++)
                            {
                                if (half > 0)
                                {
                                    Grid[x1, y] = (int)TileType.Wall;
                                    half--;
                                }
                                else if (count < 3)
                                {
                                    count++;
                                }
                                else
                                {
                                    Grid[x1, y] = (int)TileType.Wall;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private Texture2D CreateMapTexture()
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



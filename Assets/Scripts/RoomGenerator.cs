using UnityEngine;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;

public class RoomGenerator : MonoBehaviour
{
    public delegate void RoomsGeneratedHandler();
    public event RoomsGeneratedHandler OnRoomsGenerated;

    GameObject RoomsContainer;
    GameObject LinesContainer;

    public List<Room> Rooms
    {
        get;
        private set;
    }
    Dictionary<Vector2, Room> MainRooms;

    bool Done = false;
    int[] Distribution = new int[] { 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 7, 8, 10, 12, 14 };

    private List<Vector2> Points = new List<Vector2>();
    private List<LineSegment> Edges = null;
    private List<LineSegment> SpanningTree;
    private List<LineSegment> DelaunayTriangulation;

    private float RoomConnectionFrequency = 0.15f;
    public List<LineSegment> Lines;

    public void Generate(int roomCount, int radius, float mainRoomFrequency, float roomConnectionFrequency)
    {
        RoomsContainer = new GameObject("Rooms");
        LinesContainer = new GameObject("Lines");

        RoomConnectionFrequency = roomConnectionFrequency;

        int totalWidth = 0, totalHeight = 0;
        Rooms = new List<Room>();

        //Initialize our rooms
        for (int n = 0; n < roomCount; n++)
        {
            Room room = (GameObject.Instantiate(Resources.Load("Room") as GameObject)).GetComponent<Room>();
            room.transform.parent = RoomsContainer.transform;

            int width = Distribution[Random.Range(0, Distribution.Length)];
            int height = Distribution[Random.Range(0, Distribution.Length)];
            Vector2 position = GetRandomPositionInCircle(radius);

            totalWidth += width;
            totalHeight += height;

            //Start rooms at ID 10
            room.Init(n + 10, position, width, height);
            Rooms.Add(room);
        }

        //find the main rooms
        float widthAvg = totalWidth / roomCount;
        float heightAvg = totalHeight / roomCount;

        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].transform.localScale.x >= (1f + mainRoomFrequency) * widthAvg && Rooms[n].transform.localScale.y >= (1f + mainRoomFrequency) * heightAvg)
            {
                Rooms[n].SetMain();
            }
        }
    }

    private Vector2 GetRandomPositionInCircle(float radius)
    {
        float angle = Random.Range(0f, 1f) * Mathf.PI * 2f;

        float rad = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
        float x = this.transform.localPosition.x + rad * Mathf.Cos(angle);
        float y = this.transform.localPosition.y + rad * Mathf.Sin(angle);

        return new Vector2((int)x, (int)y);
    }

    void Update()
    {

        bool allSleeping = true;
        for (int n = 0; n < Rooms.Count; n++)
        {
            if (!Rooms[n].RigidBody2D.IsSleeping())
            {
                allSleeping = false;
                Rooms[n].SetLocked(false);
            }
            else
            {
                Rooms[n].SetLocked(true);
            }
        }

        //Check if all physics objects are done settling
        if (allSleeping && !Done)
        {
            Done = true;
            Calculate();
        }
    }

    private void GenerateSpanningTree(bool drawLines)
    {
        for (int n = 0; n < SpanningTree.Count; n++)
        {
            if (drawLines)
            {
                GameObject line = GameObject.Instantiate(Resources.Load("Line") as GameObject);
                line.GetComponent<LineRenderer>().SetPosition(0, SpanningTree[n].p0.Value);
                line.GetComponent<LineRenderer>().SetPosition(1, SpanningTree[n].p1.Value);
                line.GetComponent<LineRenderer>().sortingOrder = 100;
                line.GetComponent<LineRenderer>().SetColors(Color.red, Color.red);

                line.transform.parent = LinesContainer.transform;
            }

            if (!MainRooms[SpanningTree[n].p0.Value].Connections.Contains(SpanningTree[n].p1.Value))
                MainRooms[SpanningTree[n].p0.Value].Connections.Add(SpanningTree[n].p1.Value);
        }

        AddExtraConnections(drawLines);
    }

    //In order for our dungeon to look interesting, we will add more connections to our minimum spanning tree
    private void AddExtraConnections(bool drawLines)
    {
        List<int> range = new List<int>();
        for (int n = 0; n < DelaunayTriangulation.Count; n++)
        {
            range.Add(n);
        }

        for (int n = 0; n < (int)(DelaunayTriangulation.Count * RoomConnectionFrequency); n++)
        {
            int idx = Random.Range(0, range.Count);
            int value = range[idx];
            range.RemoveAt(idx);

            if (drawLines)
            {
                GameObject line = GameObject.Instantiate(Resources.Load("Line") as GameObject);
                line.GetComponent<LineRenderer>().SetPosition(0, DelaunayTriangulation[value].p0.Value);
                line.GetComponent<LineRenderer>().SetPosition(1, DelaunayTriangulation[value].p1.Value);
                line.GetComponent<LineRenderer>().sortingOrder = 100;
                line.GetComponent<LineRenderer>().SetColors(Color.blue, Color.blue);

                line.transform.parent = LinesContainer.transform;
            }

            if (!MainRooms[DelaunayTriangulation[n].p0.Value].Connections.Contains(DelaunayTriangulation[n].p1.Value))
                MainRooms[DelaunayTriangulation[n].p0.Value].Connections.Add(DelaunayTriangulation[n].p1.Value);
        }
    }

    private void GenerateRoomConnections(bool drawLines)
    {
        for (int n = 0; n < Rooms.Count; n++)
        {
            Rooms[n].SetVisible(false);
        }

        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].IsMainRoom)
            {
                Rooms[n].SetVisible(true);

                for (int i = 0; i < Rooms[n].Connections.Count; i++)
                {
                    Vector2 p0 = Rooms[n].Center;
                    Vector2 p1 = Rooms[n].Connections[i];

                    float xdiff = Mathf.Abs(p0.x - p1.x);
                    float ydiff = Mathf.Abs(p0.y - p1.y);

                    if (Rooms[n].Center.y < Rooms[n].Connections[i].y)
                    {
                        if (drawLines)
                        {
                            GameObject line = GameObject.Instantiate(Resources.Load("Line") as GameObject);
                            line.GetComponent<LineRenderer>().SetPosition(0, Rooms[n].Center);
                            line.GetComponent<LineRenderer>().SetPosition(1, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y + 3));
                            line.GetComponent<LineRenderer>().sortingOrder = 100;

                            line.transform.parent = LinesContainer.transform;
                        }

                        Lines.Add(new LineSegment(Rooms[n].Center, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y + 3)));
                    }
                    else
                    {
                        if (drawLines)
                        {
                            GameObject line = GameObject.Instantiate(Resources.Load("Line") as GameObject);
                            line.GetComponent<LineRenderer>().SetPosition(0, Rooms[n].Center);
                            line.GetComponent<LineRenderer>().SetPosition(1, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y - 3));
                            line.GetComponent<LineRenderer>().sortingOrder = 100;

                            line.transform.parent = LinesContainer.transform;
                        }

                        Lines.Add(new LineSegment(Rooms[n].Center, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y - 3)));
                    }

                    //Activate all rooms that intersect the lines we create
                    RaycastHit2D[] hit = Physics2D.LinecastAll(Rooms[n].Center, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y));
                    for (int x = 0; x < hit.Length; x++)
                    {
                        hit[x].collider.GetComponent<Room>().SetVisible(true);
                    }

                    if (drawLines)
                    {
                        GameObject line = GameObject.Instantiate(Resources.Load("Line") as GameObject);
                        line.GetComponent<LineRenderer>().SetPosition(0, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y));
                        line.GetComponent<LineRenderer>().SetPosition(1, new Vector2(Rooms[n].Connections[i].x, Rooms[n].Connections[i].y));
                        line.GetComponent<LineRenderer>().sortingOrder = 100;

                        line.transform.parent = LinesContainer.transform;
                    }

                    hit = Physics2D.LinecastAll(new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y), new Vector2(Rooms[n].Connections[i].x, Rooms[n].Connections[i].y));
                    for (int x = 0; x < hit.Length; x++)
                    {
                        hit[x].collider.GetComponent<Room>().SetVisible(true);
                    }

                    Lines.Add(new LineSegment(new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y), new Vector2(Rooms[n].Connections[i].x, Rooms[n].Connections[i].y)));


                    if (xdiff < 5)
                    {
                        if (drawLines)
                        {
                            GameObject line = GameObject.Instantiate(Resources.Load("Line") as GameObject);
                            line.GetComponent<LineRenderer>().SetPosition(0, Rooms[n].Center);
                            line.GetComponent<LineRenderer>().SetPosition(1, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y));
                            line.GetComponent<LineRenderer>().sortingOrder = 100;

                            line.transform.parent = LinesContainer.transform;
                        }

                        hit = Physics2D.LinecastAll(Rooms[n].Center, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y));
                        for (int x = 0; x < hit.Length; x++)
                        {
                            hit[x].collider.GetComponent<Room>().SetVisible(true);
                        }

                        Lines.Add(new LineSegment(Rooms[n].Center, new Vector2(Rooms[n].Center.x, Rooms[n].Connections[i].y)));
                    }


                    if (ydiff < 5)
                    {
                        if (drawLines)
                        {
                            GameObject line = GameObject.Instantiate(Resources.Load("Line") as GameObject);
                            line.GetComponent<LineRenderer>().SetPosition(0, Rooms[n].Center);
                            line.GetComponent<LineRenderer>().SetPosition(1, new Vector2(Rooms[n].Connections[i].x, Rooms[n].Center.y));
                            line.GetComponent<LineRenderer>().sortingOrder = 100;

                            line.transform.parent = LinesContainer.transform;
                        }

                        hit = Physics2D.LinecastAll(Rooms[n].Center, new Vector2(Rooms[n].Connections[i].x, Rooms[n].Center.y));
                        for (int x = 0; x < hit.Length; x++)
                        {
                            hit[x].collider.GetComponent<Room>().SetVisible(true);
                        }

                        Lines.Add(new LineSegment(Rooms[n].Center, new Vector2(Rooms[n].Connections[i].x, Rooms[n].Center.y)));
                    }
                }
            }
        }
    }

    void Calculate()
    {
        List<uint> colors = new List<uint>();
        MainRooms = new Dictionary<Vector2, Room>();

        //Get a point list of all our main rooms
        for (int n = 0; n < Rooms.Count; n++)
        {
            if (Rooms[n].IsMainRoom)
            {
                Points.Add(Rooms[n].Center);
                colors.Add(0);

                if (!MainRooms.ContainsKey(Rooms[n].Center))
                    MainRooms.Add(Rooms[n].Center, Rooms[n]);
            }
        }

        //Calculate min spanning tree
        Voronoi v = new Voronoi(Points, colors, new Rect(0, 0, 50, 50));
        Edges = v.VoronoiDiagram();
        SpanningTree = v.SpanningTree(KruskalType.MINIMUM);
        DelaunayTriangulation = v.DelaunayTriangulation();

        Lines = new List<LineSegment>();

        //Add room connections
        GenerateSpanningTree(true);
        GenerateRoomConnections(false);

        OnRoomsGenerated();
    }

}



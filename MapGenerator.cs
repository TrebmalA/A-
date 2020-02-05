using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapGenerator : MonoBehaviour
{
    //All the dang UI elements
    public Button m_submitButton, m_animateButton , m_mazeButton;
    public InputField sizeField, seedField, obstacleCountField, startXField, startYField, flagXField, flagYField;
    public Text errorText, costText, distanceText, footNoteText;
    public Toggle stepToggle;
    public Dropdown heuristicDropdown;
    public Slider animationSpeedSlider;

    //Transforms and other map related things
    public Transform tilePrefab, obstaclePrefab, startPrefab, flagPrefab, exploredPrefab, considerablePrefab, consideringPrefab;
    private int mapSize;
    private int mapSeed;
    private int startXPos;
    private int startYPos;
    private int flagXPos;
    private int flagYPos;
    public static string mapName = "Generated Map";
    Transform mapHolder;
    public int heuristicMethod;

    private int obstacleCount;

    //Storing Tiles
    public Tile[,] tileArr;
    List<Coord> allTileCoords;
    Queue<Coord> shuffledTileCoords;
    public Tile startTile;
    public Tile flagTile;

    //Scale the square!
    private float scale;

    [Range(.01f, .5f)]
    private float delay;

    //init
    private void Start()
    {
        //stepToggle.isOn = false;
        m_submitButton.onClick.AddListener(HandleMapOptions);
        m_animateButton.onClick.AddListener(setUpAnimation);
        m_mazeButton.onClick.AddListener(HandleMazeOptions);
    }

    //Sets up a few things. Code extrapulation haha!
    private void setUpAnimation()
    {
        delay = animationSpeedSlider.value;
        heuristicMethod = heuristicDropdown.value;
        StartCoroutine("PlayAnimation");
    }

    //A* algoithm in all it's glory, also does all the animating, like backtracking and shit
    IEnumerator PlayAnimation()
    {
        //A* ALGORITHM
        PriorityQueue<Tile> frontier;
        frontier = new PriorityQueue<Tile>();
        frontier.Enqueue(startTile);
        Tile curr = startTile;

        while (frontier.info.Count != 0)
        {
            //Lowest F-Score will alwyas be the top
            curr = frontier.Dequeue();

            int currx = curr.x;
            int curry = curr.y;

            //CURR IS CONSIDERING
            spawnTile(currx, curry, consideringPrefab, Tile.type.CLOSED);
            yield return new WaitForSeconds(delay);

            PriorityQueue<Tile> neighbors = curr.getNeighbors();
            Tile currNeighbor;
            Debug.Log(neighbors.info.Count);
            while (neighbors.info.Count != 0)
            {
                    currNeighbor = neighbors.Dequeue();
                    int currNx = currNeighbor.x;
                    int currNy = currNeighbor.y;
                    if (currNeighbor.tileType == Tile.type.CLOSED || currNeighbor.tileType == Tile.type.OBSTACLE || currNeighbor.tileType == Tile.type.EXPLORED)
                    {
                    }
                    else
                    {
                    Debug.Log(" (" + currNx + ", " + currNy + ")");
                    Debug.Log(currNeighbor.tileType);
                    if (currNeighbor.tileType != Tile.type.OPEN)
                        {
                            currNeighbor.tileType = Tile.type.OPEN;
                            currNeighbor.parent = curr;
                            currNeighbor.assignCosts();
                            frontier.Enqueue(currNeighbor);
                        }
                        else
                        {
                            int fromCurrToCurrNeighbor = (int)Mathf.Floor(10 * Mathf.Sqrt(((currx - currNx) * (currx - currNx)) + ((curry - currNy) * (curry - currNy))));
                            int new_cost = curr.G_COST + fromCurrToCurrNeighbor;
                            if (currNeighbor.G_COST > new_cost)
                            {
                                currNeighbor.parent = curr;
                                currNeighbor.G_COST = new_cost;
                                currNeighbor.F_COST = currNeighbor.G_COST + currNeighbor.H_COST;
                            }
                        }

                    Debug.Log("H: " + currNeighbor.H_COST);
                    Debug.Log("G: " + currNeighbor.G_COST);
                    Debug.Log("F: " + currNeighbor.F_COST);
                    Tile newTile = spawnTile(currNx, currNy, considerablePrefab, Tile.type.OPEN);
                    newTile.H_COST = currNeighbor.H_COST;
                    newTile.F_COST = currNeighbor.F_COST;
                    newTile.G_COST = currNeighbor.G_COST;
                    costText.text = "H: " + newTile.H_COST + "  G: " + newTile.G_COST + "  F: " + newTile.F_COST;
                    yield return new WaitForSeconds(delay);
                    }
                }

            //CURR BECOMES EXPLORED
            Tile createdTile = spawnTile(currx, curry, exploredPrefab, Tile.type.EXPLORED);
            createdTile.H_COST = curr.H_COST;
            createdTile.F_COST = curr.F_COST;
            createdTile.G_COST = curr.G_COST;
            if (currx == flagTile.x && curry == flagTile.y)
            {
                flagTile = createdTile;
                flagTile.parent = curr.parent;
                break;
            }
            yield return new WaitForSeconds(delay);
        }

        //BACKTRACK FROM FLAG TO START AND COLOR PATH RED
        startTile.parent = null;
        //if (curr != null && (curr.x == flagTile.x && curr.y == flagTile.y))
        if (curr != null && flagTile.tileType == Tile.type.EXPLORED)
        {
            curr = flagTile;
            int distanceTraveled = curr.G_COST;
            int steps = 0;
            while (curr.parent != null)
            {
                steps++;
                //COLOR PATH TILES RED
                spawnTile(curr.x, curr.y, flagPrefab, Tile.type.FLAG);
                yield return new WaitForSeconds(delay);
                curr = curr.parent;
            }

            distanceText.text = "Distance: " + distanceTraveled + "    Steps: " + steps;
            footNoteText.text = "*Note that these show the last calculated values which do not always correspond to the flag*";
        }
    }

    //Spawns a tile and destroys the predecessor
    private Tile spawnTile(int x, int y, Transform prefab, Tile.type type)
    {
        Vector3 tilePos = CoordToVector(x, y);
        Transform tilePrefab = Instantiate(prefab, tilePos, Quaternion.Euler(0, 0, 0)) as Transform;
        tilePrefab.parent = mapHolder;
        tilePrefab.transform.localScale *= scale;
        Tile newTile = new Tile(this, tilePrefab, x, y);
        newTile.tileType = type;
        DestroyImmediate(tileArr[x, y].transform.gameObject);
        tileArr[x, y] = newTile;
        return newTile;
    }

    //Spawns a tile and maybe destroys the predecessor
    private Tile spawnTile(int x, int y, Transform prefab, Tile.type type, bool destroy)
    {
        Vector3 tilePos = CoordToVector(x, y);
        Transform tilePrefab = Instantiate(prefab, tilePos, Quaternion.Euler(0, 0, 0)) as Transform;
        tilePrefab.parent = mapHolder;
        tilePrefab.transform.localScale *= scale;
        Tile newTile = new Tile(this, tilePrefab, x, y);
        newTile.tileType = type;
        if (destroy)
            DestroyImmediate(tileArr[x, y].transform.gameObject);
        tileArr[x, y] = newTile;
        return newTile;
    }

    //Prints  every tile with x,y, and type
    private void printTileArray()
    {
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                Debug.Log("Tile (" + tileArr[x, y].x + ", " + tileArr[x, y].y + "), type: " + tileArr[x, y].tileType);
            }
        }
    }

    //Determines how we calculate how far away things are.
    private int heuristic(Tile flag, Tile curr) //CURRENTLY MANHATTON X+Y
    {
        switch (heuristicMethod)
        {
            case 0:
                return 10 * (int)(Mathf.Abs(flag.x - curr.x) + Mathf.Abs(flag.y - curr.y));
                break;
            case 1:
                return 10* (int)(Mathf.Floor(Mathf.Sqrt((flag.x - curr.x)* (flag.x - curr.x) + (flag.y - curr.y) * (flag.y - curr.y))));
                break;
            case 2:
                int dx = Mathf.Abs(flag.x - curr.x);
                int dy = Mathf.Abs(flag.y - curr.y);
                int D = 1;
                int D2 = 2;
                if (dx > dy)
                    return (D * (dx - dy) + D2 * dy);
                else
                    return (D * (dy - dx) + D2 * dx);
            default:
                return 0;
                break;
        }
    }
    
    //Handles us submitting the map options, checks if the answers make sense
    public void HandleMapOptions()
    {
        if (int.Parse(startXField.text) > int.Parse(sizeField.text) - 1
            || int.Parse(startYField.text) > int.Parse(sizeField.text) - 1
            || int.Parse(flagXField.text) > int.Parse(sizeField.text) - 1
            || int.Parse(flagYField.text) > int.Parse(sizeField.text) - 1
            || int.Parse(obstacleCountField.text) >= (int.Parse(sizeField.text) * int.Parse(sizeField.text)))
        {
            string err = "Start Pos and Flag Pos must be a number less than or equal to " + (int.Parse(sizeField.text) - 1) + " (map size-1) and greater than 0. Obstacles must be less than " + (int.Parse(sizeField.text) * int.Parse(sizeField.text)) + " (map size squared) and greater than 0";
            errorText.text = err;
        }
        else
        {
            errorText.text = "";
            SetupMap();
        }

    }

    //Handles us submitting the maze options, checks if the answers make sense
    public void HandleMazeOptions()
    {
        Debug.Log("Clicked");
        if (int.Parse(startXField.text) > int.Parse(sizeField.text) - 1
            || int.Parse(startYField.text) > int.Parse(sizeField.text) - 1
            || int.Parse(flagXField.text) > int.Parse(sizeField.text) - 1
            || int.Parse(flagYField.text) > int.Parse(sizeField.text) - 1)
        {
            string err = "Start Pos and Flag Pos must be a number less than or equal to " + (int.Parse(sizeField.text) - 1) + " (map size-1) and greater than 0. ";
            errorText.text = err;
        }
        else
        {
            errorText.text = "";
            setUpMaze();
        }

    }

    //Sets up the scale, mapsize, and obstacles based on userInput and generates a map
    public void SetupMap()
    {
        mapSize = int.Parse(sizeField.text);
        mapSeed = int.Parse(seedField.text);
        obstacleCount = int.Parse(obstacleCountField.text);
        scale = 10.0f / mapSize;
        GenerateMap();
    }

    //Sets up our maze dimensions and such
    private void setUpMaze()
    {
        mapSize = int.Parse(sizeField.text);
        mapSeed = int.Parse(seedField.text);
        scale = 10.0f / mapSize;
        StartCoroutine("primsMaze");
    }

    //Generates a maze using Prim's algorithm
    private IEnumerator primsMaze()
    {
        footNoteText.text = "";
        tileArr = new Tile[mapSize, mapSize];
        allTileCoords = new List<Coord>();

        //DESTROY PREVIOUS MAP
        if (transform.Find(mapName))
        {
            DestroyImmediate(transform.Find(mapName).gameObject);
        }

        //CREATE A NEW MAP
        mapHolder = new GameObject(mapName).transform;
        mapHolder.parent = transform;

        //First we begin with a grid filled will walls
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                spawnTile(x, y, obstaclePrefab, Tile.type.OBSTACLE, false);
                allTileCoords.Add(new Coord(x, y));
            }
        }

        //We start the generation from the start cell
        startTile = spawnTile(int.Parse(startXField.text), int.Parse(startYField.text), startPrefab, Tile.type.START);
        yield return new WaitForSeconds(.08f);
        //Collect the 4 adjacent walls
        List<Tile> walls = new List<Tile>();
        walls = startTile.getWalls(walls);
        System.Random prng = new System.Random(mapSeed);
        //While walls is populated
        while (walls.Count != 0)
        {
            //Pick a random wall from the list
            int r = prng.Next(walls.Count);
            Tile currWall = walls[r];
            int dx = -1 * (currWall.parent.x - currWall.x);
            int dy = -1 * (currWall.parent.y - currWall.y);
            if (currWall.x + dx < mapSize && currWall.x + dx >= 0 && currWall.y + dy < mapSize && currWall.y + dy >= 0)
            {
                if (tileArr[currWall.x + dx, currWall.y + dy].tileType == Tile.type.OBSTACLE)
                {
                    Tile tile1 = spawnTile(currWall.x, currWall.y, tilePrefab, Tile.type.EMPTY);
                    Tile tile2 = spawnTile(currWall.x + dx, currWall.y + dy, tilePrefab, Tile.type.EMPTY);
                    walls = tile2.getWalls(walls);
                }
            }
            walls.RemoveAt(r);

        }

        flagTile = spawnTile(int.Parse(flagXField.text), int.Parse(flagYField.text), flagPrefab, Tile.type.FLAG);
        
    }

    //The hard work, some brute force and inellegance at it's finest
    public void GenerateMap()
    {
        footNoteText.text = "";
        tileArr = new Tile[mapSize, mapSize];
        //PSUEDO-RANDOM ALGORITHM FOR GENERATION OF OBSTACLES
        allTileCoords = new List<Coord>();
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                allTileCoords.Add(new Coord(x, y));
            }
        }
        shuffledTileCoords = new Queue<Coord>(HelperMethods.ShuffleArray(allTileCoords.ToArray(), mapSeed));

        //DESTROY PREVIOUS MAP
        if (transform.Find(mapName))
        {
            DestroyImmediate(transform.Find(mapName).gameObject);
        }

        //CREATE A NEW MAP
        mapHolder = new GameObject(mapName).transform;
        mapHolder.parent = transform;

        //SPAWN OPEN TILES
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                spawnTile(x, y, tilePrefab, Tile.type.EMPTY, false);
            }
        }
        //SPAWN OBSTACLE
        for (int i = 0; i < obstacleCount; i++)
        {
            Coord randomCoord = GetRandomCoord();
            spawnTile(randomCoord.x, randomCoord.y, obstaclePrefab, Tile.type.OBSTACLE);
        }

        //SPAWN START
        startTile = spawnTile(int.Parse(startXField.text), int.Parse(startYField.text), startPrefab, Tile.type.START);

        //SPAWN FLAG
        flagTile = spawnTile(int.Parse(flagXField.text), int.Parse(flagYField.text), flagPrefab, Tile.type.FLAG);

        //printTileArray();
    }

    //Change array coordinates to vectors in our game
    Vector3 CoordToVector(int x, int y)
    {
        return new Vector3((-mapSize / 2.0f + 0.5f + x) * scale, (-mapSize / 2.0f + 0.5f + y) * scale, 0);
    }

    //Gets a random coordinate (based on a seed)
    public Coord GetRandomCoord()
    {
        Coord randomCoord = shuffledTileCoords.Dequeue();
        shuffledTileCoords.Enqueue(randomCoord);
        return randomCoord;
    }

    //Struct for the Map honestly I don't remember if I used this or not, I'm so tired
    public struct Map
    {
        public Transform[,] tileArr;

        public Map(Transform[,] _tileArr)
        {
            tileArr = _tileArr;
        }
    }

    //Class that defines a Tile, it does a lot of things and none of it is interesting
    public class Tile : IComparable<Tile>
    {
        public MapGenerator map;
        public InputField sizeField;
        public Transform transform;
        public enum type { OPEN, OBSTACLE, START, FLAG, CLOSED, EMPTY, EXPLORED };
        public type tileType;
        public int F_COST;
        public int H_COST;
        public int G_COST;
        public int x;
        public int y;
        public Tile parent;

        //Sets up a tile with some default values
        public Tile(MapGenerator _map, Transform _transform, int _x, int _y)
        {
            map = _map;
            sizeField = map.sizeField;
            transform = _transform;
            tileType = type.OPEN;
            F_COST = 0;
            H_COST = 0;
            G_COST = 0;
            x = _x;
            y = _y;
            parent = null;
        }

        //How to Compare tile values
        public int CompareTo(Tile otherTile)
        {
            if (otherTile == null) return 1;

            if (otherTile != null)
                return this.F_COST.CompareTo(otherTile.F_COST);
            else
                throw new ArgumentException("Object is not a Tile");
        }

        //Gets all the neighboring cells
        public PriorityQueue<Tile> getNeighbors()
        {
            //Debug.Log("CHECKING: (" + this.x + ", " + this.y + ")");
            PriorityQueue<Tile> neighbors = new PriorityQueue<Tile>();
            for (int j = this.x - 1; j <= this.x + 1; j++)
            {
                for (int k = this.y - 1; k <= this.y + 1; k++)
                {
                    //Debug.Log("(" + j + ", " + k + ")");
                    if (j >= 0 && j < int.Parse(sizeField.text) && k >= 0 && k < int.Parse(sizeField.text)) 
                    {
                       // Debug.Log("(" + j + ", " + k + ") of type "+ map.tileArr[j, k].tileType);
                        if (!(j == this.x && k == this.y))
                        {
                            neighbors.Enqueue(map.tileArr[j, k]);
                           // Debug.Log("added (" + j + ", " + k + ")");
                        }
                    }
                }
            }
            return neighbors;
        }

        //Assigns the costs to the cells
        internal void assignCosts()
        {
                Tile parent = this.parent;
                int fromCurrToParent = (int)Mathf.Floor(10 * Mathf.Sqrt(((this.x - parent.x) * (this.x - parent.x)) + ((this.y - parent.y) * (this.y - parent.y))));
                this.G_COST = parent.G_COST + fromCurrToParent;
                this.H_COST = map.heuristic(map.flagTile, this);
                this.F_COST = this.G_COST + this.H_COST;

        }

        internal List<Tile> getWalls(List<Tile> walls)
        {
                if (this.x+1 < int.Parse(map.sizeField.text) && map.tileArr[this.x + 1, this.y].tileType == Tile.type.OBSTACLE)
                {
                    walls.Add(map.tileArr[this.x + 1, this.y]);
                    map.tileArr[this.x + 1, this.y].parent = this;
                }
                if (this.y + 1 < int.Parse(map.sizeField.text) && map.tileArr[this.x, this.y + 1].tileType == Tile.type.OBSTACLE)
                {
                    walls.Add(map.tileArr[this.x, this.y + 1]);
                    map.tileArr[this.x, this.y + 1].parent = this;
                }
                if (this.x - 1 >= 0 && map.tileArr[this.x - 1, this.y].tileType == Tile.type.OBSTACLE)
                {
                    walls.Add(map.tileArr[this.x - 1, this.y]);
                    map.tileArr[this.x - 1, this.y].parent = this;
                }
                if (this.y - 1 >= 0 && map.tileArr[this.x, this.y - 1].tileType == Tile.type.OBSTACLE)
                {
                    walls.Add(map.tileArr[this.x, this.y - 1]);
                    map.tileArr[this.x, this.y - 1].parent = this;
                }
            return walls;
        }

        internal Tile getAcrossWall()
        {
            return null;
        }
    }

    //Coordinate structure :) so cute
    public struct Coord
    {
        public int x;
        public int y;

        public Coord(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
    }
}

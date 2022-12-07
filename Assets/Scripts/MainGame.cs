using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGame : MonoBehaviour
{
    public static MainGame Instance;

    enum GameState
    {
        Normal,
        Won,
        Lost
    }

    public Transform BubbleTopLeft;
    public Transform BubbleStart;
    public Transform NextBubbleStart;
    public GameObject[] PrefabBubbles;
    public float BubbleSize = 1.6f;
    public int Lines = 11;
    public int Width = 8;
    public GameObject Canon;
    public float RotationSpeed = Mathf.PI / 4;
    public GameObject Lost;
    public GameObject Won;
    public Transform Death;

    public Bubble[,] BubblesGrid;


    Bubble _currentBubble;
    Bubble _nextBubble;
    float _angle;
    GameState _state;

    public void Awake()
    {
        Instance = this;

        BubblesGrid = new Bubble[Width, Lines];

        //for (int y = 0; y < 3; y++)
        //{
        //    int xCount = y % 2 == 0 ? 8 : 7;
        //    float xOffset = y % 2 == 0 ? 0 : BubbleSize / 2.0f;

        //    for (int x = 0; x < xCount; x++)
        //    {
        //        GameObject go = GameObject.Instantiate(PrefabBubble,  GridToWorld(x,y) , Quaternion.identity);
        //    }
        //}

        SpawnNewBubble();

    }

    void SpawnNewBubble()
    {
        int rnd = Random.Range(0, PrefabBubbles.Length);
        GameObject go;
        if (_nextBubble == null)
        {
            go = GameObject.Instantiate(PrefabBubbles[rnd], BubbleStart.transform.position, Quaternion.identity);
            _currentBubble = go.GetComponent<Bubble>();
        }
        else
        {
            _currentBubble = _nextBubble;
            _currentBubble.transform.position = BubbleStart.transform.position;
        }

        rnd = Random.Range(0, PrefabBubbles.Length);

        go = GameObject.Instantiate(PrefabBubbles[rnd], NextBubbleStart.transform.position, Quaternion.identity);
        _nextBubble = go.GetComponent<Bubble>();
    }


    public void Update()
    {
        if (_state != GameState.Normal)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _currentBubble.Move(new Vector2(Mathf.Cos(_angle + Mathf.PI / 2.0f), Mathf.Sin(_angle + Mathf.PI / 2.0f)));
            SpawnNewBubble();

        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (_angle < Mathf.PI / 3.0f)
                _angle += RotationSpeed * Time.deltaTime;
            else
                _angle = Mathf.PI / 3.0f;

            Canon.transform.rotation = Quaternion.Euler(0, 0, _angle * Mathf.Rad2Deg);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            if (_angle > -Mathf.PI / 3.0f)
                _angle -= RotationSpeed * Time.deltaTime;
            else
                _angle = -Mathf.PI / 3.0f;

            Canon.transform.rotation = Quaternion.Euler(0, 0, _angle * Mathf.Rad2Deg);

        }
    }



    public Vector3 GridToWorld(int x, int y)
    {
        int xCount = y % 2 == 0 ? Width : Width - 1;
        float xOffset = y % 2 == 0 ? 0 : BubbleSize / 2.0f;

        return new Vector3(BubbleTopLeft.transform.position.x + BubbleSize * x + xOffset, BubbleTopLeft.transform.position.y - y * BubbleSize, BubbleTopLeft.transform.position.z);
    }

    public Vector2Int WorldToGrid(Vector3 world)
    {
        Vector2Int bestPosition = new Vector2Int(-1, -1);
        float bestDistanceSq = float.MaxValue;

        for (int y = 0; y < Lines; y++)
        {
            int xCount = y % 2 == 0 ? Width : Width - 1;
            for (int x = 0; x < xCount; x++)
            {
                if (BubblesGrid[x, y] != null)
                    continue;

                float distanceSq = (world - GridToWorld(x, y)).sqrMagnitude;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestPosition = new Vector2Int(x, y);
                }

            }
        }
        return bestPosition;
    }

    public Vector3 WorldToWorldAligned(Vector3 world, out Vector2Int grid)
    {
        grid = WorldToGrid(world);
        return GridToWorld(grid.x, grid.y);
    }

    public void FixBubble(Bubble bubble)
    {
        if ( bubble.transform.position.y < Death.transform.position.y)
        {
            _state = GameState.Lost;
            Lost.SetActive(true);

            return;
        }

        bubble.transform.position = WorldToWorldAligned(bubble.transform.position, out Vector2Int grid);
        MainGame.Instance.BubblesGrid[grid.x, grid.y] = bubble;

        BubbleColor[,] colorGrid = new BubbleColor[Width, Lines];

        FillNeighbourColor(colorGrid, bubble.Color, grid);

        int count = 0;

        for (int y = 0; y < Lines; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (colorGrid[x, y] == bubble.Color)
                    count++;
            }
        }

        if (count >= 3)
        {
            for (int y = 0; y < Lines; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (colorGrid[x, y] == bubble.Color)
                    {
                        Bubble bubbleToDestroy = BubblesGrid[x, y];
                        bubbleToDestroy.DestroyBubble();
       

                        BubblesGrid[x, y] = null;
                    }
                }
            }


            for (int y = 0; y < Lines; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (BubblesGrid[x, y] != null)
                    {
                        BubblesGrid[x, y].Attached = y == 0;
                    }
                }
            }

            bool[,] bubbleChecked = new bool[Width, Lines];
            int countAttached = 0;
            for (int x = 0; x < Width; x++)
            {
                if (BubblesGrid[x, 0] != null)
                {
                    countAttached++;
                    CheckAttached(bubbleChecked, new Vector2Int(x, 0));
                }
            }

            for (int y = 0; y < Lines; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (BubblesGrid[x, y] != null &&  BubblesGrid[x, y].Attached == false)
                    {
                        Bubble bubbleToDestroy = BubblesGrid[x, y];
                        bubbleToDestroy.DestroyBubble();

                        BubblesGrid[x, y] = null;
                    }
                }
            }

            if (countAttached == 0 )
            {
                _state = GameState.Won;
                Won.SetActive(true);
            }
        }



    }

    void CheckAttached(bool[,] bubbleChecked, Vector2Int position)
    {
        int xCount = position.y % 2 == 0 ? Width : Width - 1;

        if (position.x >= xCount || position.x < 0)
            return;

        if (position.y >= Lines || position.y < 0)
            return;

        if (bubbleChecked[position.x, position.y] == true)
            return;

        bubbleChecked[position.x, position.y] = true;

        if (BubblesGrid[position.x, position.y] == null)
        {
            return;
        }

        BubblesGrid[position.x, position.y].Attached = true;

        if (position.y % 2 == 0)
        {
            CheckAttached(bubbleChecked, position + new Vector2Int(-1, -1));
            CheckAttached(bubbleChecked, position + new Vector2Int(0, -1));
            CheckAttached(bubbleChecked, position + new Vector2Int(-1, 0));
            CheckAttached(bubbleChecked, position + new Vector2Int(1, 0));
            CheckAttached(bubbleChecked, position + new Vector2Int(-1, 1));
            CheckAttached(bubbleChecked, position + new Vector2Int(0, 1));
        }
        else
        {
            CheckAttached(bubbleChecked, position + new Vector2Int(0, -1));
            CheckAttached(bubbleChecked, position + new Vector2Int(1, -1));
            CheckAttached(bubbleChecked, position + new Vector2Int(-1, 0));
            CheckAttached(bubbleChecked, position + new Vector2Int(1, 0));
            CheckAttached(bubbleChecked, position + new Vector2Int(0, 1));
            CheckAttached(bubbleChecked, position + new Vector2Int(1, 1));
        }
    }


    void FillNeighbourColor(BubbleColor[,] grid, BubbleColor color, Vector2Int position)
    {
        int xCount = position.y % 2 == 0 ? Width : Width - 1;

        if (position.x >= xCount || position.x < 0)
            return;

        if (position.y >= Lines || position.y < 0)
            return;

        if (grid[position.x, position.y] == color || grid[position.x, position.y] == BubbleColor.Explored)
            return;

        if (BubblesGrid[position.x, position.y] != null && BubblesGrid[position.x, position.y].Color == color)
        {
            grid[position.x, position.y] = color;
        }
        else
        {
            grid[position.x, position.y] = BubbleColor.Explored;
            return;

        }

        if (position.y % 2 == 0)
        {
            FillNeighbourColor(grid, color, position + new Vector2Int(-1, -1));
            FillNeighbourColor(grid, color, position + new Vector2Int(0, -1));
            FillNeighbourColor(grid, color, position + new Vector2Int(-1, 0));
            FillNeighbourColor(grid, color, position + new Vector2Int(1, 0));
            FillNeighbourColor(grid, color, position + new Vector2Int(-1, 1));
            FillNeighbourColor(grid, color, position + new Vector2Int(0, 1));
        }
        else
        {
            FillNeighbourColor(grid, color, position + new Vector2Int(0, -1));
            FillNeighbourColor(grid, color, position + new Vector2Int(1, -1));
            FillNeighbourColor(grid, color, position + new Vector2Int(-1, 0));
            FillNeighbourColor(grid, color, position + new Vector2Int(1, 0));
            FillNeighbourColor(grid, color, position + new Vector2Int(0, 1));
            FillNeighbourColor(grid, color, position + new Vector2Int(1, 1));
        }

    }


}

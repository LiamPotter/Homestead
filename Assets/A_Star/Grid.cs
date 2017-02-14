using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BeardedManStudios.Network;
[ExecuteInEditMode]
[System.Serializable]
public class Grid : NetworkedMonoBehavior {
    public bool displayGridGizmos;
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public  float nodeRadius;

    [SerializeField]
    public Node[] grid;

    float nodeDiameter;

    [SerializeField]
    int gridSizeX, gridSizeY;

    [NetSync]
    public bool gridCreated;

    [SerializeField]
    public Vector3 gridStartPos;

    public GameObject farmTile;



    protected override void NetworkStart()
    {
        base.NetworkStart();
        Debug.Log(NetworkedId);

    }
 
   
  
  void Awake()
    {
        gridStartPos = transform.position;
       
    }

    public float NodeDiameter
    {
        get { return nodeDiameter; }
    }
    public int MaxSize
    {
        get{
            return gridSizeX * gridSizeY;
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
            if (grid != null && displayGridGizmos)
            {
                foreach (Node n in grid)
                {
                    Gizmos.color = (n.walkable) ? Color.white : Color.red;
                if (n.movementPenalty > 30)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawCube(n.worldPos, Vector3.one * (nodeDiameter - 0.1f));
                }
            }
    }
 
    public List<Node> GetNeighbours(Node node)
    {
        //Where is the node in Grid?
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >=0 && checkY < gridSizeY)
                {
                  //  neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;

    }
    
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {


        //convert world pos to percentage for x and y coordinate to tell how far along it is
        // for X coordinate on Far left will have a percentage of 0
        float percentX =  (worldPosition.x - gridStartPos.x + gridWorldSize.x / 2) / gridWorldSize.x ;
        float percentY =   (worldPosition.z - gridStartPos.z + gridWorldSize.y / 2) / gridWorldSize.y ;


        //Clamp to stop errors if it is outside of the grid
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt( (gridSizeX ) * percentX);
        int y = Mathf.RoundToInt((gridSizeY ) * percentY);
       
        return grid[y * gridSizeX + x];
    }
    
    [SerializeField]
    public void CreateGrid()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        
        grid = new Node[gridSizeX * gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
        int i = 0;
        for (int x  = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y ++)
            {
                //As x increses, go in increments of node diameter along the world untill we reach the edge 
                //same for Y 
                //this gives each point that a node will occupy in our world
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                worldPoint.x = Mathf.RoundToInt(worldPoint.x);
                worldPoint.y = Mathf.RoundToInt(worldPoint.y);
                worldPoint.z = Mathf.RoundToInt(worldPoint.z);

                //Can raycast to find layer, and set the cost higher

                grid[y * gridSizeX + x] = new Node(true, worldPoint, x,y, 0);
                
                GameObject tileins = Instantiate(farmTile, grid[ y * gridSizeX + x].worldPos, Quaternion.identity, transform);
                FarmTile fTile = tileins.GetComponent<FarmTile>();
                tileins.transform.localScale = Vector3.one * ( nodeRadius * 0.2f );
                tileins.GetComponent<Renderer>().material = fTile.mats[0];
                fTile.myNode = grid[y * gridSizeX + x];
                fTile.myNode.myTile = fTile;
                fTile.matName = "Grass";
                tileins.name = "Tile " + i;
                gridCreated = true;
                i++;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;

public class PathAStar
{
    Queue<Tile> path;

    public PathAStar(World world, Tile tileStart, Tile tileEnd)
    {
        //check to see if we have a valid tile graph
        if(world.tileGraph == null)
        {
            world.tileGraph = new PathTileGraph(world);
        }
        //A dictionary of all valid, walkable nodes.
        Dictionary<Tile, PathNode<Tile>> nodes = world.tileGraph.nodes;
        //Make sure our start/end tiles are in the list of nodes
        if (nodes.ContainsKey(tileStart) == false)
        {
            Debug.LogError("PathAStar: Starting tile is not in the list of nodes");
            //Right now going to manually add the start tile into the list of nodes
            return;
        }

        if (nodes.ContainsKey(tileEnd) == false)
        {
            Debug.LogError("PathAStar: Ending tile is not in the list of nodes");
            return;
        }

        PathNode<Tile> start = nodes[tileStart];
        PathNode<Tile> goal = nodes[tileEnd];



        List<PathNode<Tile>> closedSet = new List<PathNode<Tile>>();
        //List<PathNode<Tile>> openSet = new List<PathNode<Tile>>();
        //openSet.Add(start);

        SimplePriorityQueue<PathNode<Tile>> openSet = new SimplePriorityQueue<PathNode<Tile>>();
        openSet.Enqueue(start,0);

        Dictionary<PathNode<Tile>, PathNode<Tile>> cameFrom = new Dictionary<PathNode<Tile>, PathNode<Tile>>();

        Dictionary<PathNode<Tile>, float> gscore = new Dictionary<PathNode<Tile>, float>();

        foreach(PathNode<Tile> node in nodes.Values)
        {
            gscore[node] = Mathf.Infinity;
        }

        gscore[start] = 0;

        Dictionary<PathNode<Tile>, float> fscore = new Dictionary<PathNode<Tile>, float>();

        foreach (PathNode<Tile> node in nodes.Values)
        {
            fscore[node] = Mathf.Infinity;
        }

        fscore[start] = HeuristicCostEstimate(start, goal);
        while(openSet.Count > 0)
        {
            PathNode<Tile> current = openSet.Dequeue();
            if(current == goal)
            {
                //We have reached our goal lets convert this to a sequence of tiles and then end this constructor
                ReconstructPath(cameFrom, current);
                return;
            }
            closedSet.Add(current);
            foreach(PathEdge<Tile> edgeNeighbour in current.edges)
            {
                PathNode<Tile> neighbour = edgeNeighbour.node;
                
                if(closedSet.Contains(neighbour) == true)
                {
                    continue; //ignore this already completed neighbour
                }
                float movementCostToNeighbor = neighbour.data.movementCost * DistanceBetween(current, neighbour);
                float tentativeGscore = gscore[current] + movementCostToNeighbor;

                if(openSet.Contains(neighbour) && tentativeGscore >= gscore[neighbour])
                {
                    continue;
                }

                cameFrom[neighbour] = current;
                gscore[neighbour] = tentativeGscore;
                fscore[neighbour] = gscore[neighbour] + HeuristicCostEstimate(neighbour, goal);

                if(openSet.Contains(neighbour) == false)
                {
                    openSet.Enqueue(neighbour, fscore[neighbour]);
                }
            }
        }

        //if we reach here we went through the whole set without reaching the goal
        //there is no path from start to goal
        return;
    }

    float HeuristicCostEstimate(PathNode<Tile> start, PathNode<Tile> goal)
    {
        return Mathf.Sqrt(Mathf.Pow(start.data.X - goal.data.X, 2) + Mathf.Pow(start.data.Y - goal.data.Y, 2));
    }

    float DistanceBetween(PathNode<Tile> start, PathNode<Tile> goal)
    {
        //We can make assumptions because we know we are working on a grid

        //Hori/Vert neighbours have a distance of 1
        if(Mathf.Abs(start.data.X - goal.data.X) + Mathf.Abs(start.data.Y - goal.data.Y) == 1)
        {
            return 1f;
        }

        //Diag neighbours have a distance of 1.41421356237
        if(Mathf.Abs(start.data.X - goal.data.X)==1 &&  Mathf.Abs(start.data.Y - goal.data.Y) == 1)
        {
            return 1.41421356237f;
        }

        //otherwise do the actual math
        return Mathf.Sqrt(Mathf.Pow(start.data.X - goal.data.X, 2) + Mathf.Pow(start.data.Y - goal.data.Y, 2));
    }

    void ReconstructPath(Dictionary<PathNode<Tile>, PathNode<Tile>> cameFrom, PathNode<Tile> current)
    {
        //At this point current is the goal so walk backwards from the camefrom map
        Queue<Tile> totalPath = new Queue<Tile>();
        totalPath.Enqueue(current.data);

        while(cameFrom.ContainsKey(current))
        {
            //camefrom is a map where the key => value relationship is really saying some_node => we got there from this node
            current = cameFrom[current];
            totalPath.Enqueue(current.data);
        }

        //At this point, total path is a queue that is running backwards from the End tile to the start tile
        path = new Queue<Tile> (totalPath.Reverse());
    }

    public Tile Dequeue()
    {
        return path.Dequeue();
    }

    public int Length()
    {
        if(path == null)
        {
            return 0;
        }

        return path.Count;
    }
}


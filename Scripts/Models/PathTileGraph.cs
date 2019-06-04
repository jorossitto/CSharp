using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTileGraph
{
    //This class constructs a simple path-finding compatible graph of our world
    //each tile is a node and each Walkable neighbour from a tile is linked via an edge connection
    public Dictionary<Tile, PathNode<Tile>> nodes;

    public PathTileGraph(World world)
    {
        //Debug.Log("PathTileGraph");
        //loop through all tiles of the world
        //for each tile, create a node
        nodes = new Dictionary<Tile, PathNode<Tile>>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile tile = world.GetTileAt(x, y);
                //if(tile.movementCost > 0) //Tiles with movement cost of 0 are unwalkable
                //{
                    PathNode<Tile> node = new PathNode<Tile>();
                    node.data = tile;
                    nodes.Add(tile, node);
                //}
            }
        }
        //Debug.Log("PathTileGraph created " + nodes.Count + " nodes");

        //loop through a second time and create edges for neighbours
        int edgeCount = 0;
        foreach (Tile tile in nodes.Keys)
        {
            List<PathEdge<Tile>> edges = new List<PathEdge<Tile>>();
            PathNode<Tile> node = nodes[tile];
            //get a list of neighbours for the tile
            Tile[] neighbours = tile.GetNeighbours(true); //Note: some of the array spots could be null.

            //if neighbour is walkable, create an edge to the relevant node.
            for (int i = 0; i < neighbours.Length; i++)
            {
                if(neighbours[i] != null && neighbours[i].movementCost > 0)
                {
                    //Neighbour exists and is walkable, so create an edge
                    //But make sure we aren't trying to squeeze inapproperiately
                    if(IsClippingCorner(tile, neighbours[i]))
                    {
                        continue; //skip to the next neighbour without building an edge
                    }
                    PathEdge<Tile> edge = new PathEdge<Tile>();
                    edge.cost = neighbours[i].movementCost;
                    edge.node = nodes[neighbours[i]];
                    //Add the edge to our temporary and growable list
                    edges.Add(edge);
                    edgeCount++;
                }
            }
            node.edges = edges.ToArray();
        }
        //Debug.Log("PathTileGraph created " + edgeCount + " edges");
    }
    bool IsClippingCorner(Tile current, Tile neighbour)
    {
        int dx = current.X - neighbour.X;
        int dy = current.Y - neighbour.Y;
        //if the movment from current to neighbour is diagnal then check to make sure we aren't clipping
        if (Mathf.Abs(dx) + Mathf.Abs(dy)==2)
        {
            //we are diagonal

            if(current.world.GetTileAt(current.X - dx, current.Y).movementCost == 0)
            {
                //East or west is unwalkable, therefore this would be a clipped movment
                return true;
            }
            if (current.world.GetTileAt(current.X, current.Y - dy).movementCost == 0)
            {
                return true;
            }
        }

        return false;
    }
}

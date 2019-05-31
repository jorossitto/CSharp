using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTileGraph
{
    //This class constructs a simple path-finding compatible graph of our world
    //each tile is a node and each Walkable neighbour from a tile is linked via an edge connection
    Dictionary<Tile, PathNode<Tile>> nodes;

    public PathTileGraph(World world)
    {
        //loop through all tiles of the world
        //for each tile, create a node
        

        nodes = new Dictionary<Tile, PathNode<Tile>>();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile tile = world.GetTileAt(x, y);
                if(tile.movementCost > 0) //Tiles with movement cost of 0 are unwalkable
                {
                    PathNode<Tile> node = new PathNode<Tile>();
                    node.data = tile;
                    nodes.Add(tile, node);
                }
            }
        }
        //loop through a second time and create edges for neighbours

        foreach (Tile tile in nodes.Keys)
        {
            //get a list of neighbours for the tile
            //if neighbour is walkable, create an edge to the relevant node.
        }
    }
}

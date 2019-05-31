using UnityEngine;
using System.Collections;
using System;

public enum TileType { Empty, Floor };

public class Tile
{

	TileType type = TileType.Empty;

    // the function we callback any time our tile's data changes
    Action<Tile> callbackTileChanged;

    int x;
    int y;
    public int X { get => x; }
    public int Y { get => y; }

    public float movementCost
    {
        get
        {
            if(type == TileType.Empty)
            {
                return 0; //tile is unwalkable
            }
            if(furniture == null)
            {
                return 1;
            }

            return 1 * furniture.movementCost;
        }
    }

    Inventory inventory;

    //furniture is something like a wall door or sofa
    public Furniture furniture
    {
        get; protected set;
    }

    //The contex in which we exist
    public World world
    {
        get; protected set;
    }

    public Job pendingFurnitureJob;

    public TileType Type
    {
        get
        {
            return type;
        }

        //Call the callback and let things know we've changed.
        set
        {
            TileType oldType = type;
            type = value;
            if(callbackTileChanged != null && oldType != type)
            {
                callbackTileChanged(this);
            }
            
        } 
    }

    //Initalizes a new instance of the class
    public Tile( World world, int x, int y )
    {
		this.world = world;
		this.x = x;
		this.y = y;
	}

    //Register a function to be called back when our tile type changes
    public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        callbackTileChanged += callback;
    }

    public void UnRegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        callbackTileChanged -= callback;
    }

    public bool PlaceFurniture(Furniture objInstance)
    {
        if(objInstance == null)
        {
            //Uninstall whatever was here before
            furniture = null;
            return true;
        }

        // objInstance isn't null
        if(furniture != null)
        {
            Debug.LogError("Trying to assign furniture to a tile that already has one!");
            return false;
        }

        //At this point everythin's fine
        furniture = objInstance;
        return true;
    }

    //Tells us if two tiles are adjacent
    public bool IsNeighbour(Tile tile, bool diagOkay = false)
    {
        if(this.x == tile.x && (this.y == tile.y +1 || this.y == tile.y-1))
        {
            return true;
        }
        if (this.y == tile.y &&(this.x == tile.x +1 || this.x == tile.y-1))
        {
            return true;
        }

        if(diagOkay)
        {
            if (this.x == tile.x+1 && (this.y == tile.y + 1 || this.y == tile.y - 1))
            {
                return true;
            }
            if (this.x == tile.x -1 && (this.y == tile.y + 1 || this.y == tile.y - 1))
            {
                return true;
            }
        }

        return false;
    }
}

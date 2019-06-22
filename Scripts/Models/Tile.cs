using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public enum TileType { Empty, Floor };
public enum Enterability { Yes, Never, Soon};

public class Tile:IXmlSerializable
{
    const string TYPE = "Type";

	TileType type = TileType.Empty;
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
            if (callbackTileChanged != null && oldType != type)
            {
                callbackTileChanged(this);
            }

        }
    }

    //Is something like a drill or a stack of metal sitting on the floor
    public Inventory inventoryTile;

    public Room room;

    // the function we callback any time our tile's data changes
    Action<Tile> callbackTileChanged;

    //furniture is something like a wall door or sofa
    public Furniture furniture
    {
        get; protected set;
    }

    public Job pendingFurnitureJob;

    //The contex in which we exist
    public World world
    {
        get; protected set;
    }

    int x;
    int y;
    public int X { get => x; }
    public int Y { get => y; }

    float baseTileMovementCost = 1f; // Fixme this is hardcoded for now
    public float movementCost
    {
        get
        {
            if (type == TileType.Empty)
            {
                return 0; //tile is unwalkable
            }
            if (furniture == null)
            {
                return 1;
            }

            return baseTileMovementCost * furniture.movementCost;
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

    public bool UnplaceFurniture()
    {
        //Just uninstalling Fixme: What if we have a multi-tile furniture
        if(furniture == null)
        {
            return false;
        }

        Furniture currentFurnitureObject = furniture;

        for (int x_off = x; x_off < (x + currentFurnitureObject.width); x_off++)
        {
            for (int y_off = y; y_off < (y + currentFurnitureObject.height); y_off++)
            {
                Tile tile = world.GetTileAt(x_off, y_off);
                tile.furniture = null;
            }
        }
        return true;
    }
    public bool PlaceFurniture(Furniture objInstance)
    {
        if( objInstance == null)
        {
            return UnplaceFurniture();
        }

        if (objInstance != null && objInstance.IsValidPosition(this) == false)
        {
            Debug.LogError("Trying to assign furniture to a tile that already has one!");
            return false;
        }

        for (int x_off = x; x_off < (x + objInstance.width); x_off++)
        {
            for (int y_off = y; y_off < (y + objInstance.height); y_off++)
            {
                Tile tile = world.GetTileAt(x_off, y_off);
                tile.furniture = objInstance;
            }
        }
        return true;
    }

    public bool PlaceInventory(Inventory inventoryInstance)
    {
        if (inventoryInstance == null)
        {
            //Uninstall whatever was here before
            inventoryTile = null;
            return true;
        }

        // inventoryInstance isn't null
        if (inventoryTile != null)
        {
            //There is already inventory here maybe we can combine a stack?
            if(inventoryTile.objectType != inventoryInstance.objectType)
            {
                Debug.LogError("Trying to assign inventory to a tile who already has some of a different type");
                return false;
            }
            int numToMove = inventoryInstance.stackSize;
            if(inventoryTile.stackSize + numToMove > inventoryTile.maxStackSize)
            {
                numToMove = inventoryTile.maxStackSize - inventoryTile.stackSize;
            }
            inventoryTile.stackSize += numToMove;
            inventoryInstance.stackSize -= numToMove;
            return true;
        }
        inventoryTile = inventoryInstance.Clone();
        inventoryTile.tile = this;
        inventoryInstance.stackSize = 0;
        return true;
    }

    //Tells us if two tiles are adjacent
    public bool IsNeighbour(Tile tile, bool diagOkay = false)
    {
        //check to see if we have a difference of exactly one between the two tile coordinates
        //if true we are neighbours
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


    public Tile[] GetNeighbours(bool diagOkay = false)
    {
        Tile[] neighbours;
        if(diagOkay == false)
        {
            neighbours = new Tile[4]; //Tile order : NESW
        }
        else
        {
            neighbours = new Tile[8]; //Tile order :NESW NE SE SW NW
        }

        Tile neighbour;
        neighbour = world.GetTileAt(x, y + 1);
        neighbours[0] = neighbour; //could be null but thats ok
        neighbour = world.GetTileAt(x+1, y);
        neighbours[1] = neighbour; //could be null but thats ok
        neighbour = world.GetTileAt(x, y - 1);
        neighbours[2] = neighbour; //could be null but thats ok
        neighbour = world.GetTileAt(x-1, y);
        neighbours[3] = neighbour; //could be null but thats ok

        if(diagOkay == true)
        {
            neighbour = world.GetTileAt(x+1, y + 1);
            neighbours[4] = neighbour; //could be null but thats ok
            neighbour = world.GetTileAt(x + 1, y-1);
            neighbours[5] = neighbour; //could be null but thats ok
            neighbour = world.GetTileAt(x-1, y - 1);
            neighbours[6] = neighbour; //could be null but thats ok
            neighbour = world.GetTileAt(x - 1, y+1);
            neighbours[7] = neighbour; //could be null but thats ok
        }

        return neighbours;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                                                Saving and Loading
    /// 
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public XmlSchema GetSchema()
    {
        return null;
    }
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString(TYPE, ((int)Type).ToString());
    }
    public void ReadXml(XmlReader reader)
    {
        //type = (TileType)int.Parse(reader.GetAttribute("Type"));
        Type = (TileType)int.Parse(reader.GetAttribute(TYPE));
    }

    public Enterability IsEnterable()
    {
        //This returns true if you can enter this tile right this moment.
        if(movementCost == 0)
        {
            return Enterability.Never;
        }

        //Check out furniture to see if it has a special block on enterability
        if(furniture != null && furniture.isEnterable != null)
        {
            return furniture.isEnterable(furniture);
        }

        return Enterability.Yes;
    }

    public Tile North()
    {
        return world.GetTileAt(x, y + 1);
    }
    public Tile South()
    {
        return world.GetTileAt(x, y - 1);
    }
    public Tile East()
    {
        return world.GetTileAt(x+1, y);
    }
    public Tile West()
    {
        return world.GetTileAt(x-1, y);
    }


}

using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public enum TileType { Empty, Floor };

public class Tile:IXmlSerializable
{

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

    Inventory inventory;

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
        writer.WriteAttributeString("Type", ((int)Type).ToString());
    }
    public void ReadXml(XmlReader reader)
    {
        //type = (TileType)int.Parse(reader.GetAttribute("Type"));
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
    }


}

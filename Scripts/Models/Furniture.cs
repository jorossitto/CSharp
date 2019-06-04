using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;

//Installed objects are things like walls doors furniture
public class Furniture:IXmlSerializable
{
    const string OBJECT_TYPE = "objectType";
    const string MOVEMENT_COST = "movementCost";

    public Dictionary<string, object> furnitureParamaters;
    public Action<Furniture, float> updateActions;

    public void Update(float deltaTime)
    {
        if(updateActions != null)
        {
            updateActions(this, deltaTime);
        }       
    }

    //This represents the base tile of the object
    //objects may take up more then one tile
    public Tile tile
    {
        get; protected set;
    }

    //this object type will be queried by the visual system to know what sprite to render
    public string objectType
    {
        get; protected set;
    }

    //Speed = 1/Movementcost
    //Tile types + effects may be combined for total movmentCost
    //Movemnt cost of 0 is impassable
    public float movementCost
    {
        get; protected set;
    }

    //Graphics cost may not be the full covered area
    int width;
    int height;

    public bool linksToNeighbour
    {
        get; protected set;
    }


    Action<Furniture> callBackOnChanged;

    Func<Tile, bool> funcPositionValidation;

    //Todo implement larger objects
    //Todo implement object rotation

    /// <summary>
    /// Empty furniture constructor for serilization only
    /// </summary>
    public Furniture()
    {
        furnitureParamaters = new Dictionary<string, object>();
    }
    /// <summary>
    /// Copy Construtor
    /// </summary>
    /// <param name="other"> the OTHER furniture you would like to copy</param>
    protected Furniture(Furniture other)
    {
        this.objectType = other.objectType;
        this.movementCost = other.movementCost;
        this.width = other.width;
        this.height = other.height;
        this.linksToNeighbour = other.linksToNeighbour;
        this.furnitureParamaters = new Dictionary<string, object>(other.furnitureParamaters);
        if(other.updateActions != null)
        {
            this.updateActions = (Action<Furniture, float>)other.updateActions.Clone();
        }
        
    }

    virtual public Furniture Clone()
    {
        return new Furniture(this);
    }

    /// <summary>
    /// Creates furniture from paramaters -- this will only ever be used for prototypes
    /// </summary>
    /// <param name="objectType">The string representing the objects type</param>
    /// <param name="movementCost">The cost of movment through the square/ Use 0 for impassable </param>
    /// <param name="width">Width in tiles the object occupies</param>
    /// <param name="height">Height in tiles the object occupies</param>
    /// <param name="linksToNeighbour">Does the sprite change if placed adjacent to other tiles</param>
    public Furniture (string objectType, float movementCost=1f, int width=1, int height=1, bool linksToNeighbour = false)
    {
        this.objectType = objectType;
        this.movementCost = movementCost;
        this.width = width;
        this.height = height;
        this.linksToNeighbour = linksToNeighbour;
        this.funcPositionValidation = this.__IsValidPosition;
        this.furnitureParamaters = new Dictionary<string, object>();
    }

    static public Furniture PlaceInstance(Furniture proto, Tile tile)
    {
        if(proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance -- Furniture Position is not valid");
            return null;
        }

        //We know our placement destination is valid
        Furniture objFurniture = proto.Clone();
        objFurniture.tile = tile;

        //Todo Fixme this assumes we are 1x1
        if (tile.PlaceFurniture(objFurniture) == false)
        {
            //for some reason we weren't able to place our object in this tile
            //Probably it was already occupied
            //Do not return our newly instantiated object.
            //(it will be garbage collected)
            return null;
        }
        if(objFurniture.linksToNeighbour)
        {
            //This type of furniture links itself to its neighbours
            //Informs furniture that they have a new buddy by triggering onchangedcallback
            Tile tempTile;
            int x = tile.X;
            int y = tile.Y;

            tempTile = tile.world.GetTileAt(x, y + 1);
            if (tempTile != null && tempTile.furniture != null && tempTile.furniture.callBackOnChanged != null && tempTile.furniture.objectType == objFurniture.objectType)
            {
                //We have a northern neighbour with the same object type as us
                //tell the furniture to change by firing the callback
                tempTile.furniture.callBackOnChanged(tempTile.furniture);
            }
            tempTile = tile.world.GetTileAt(x, y - 1);
            if (tempTile != null && tempTile.furniture != null && tempTile.furniture.callBackOnChanged != null && tempTile.furniture.objectType == objFurniture.objectType)
            {
                tempTile.furniture.callBackOnChanged(tempTile.furniture);
            }
            tempTile = tile.world.GetTileAt(x + 1, y);
            if (tempTile != null && tempTile.furniture != null && tempTile.furniture.callBackOnChanged != null && tempTile.furniture.objectType == objFurniture.objectType)
            {
                tempTile.furniture.callBackOnChanged(tempTile.furniture);
            }
            tempTile = tile.world.GetTileAt(x - 1, y);
            if (tempTile != null && tempTile.furniture != null && tempTile.furniture.callBackOnChanged != null && tempTile.furniture.objectType == objFurniture.objectType)
            {
                tempTile.furniture.callBackOnChanged(tempTile.furniture);
            }
        }
        return objFurniture;
    }

    public void RegisterOnChangedCallback(Action<Furniture> callbackFunction)
    {
        callBackOnChanged += callbackFunction;
    }

    public void UnregisterOnChangedCallback(Action<Furniture> callbackFunction)
    {
        callBackOnChanged += callbackFunction;
    }

    public bool IsValidPosition(Tile tile)
    {
        return funcPositionValidation(tile);
    }

    //todo fixme these functions should never be called directly
    //therefore they shouldn't be public
    public bool __IsValidPosition(Tile tile)
    {
        //Make sure tile is floor
        if(tile.Type != TileType.Floor)
        {
            return false;
        }
        //Make sure tile doesn't already have furniture
        if(tile.furniture != null)
        {
            return false;
        }

        return true;
    }

    public bool __IsValidPositionDoor(Tile tile)
    {
        if(__IsValidPosition(tile) == false)
        {
            return false;
        }

        //Make sure we have a pair of E/W walls or N/S walls
        return true;
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
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString(OBJECT_TYPE, objectType);
        writer.WriteAttributeString(MOVEMENT_COST, movementCost.ToString());

        foreach (string key in furnitureParamaters.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", key);
            writer.WriteAttributeString("value", furnitureParamaters[key].ToString());
            writer.WriteEndElement();

        }
    }

    public void ReadXml(XmlReader reader)
    {

        //Debug.LogError("Furniture read xml not used");
        //x, y, and object type have already been set and we should already be assigned to a tile so just read extra data
        //objectType = reader.GetAttribute(OBJECT_TYPE);
        movementCost = int.Parse(reader.GetAttribute(MOVEMENT_COST));

        if(reader.ReadToDescendant("Param"))
        {
            do
            {

            } while (true);
        }
    }


}

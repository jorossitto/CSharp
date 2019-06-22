using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;

//Installed objects are things like walls doors furniture
public class Furniture : IXmlSerializable
{
    const string OBJECT_TYPE = "objectType";
    const string MOVEMENT_COST = "movementCost";
    const string NAME = "name";
    const string PARAM = "Param";
    const string VALUE = "value";

    //custom parameter for this particular piece of furniture
    //We are using a diction because later custom lua function will be able to use whatever parameters the user/modder would like
    //The lua code will bind to this dictionary
    protected Dictionary<string, float> furnitureParamaters;
    //Actions are called every update they get passed the furniture they belonged to plus a delta time
    protected Action<Furniture, float> updateActions;


    public Func<Furniture, Enterability> isEnterable;

    List<Job> jobs;

    public void Update(float deltaTime)
    {
        if (updateActions != null)
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

    public bool roomEnclosure
    {
        get; protected set;
    }
    //Graphics cost may not be the full covered area
    public int width
    {
        get; protected set;
    }
    public int height
    {
        get; protected set;
    }

    public Color tint = Color.white;

    public bool linksToNeighbour
    {
        get; protected set;
    }


    public Action<Furniture> callBackOnChanged;
    public Action<Furniture> callBackOnRemoved;

    Func<Tile, bool> funcPositionValidation;

    //Todo implement larger objects
    //Todo implement object rotation

    /// <summary>
    /// Empty furniture constructor for serilization only
    /// </summary>
    public Furniture()
    {
        furnitureParamaters = new Dictionary<string, float>();
        jobs = new List<Job>();
    }
    /// <summary>
    /// Copy Construtor -- don't call this directly unless we never do any subclassing instead use clone() which is more virtual
    /// </summary>
    /// <param name="other"> the OTHER furniture you would like to copy</param>
    protected Furniture(Furniture other)
    {
        this.objectType = other.objectType;
        this.movementCost = other.movementCost;
        this.roomEnclosure = other.roomEnclosure;
        this.width = other.width;
        this.height = other.height;
        this.tint = other.tint;
        this.linksToNeighbour = other.linksToNeighbour;
        this.furnitureParamaters = new Dictionary<string, float>(other.furnitureParamaters);
        jobs = new List<Job>();

        if (other.updateActions != null)
        {
            this.updateActions = (Action<Furniture, float>)other.updateActions.Clone();
        }

        if (other.funcPositionValidation != null)
        {
            this.funcPositionValidation = (Func<Tile, bool>)other.funcPositionValidation.Clone();
        }

        this.isEnterable = other.isEnterable;
        
    }
    /// <summary>
    /// Make a copy of the current furniture. Sub-classes should override this clone() if a different (sub-classed) copy constructor should be run
    /// </summary>
    /// <returns></returns>
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
    public Furniture (string objectType, float movementCost=1f, int width=1, int height=1, bool linksToNeighbour = false, bool roomEnclosure = false)
    {
        this.objectType = objectType;
        this.movementCost = movementCost;
        this.roomEnclosure = roomEnclosure;
        this.width = width;
        this.height = height;
        this.linksToNeighbour = linksToNeighbour;
        this.funcPositionValidation = this.DefaultIsValidPosition;
        this.furnitureParamaters = new Dictionary<string, float>();
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

    public void RegisterOnRemovedCallback(Action<Furniture> callbackFunction)
    {
        callBackOnRemoved += callbackFunction;
    }

    public void UnregisterOnRemovedCallback(Action<Furniture> callbackFunction)
    {
        callBackOnRemoved += callbackFunction;
    }


    public bool IsValidPosition(Tile tile)
    {
        return funcPositionValidation(tile);
    }

    //todo fixme these functions should never be called directly
    //therefore they shouldn't be public
    //this will be replaced by validation checks fed to us from lua
    //will be custimizable for each piece of furniture
    //door might be specific that it needs two walls to connect to.
    protected bool DefaultIsValidPosition(Tile tile)
    {
        for (int x_off = tile.X; x_off < (tile.X + width); x_off++)
        {
            for (int y_off = tile.Y; y_off < (tile.Y + height); y_off++)
            {
                Tile tileInRange = tile.world.GetTileAt(x_off, y_off);
                //Make sure tile is floor
                if (tileInRange.Type != TileType.Floor)
                {
                    return false;
                }
                //Make sure tile doesn't already have furniture
                if (tileInRange.furniture != null)
                {
                    return false;
                }
            }
        }
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
        //writer.WriteAttributeString(MOVEMENT_COST, movementCost.ToString());

        foreach (string key in furnitureParamaters.Keys)
        {
            writer.WriteStartElement(PARAM);
            writer.WriteAttributeString(NAME, key);
            writer.WriteAttributeString(VALUE, furnitureParamaters[key].ToString());
            writer.WriteEndElement();

        }
    }

    public void ReadXml(XmlReader reader)
    {

        //Debug.LogError("Furniture read xml not used");
        //x, y, and object type have already been set and we should already be assigned to a tile so just read extra data
        //objectType = reader.GetAttribute(OBJECT_TYPE);
        //movementCost = int.Parse(reader.GetAttribute(MOVEMENT_COST));

        if(reader.ReadToDescendant(PARAM))
        {
            do
            {
                string key = reader.GetAttribute(NAME);
                float value = float.Parse(reader.GetAttribute(VALUE));
                furnitureParamaters[key] = value;

            } while (reader.ReadToNextSibling(PARAM));
        }
    }
    /// <summary>
    /// Gets the custom furniture paramater from a string
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns>the paramater value from a float </returns>
    public float GetParameter(string key, float defaultValue = 0)
    {
        if (furnitureParamaters.ContainsKey(key) == false)
        {
            return defaultValue;
        }
        return furnitureParamaters[key];
    }

    public void SetParameter(string key, float value)
    {
        furnitureParamaters[key] = value;
    }

    public void ChangeParameter(string key, float value)
    {
        if (furnitureParamaters.ContainsKey(key) == false)
        {
            furnitureParamaters[key] = value;
        }

        furnitureParamaters[key] += value;
    }

    /// <summary>
    /// Register a function that will be called every update
    /// Later this implementation might change a bit as we support lua
    /// </summary>
    /// <param name="a">a stands for action</param>
    public void RegisterUpdateAction(Action<Furniture, float> action)
    {
        updateActions += action;
    }

    public void UnregisterUpdateAction(Action<Furniture, float> action)
    {
        updateActions -= action;
    }

    public int JobCount()
    {
        return jobs.Count;
    }

    public void AddJob(Job job)
    {
        jobs.Add(job);
        tile.world.jobQueue.Enqueue(job);
    }

    public void RemoveJob(Job job)
    {
        jobs.Remove(job);
        job.CancelJob();
        tile.world.jobQueue.Remove(job);

    }

    public void ClearJobs()
    {
        foreach(Job job in jobs)
        {
            RemoveJob(job);
        }
    }

    public bool IsStockpile()
    {
        return objectType == Config.STOCKPILE;
    }

    public void Deconstruct()
    {
        Debug.Log("Deconstruct");
        tile.UnplaceFurniture();

        if(callBackOnRemoved != null)
        {
            callBackOnRemoved(this);
        }
        //At this point no data structures should be pointing to us so we should get garbage-collected

    }
}

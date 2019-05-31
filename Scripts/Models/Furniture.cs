using UnityEngine;
using System.Collections;
using System;

//Installed objects are things like walls doors furniture
public class Furniture
{
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

    protected Furniture()
    {

    }

    static public Furniture CreatePrototype(string objectType, float movementCost=1f, int width=1, int height=1, bool linksToNeighbour = false)
    {
        Furniture objFurniture = new Furniture();
        objFurniture.objectType = objectType;
        objFurniture.movementCost = movementCost;
        objFurniture.width = width;
        objFurniture.height = height;
        objFurniture.linksToNeighbour = linksToNeighbour;
        objFurniture.funcPositionValidation = objFurniture.__IsValidPosition;


        return objFurniture;
    }

    static public Furniture PlaceInstance(Furniture proto, Tile tile)
    {
        if(proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance -- Furniture Position is not valid");
            return null;
        }

        //We know our placement destination is valid
        Furniture objFurniture = new Furniture();
        objFurniture.objectType = proto.objectType;
        objFurniture.movementCost = proto.movementCost;
        objFurniture.width = proto.width;
        objFurniture.height = proto.height;
        objFurniture.linksToNeighbour = proto.linksToNeighbour;
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
            if (tempTile != null && tempTile.furniture != null && tempTile.furniture.objectType == objFurniture.objectType)
            {
                //We have a northern neighbour with the same object type as us
                //tell the furniture to change by firing the callback
                tempTile.furniture.callBackOnChanged(tempTile.furniture);
            }
            tempTile = tile.world.GetTileAt(x, y - 1);
            if (tempTile != null && tempTile.furniture != null && tempTile.furniture.objectType == objFurniture.objectType)
            {
                tempTile.furniture.callBackOnChanged(tempTile.furniture);
            }
            tempTile = tile.world.GetTileAt(x + 1, y);
            if (tempTile != null && tempTile.furniture != null && tempTile.furniture.objectType == objFurniture.objectType)
            {
                tempTile.furniture.callBackOnChanged(tempTile.furniture);
            }
            tempTile = tile.world.GetTileAt(x - 1, y);
            if (tempTile != null && tempTile.furniture != null && tempTile.furniture.objectType == objFurniture.objectType)
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
}

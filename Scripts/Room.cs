using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Room 
{
    Dictionary<string, float> atmosphericGasses;

    List<Tile> tiles;

    World world;

    public Room(World world)
    {
        this.world = world;
        tiles = new List<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
    }

    public void AssignTile(Tile tile)
    {
        if(tiles.Contains(tile))
        {
            // this tile already is in this room 
            return;
        }

        if(tile.room != null)
        {
            //Belongs to some other room
            tile.room.tiles.Remove(tile);
        }

        tile.room = this;
        tiles.Add(tile);
    }

    public void UnassignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutsideRoom(); //Assign to outside
        }
        tiles = new List<Tile>();
    }

    public bool IsOutsideRoom()
    {
        return this == world.GetOutsideRoom();
    }

    public void ChangeGas(string name, float amount)
    {
        if(IsOutsideRoom())
        {
            return;
        }

        if(atmosphericGasses.ContainsKey(name))
        {
            atmosphericGasses[name] += amount;
        }
        else
        {
            atmosphericGasses[name] = amount;
        }

        if(atmosphericGasses[name] < 0)
        {
            atmosphericGasses[name] = 0;
        }
    }

    public float GetGasAmount(string name)
    {
        if(atmosphericGasses.ContainsKey(name))
        {
            return atmosphericGasses[name];
        }

        return 0;
    }

    public float GetGasPercentage(string name)
    {
        if(atmosphericGasses.ContainsKey(name) == false)
        {
            return 0f;
        }

        float total = 0f;

        foreach (string n in atmosphericGasses.Keys)
        {
            total += atmosphericGasses[n];
        }

        return atmosphericGasses[name] / total;
    }

    public string[] GetGasNames()
    {
        return atmosphericGasses.Keys.ToArray();
    }

    /// <summary>
    /// Checks the room for openings by looking at the NESW neighbours and do flood fills from them
    /// </summary>
    /// <param name="sourceFurniture">sourcefurniture is the piece of furniture that may be splitting
    /// two existing rooms or closing a new room</param>
    public static void DoRoomFloodFill(Furniture sourceFurniture)
    {
        World world = sourceFurniture.tile.world;

        Room oldRoom = sourceFurniture.tile.room;
        //Try building new rooms starting from the north
        foreach(Tile tile in sourceFurniture.tile.GetNeighbours())
        {
            ActualFloodFill(tile, oldRoom);
        }

        sourceFurniture.tile.room = null;
        oldRoom.tiles.Remove(sourceFurniture.tile);

        //If this piece of furniture was added to an existing room
        //(Should always be true because the outside is one big room)
        //delete that room and assign all tiles within to be"outside" for now
        if(oldRoom.IsOutsideRoom() == false)
        {
            //At this point oldroom shouldn't have any more tiles left in it
            //this delete room should mostly only need to remove the room from the worlds list
            if(oldRoom.tiles.Count > 0)
            {
                Debug.LogError("Oldroom still has tiles, this should not happen");
            }
            world.DeleteRoom(oldRoom);
        }
    }

    protected static void ActualFloodFill(Tile tile, Room oldRoom)
    {
        if(tile == null)
        {
            //we are trying to flood fill off the map so just return
            return;
        }

        if(tile.room != oldRoom)
        {
            //This tile was already assigned to another "new" room, which means that
            //the direction picked was not isolated 
            return;
        }

        if(tile.furniture != null && tile.furniture.roomEnclosure)
        {
            return;
        }

        if (tile.Type == TileType.Empty)
        {
            //This tile is empty space and must remain part of the outside
            return;
        }
        //If we get to this point then we know that we need to create a new room
        Room newRoom = new Room(oldRoom.world);
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        while(tilesToCheck.Count > 0)
        {
            Tile currentTileToCheck = tilesToCheck.Dequeue();

            if(tile.Type == TileType.Empty)
            {

            }
            if(currentTileToCheck.room == oldRoom)
            {
                newRoom.AssignTile(currentTileToCheck);
                Tile[] neighbourTiles = currentTileToCheck.GetNeighbours();
                foreach(Tile neighbourTile in neighbourTiles)
                {
                    if(neighbourTile == null || neighbourTile.Type == TileType.Empty)
                    {
                        //We have hit open space (either by being the edge of the map or being an empty tile
                        //so this room we are building is actually the outside  therefore we can immediatly end the flood fill
                        //delete this new room and re-assign all the tiles
                        newRoom.UnassignAllTiles();
                        return;
                    }
                    //we know t2 is not null or empty
                    if (neighbourTile.room == oldRoom && (neighbourTile.furniture == null || neighbourTile.furniture.roomEnclosure == false))
                    {
                        tilesToCheck.Enqueue(neighbourTile);
                    }
                }
            }
        }
        //Copy data from the old room into the new room.
        newRoom.CopyGas(oldRoom);

        //Tell the world that a new room has been formed
        tile.world.AddRoom(newRoom);
    }

    void CopyGas(Room otherRoom)
    {
        foreach (string name in otherRoom.atmosphericGasses.Keys)
        {
            this.atmosphericGasses[name] = otherRoom.atmosphericGasses[name];
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class World : IXmlSerializable
{
    const string STOCKPILE = Config.STOCKPILE;
    const string OXYGEN_GENERATOR = "Oxygen Generator";
    

    //two dimensional array to hold our tile data
    Tile[,] tiles;
    public List<Character> characters;
    public List<Furniture> furnitures;
    public List<Room> rooms;
    public InventoryManager inventoryManager;

    //Pathfinding graph used to navigate our world
    public PathTileGraph tileGraph;

    public Dictionary<string, Furniture> furniturePrototypes;
    public Dictionary<string, Job> furnitureJobPrototypes;

    //Tile Width of the world
    int width;
    public int Width { get => width; }

    //Tile height of the world
    int height;
    public int Height { get => height; }

    //CB stands for callback
    Action<Furniture> callBackFurnitureCreated;
    Action<Character> callBackCharacterCreated;
    Action<Inventory> callBackInventoryCreated;
    Action<Tile> callBackTileChanged;

    //Todo most likely this will be replaced with a dedicated class for managing job queues
    //might be static or self initalizing
    public JobQueue jobQueue;


    //Initializes a new instance of the world class
    public World(int width, int height)
    {
        //Creates an empty world
        SetUpWorld(width, height);
        //Make one character
        Character character = CreateCharacter(GetTileAt(Width / 2, Height / 2));
    }
    /// <summary>
    /// Default Constructor, used when loading a world from a file.
    /// </summary>
    public World()
    {

    }
    public Room GetOutsideRoom()
    {
        return rooms[0];
    }

    public void AddRoom(Room room)
    {
        rooms.Add(room);
    }
    public void DeleteRoom(Room room)
    {
        if(room == GetOutsideRoom())
        {
            Debug.LogError("Tried to delete the outside");
            return;
        }
        rooms.Remove(room);//remove room from rooms list
        room.UnassignAllTiles();//all tiles that belong to this room should be re-assigned to the outside

    }

    private void SetUpWorld(int width, int height)
    {
        jobQueue = new JobQueue();

        this.width = width;
        this.height = height;

        tiles = new Tile[width, height];

        rooms = new List<Room>();
        rooms.Add(new Room(this)); //Create the outside

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
                tiles[x, y].room = rooms[0]; //room[0] is outside
            }
        }

        //Debug.Log ("World created with " + (width*height) + " tiles.");
        CreateFurniturePrototypes();
        characters = new List<Character>();
        furnitures = new List<Furniture>();
        inventoryManager = new InventoryManager();

    }

    public void Update(float deltaTime)
    {
        foreach(Character character in characters)
        {
            character.Update(deltaTime);
        }

        foreach (Furniture furniture in furnitures)
        {
            furniture.Update(deltaTime);
        }

    }

    public Character CreateCharacter(Tile tile)
    {
        Character character = new Character(tile);
        characters.Add(character);
        if(callBackCharacterCreated != null)
        {
            callBackCharacterCreated(character);
        }

        return character;
    }

    void CreateFurniturePrototypes()
    {
        furniturePrototypes = new Dictionary<string, Furniture>();
        furnitureJobPrototypes = new Dictionary<string, Job>();

        furniturePrototypes.Add(Config.WALL, //Name of object
            new Furniture(Config.WALL,
            0, //Can not be passed through
            1, //Width
            1, //Height
            true, //Links to neighbours and becomes part of a larger object
            true //The walls enclose rooms
            ));

        furnitureJobPrototypes.Add(Config.WALL, new Job(null, Config.WALL, FurnitureActions.JobCompleteFurnitureBuilding, 1f, new Inventory[] { new Inventory(Config.STEEL_PLATE, 5, 0)}));
        

        furniturePrototypes.Add(Config.DOOR, //Name of object
            new Furniture(Config.DOOR,
            1, //Pathfinding cost
            1, //Width
            1, //Height
            false, //Links to neighbours and becomes part of a larger object
            true //Doors enclose rooms
            ));

        //What if the object behaviors were scriptable and therefore were part of the text file
        furniturePrototypes[Config.DOOR].SetParameter(Config.OPENNESS,0);
        furniturePrototypes[Config.DOOR].SetParameter(Config.IS_OPENING, 0);
        furniturePrototypes[Config.DOOR].RegisterUpdateAction( FurnitureActions.DoorUpdateAction);
        furniturePrototypes[Config.DOOR].isEnterable = FurnitureActions.DoorIsEnterable;

        furniturePrototypes.Add(STOCKPILE, //Name of object
        new Furniture(STOCKPILE,
        1, //1 = standard movment cost
        1, //Width
        1, //Height
        true, //the stockpiles link to their neighbours
        false //The stockpiles do not enclose rooms
        ));

        furniturePrototypes[STOCKPILE].RegisterUpdateAction(FurnitureActions.StockpileUpdateAction);
        furniturePrototypes[STOCKPILE].tint = new Color32(186,31,31,100);
        furnitureJobPrototypes.Add(STOCKPILE, new Job(null, STOCKPILE, FurnitureActions.JobCompleteFurnitureBuilding, -1f, null));


        furniturePrototypes.Add(OXYGEN_GENERATOR, //Name of object
        new Furniture(OXYGEN_GENERATOR,
        10, //Pathfinding cost
        2, //Width
        2, //Height
        false, //Links to neighbours and becomes part of a larger object
        false //Doors enclose rooms
        ));

        furniturePrototypes[OXYGEN_GENERATOR].RegisterUpdateAction(FurnitureActions.OxygenGeneratorUpdateAction);
    }

    public void RandomizeTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if(UnityEngine.Random.Range(0,2)==0)
                {
                    tiles[x, y].Type = TileType.Empty;
                }
                else
                {
                    tiles[x, y].Type = TileType.Floor;
                }
            }
        }
    }

    public void SetupPathFindingExample()
    {
        //Debug.Log("SetupPathFindingExample");
        //set up floors and walls to test pathfinding
        int l = width / 2 - 5;
        int b = height / 2 - 5;
        for (int x = l - 5; x < l + 15; x++)
        {
            for (int y = b - 5; y < b + 15; y++)
            {
                tiles[x, y].Type = TileType.Floor;
                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        PlaceFurniture(Config.WALL, tiles[x, y]);
                    }
                }
            }
        }
    }

    public Tile GetTileAt(int x, int y)
    {
		if( x >= width || x < 0 || y >= Height || y < 0)
        {
			//Debug.LogError("Tile ("+x+","+y+") is out of range.");
			return null;
		}
		return tiles[x, y];
	}

    public Furniture PlaceFurniture(string objectType, Tile tile)
    {
        //Debug.Log("PlaceFurniture");
        //Todo this function assumes 1x1 tiles -- change later

        if(furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("FurniturePrototypes doesn't contain a proto for a key: " + objectType);
            return null;
        }

        Furniture furniture = Furniture.PlaceInstance(furniturePrototypes[objectType], tile);

        if (furniture == null)
        {
            //Failed to place object -- most likely there was something already there
            return null;
        }

        furniture.RegisterOnRemovedCallback(OnFurnitureRemoved);
        furnitures.Add(furniture);
        
        //Do we need to recalculate our rooms?
        if(furniture.roomEnclosure)
        {
            Room.DoRoomFloodFill(furniture);
        }

        if(callBackFurnitureCreated != null)
        {
            callBackFurnitureCreated(furniture);
            if(furniture.movementCost != 1)
            {
                //Since tiles return movement cost as their base cost multiplied by furniture
                //a movement cost of 1 does not effect pathfinding
                InvalidateTileGraph(); //Reset the pathfinding system
            }
            
        }

        return furniture;
    }

    public void RegisterFurnitureCreated(Action<Furniture> callbackFunc)
    {
        callBackFurnitureCreated += callbackFunc;
    }

    public void UnregisterFurnitureCreated(Action<Furniture> callbackFunc)
    {
        callBackFurnitureCreated -= callbackFunc;
    }

    public void RegisterCharacterCreated(Action<Character> callbackFunc)
    {
        callBackCharacterCreated += callbackFunc;
    }

    public void UnregisterCharacterCreated(Action<Character> callbackFunc)
    {
        callBackCharacterCreated -= callbackFunc;
    }

    public void RegisterInventoryCreated(Action<Inventory> callbackFunc)
    {
        callBackInventoryCreated += callbackFunc;
    }

    public void UnregisterInventoryCreated(Action<Inventory> callbackFunc)
    {
        callBackInventoryCreated -= callbackFunc;
    }

    public void RegisterTileChanged(Action<Tile> callbackFunc)
    {
        callBackTileChanged += callbackFunc;
    }

    public void UnregisterTileChanged(Action<Tile> callbackFunc)
    {
        callBackTileChanged -= callbackFunc;
    }

    //Gets called whenever any tile changes
    void OnTileChanged(Tile tile)
    {
        if(callBackTileChanged == null)
        {
            return;
        }
        callBackTileChanged(tile);
        InvalidateTileGraph();
    }

    //Should be called whenever a change to the world
    //means that our old pathfinding info is invalid
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }
    public bool IsFurniturePlacementValid(string furnitureType, Tile tile)
    {
        return furniturePrototypes[furnitureType].IsValidPosition(tile);
    }

    public Furniture GetFurniturePrototype(string objectType)
    {
        if(furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("No furniture with type: " + objectType);
            return null;
        }
        return furniturePrototypes[objectType];
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
        //save info here
        writer.WriteAttributeString(Config.WIDTH, Width.ToString());
        writer.WriteAttributeString(Config.HEIGHT, Height.ToString());

        writer.WriteStartElement(Config.TILES);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if(tiles[x,y].Type != TileType.Empty)
                {
                    writer.WriteStartElement(Config.TILE);
                    tiles[x, y].WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }
        writer.WriteEndElement();

        writer.WriteStartElement(Config.FURNITURES);
        foreach (Furniture furniture in furnitures)
        {
            writer.WriteStartElement(Config.FURNITURE);
            furniture.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement(Config.CHARACTERS);
        foreach (Character character in characters)
        {
            writer.WriteStartElement(Config.CHARACTER);
            character.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        //Load Info Here
        //Debug.Log("ReadXML");
        width = int.Parse(reader.GetAttribute(Config.WIDTH));
        height = int.Parse(reader.GetAttribute(Config.HEIGHT));
        SetUpWorld(width, height);

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case Config.TILES:
                    ReadXmlTiles(reader);
                    break;
                case Config.FURNITURES:
                    ReadXmlFurnitures(reader);
                    break;
                case Config.CHARACTERS:
                    ReadXmlCharacters(reader);
                    break;

            }
        }
        //todo fixme: Debugging only remove me later
        //Create an inventory item

        Inventory inventory = new Inventory(Config.STEEL_PLATE, 50, 50);
        SpawnInventory(0, 0, inventory);
        inventory = new Inventory(Config.STEEL_PLATE, 50, 4);
        SpawnInventory(2, 0, inventory);
        inventory = new Inventory(Config.STEEL_PLATE, 50, 3);
        SpawnInventory(1, 2, inventory);

    }

    /// <summary>
    /// Location where the inventory will be spawned
    /// </summary>
    /// <param name="spawnLocationX"></param>
    /// <param name="spawnLocationY"></param>
    /// <param name="inventory"> the inventory to spawn</param>
    private void SpawnInventory(int spawnLocationX, int spawnLocationY, Inventory inventory)
    {
        Tile tile = GetTileAt(Width / 2 + spawnLocationX, Height / 2 + spawnLocationY);
        inventoryManager.PlaceInventory(tile, inventory);
        if (callBackInventoryCreated != null)
        {
            callBackInventoryCreated(tile.inventoryTile);
        }
    }

    private void ReadXmlTiles(XmlReader reader)
    {
        //We are in the tiles element, so read elements until we run out of "tile" nodes.
        if(reader.ReadToDescendant(Config.TILE))
        {
            //we have at least one tile so do something with it
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                tiles[x, y].ReadXml(reader);
            } while (reader.ReadToNextSibling(Config.TILE));
        }
    }

    private void ReadXmlFurnitures(XmlReader reader)
    {
        //We are in the furnitures element, so read elements until we run out of "Furniture" nodes.

        if (reader.ReadToDescendant(Config.FURNITURE))
        {
            //we have at least one tile so do something with it
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                //tiles[x, y].ReadXml(reader);
                Furniture furniture = PlaceFurniture(reader.GetAttribute(Config.OBJECT_TYPE), tiles[x, y]);
            } while (reader.ReadToNextSibling(Config.FURNITURE));
        }
    }

    private void ReadXmlCharacters(XmlReader reader)
    {
        //We are in the characters element, so read elements until we run out of "Character" objects.
        if (reader.ReadToDescendant(Config.CHARACTER))
        {
            //we have at least one tile so do something with it
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                //tiles[x, y].ReadXml(reader);
                Character character = CreateCharacter(tiles[x, y]);
            } while (reader.ReadToNextSibling(Config.CHARACTER));
        }
    }

    public void OnInventoryCreated(Inventory inventory)
    {
        if(callBackInventoryCreated != null)
        {
            callBackInventoryCreated(inventory);
        }
    }

    public void OnFurnitureRemoved(Furniture furniture)
    {
        furnitures.Remove(furniture);
    }
}

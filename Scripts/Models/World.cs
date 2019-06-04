using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class World : IXmlSerializable
{
    //Const Variables
    const string WIDTH = "Width";
    const string HEIGHT = "Height";
    const string TILES = "Tiles";
    const string TILE = "Tile";
    const string WALL = "Wall";
    const string FURNITURES = "Furnitures";
    const string FURNITURE = "Furniture";
    const string OBJECT_TYPE = "objectType";
    const string CHARACTERS = "Characters";
    const string CHARACTER = "Character";
    const string DOOR = "Door";

    //two dimensional array to hold our tile data
    Tile[,] tiles;
    public List<Character> characters;
    public List<Furniture> furnitures;

    //Pathfinding graph used to navigate our world
    public PathTileGraph tileGraph;

    Dictionary<string, Furniture> furniturePrototypes;

    //Tile Width of the world
    int width;
    public int Width { get => width; }

    //Tile height of the world
    int height;
    public int Height { get => height; }

    //CB stands for callback
    Action<Furniture> callBackFurnitureCreated;
    Action<Character> callBackCharacterCreated;
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

    private void SetUpWorld(int width, int height)
    {
        jobQueue = new JobQueue();

        this.width = width;
        this.height = height;

        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
            }
        }

        //Debug.Log ("World created with " + (width*height) + " tiles.");
        CreateFurniturePrototypes();
        characters = new List<Character>();
        furnitures = new List<Furniture>();
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
        furniturePrototypes.Add(WALL, //Name of object
            new Furniture(WALL,
            0, //Can not be passed through
            1, //Width
            1, //Height
            true //Links to neighbours and becomes part of a larger object
            ));

        furniturePrototypes.Add(DOOR, //Name of object
            new Furniture(DOOR,
            0, //Can not be passed through
            1, //Width
            1, //Height
            true //Links to neighbours and becomes part of a larger object
            ));

        //What if the object behaviors were scriptable and therefore were part of the text file
        furniturePrototypes[DOOR].furnitureParamaters["openess"] = 0;
        furniturePrototypes[DOOR].updateActions += FurnitureActions.DoorUpdateAction;

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
                        PlaceFurniture(WALL, tiles[x, y]);
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

        furnitures.Add(furniture);

        if(callBackFurnitureCreated != null)
        {
            callBackFurnitureCreated(furniture);
            InvalidateTileGraph();
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
    
    public World()
    {

    }

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        //save info here
        writer.WriteAttributeString(WIDTH, Width.ToString());
        writer.WriteAttributeString(HEIGHT, Height.ToString());

        writer.WriteStartElement(TILES);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                writer.WriteStartElement(TILE);
                tiles[x, y].WriteXml(writer);
                writer.WriteEndElement();
            }
        }
        writer.WriteEndElement();

        writer.WriteStartElement(FURNITURES);
        foreach (Furniture furniture in furnitures)
        {
            writer.WriteStartElement(FURNITURE);
            furniture.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement(CHARACTERS);
        foreach (Character character in characters)
        {
            writer.WriteStartElement(CHARACTER);
            character.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        //Load Info Here
        //Debug.Log("ReadXML");
        width = int.Parse(reader.GetAttribute(WIDTH));
        height = int.Parse(reader.GetAttribute(HEIGHT));
        SetUpWorld(width, height);

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case TILES:
                    ReadXmlTiles(reader);
                    break;
                case FURNITURES:
                    ReadXmlFurnitures(reader);
                    break;
                case CHARACTERS:
                    ReadXmlCharacters(reader);
                    break;

            }
        }

    }

    private void ReadXmlTiles(XmlReader reader)
    {
        //We are in the tiles element, so read elements until we run out of "tile" nodes.
        if(reader.ReadToDescendant(TILE))
        {
            //we have at least one tile so do something with it
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                tiles[x, y].ReadXml(reader);
            } while (reader.ReadToNextSibling(TILE));
        }
    }

    private void ReadXmlFurnitures(XmlReader reader)
    {
        //We are in the furnitures element, so read elements until we run out of "Furniture" nodes.

        if (reader.ReadToDescendant(FURNITURE))
        {
            //we have at least one tile so do something with it
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                //tiles[x, y].ReadXml(reader);
                Furniture furniture = PlaceFurniture(reader.GetAttribute(OBJECT_TYPE), tiles[x, y]);
            } while (reader.ReadToNextSibling(FURNITURE));
        }
    }

    private void ReadXmlCharacters(XmlReader reader)
    {
        //We are in the characters element, so read elements until we run out of "Character" objects.
        if (reader.ReadToDescendant(CHARACTER))
        {
            //we have at least one tile so do something with it
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                //tiles[x, y].ReadXml(reader);
                Character character = CreateCharacter(tiles[x, y]);
            } while (reader.ReadToNextSibling(CHARACTER));
        }
    }
}

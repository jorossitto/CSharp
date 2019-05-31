using UnityEngine;
using System.Collections.Generic;
using System;

public class World
{
    //two dimensional array to hold our tile data
	Tile[,] tiles;
    List<Character> characters;

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
    public World(int width = 100, int height = 100)
    {
        jobQueue = new JobQueue();

		this.width = width;
		this.height = height;

		tiles = new Tile[width,height];

		for (int x = 0; x < width; x++)
        {
			for (int y = 0; y < height; y++)
            {
				tiles[x,y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
			}
		}

		Debug.Log ("World created with " + (width*height) + " tiles.");
        CreateFurniturePrototypes();
        characters = new List<Character>();
	}

    public void Update(float deltaTime)
    {
        foreach(Character character in characters)
        {
            character.Update(deltaTime);
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
        Furniture wallPrototype = Furniture.CreatePrototype("Wall", 
            0, //Can not be passed through
            1, //Width
            1, //Height
            true //Links to neighbours and becomes part of a larger object
            );

        furniturePrototypes.Add("Wall", wallPrototype);
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
        Debug.Log("SetupPathFindingExample");
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
                        PlaceFurniture("Wall", tiles[x, y]);
                    }
                }
            }
        }
    }

    public Tile GetTileAt(int x, int y)
    {
		if( x > width || x < 0 || y > Height || y < 0)
        {
			Debug.LogError("Tile ("+x+","+y+") is out of range.");
			return null;
		}
		return tiles[x, y];
	}

    public void PlaceFurniture(string objectType, Tile tile)
    {
        Debug.Log("PlaceFurniture");
        //Todo this function assumes 1x1 tiles -- change later

        if(furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("FurniturePrototypes doesn't contain a proto for a key: " + objectType);
            return;
        }

        Furniture obj = Furniture.PlaceInstance(furniturePrototypes[objectType], tile);
        if (obj == null)
        {
            //Failed to place object -- most likely there was something already there
            return;
        }
        if(callBackFurnitureCreated != null)
        {
            callBackFurnitureCreated(obj);
        }
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

    void OnTileChanged(Tile tile)
    {
        if(callBackTileChanged == null)
        {
            return;
        }
        callBackTileChanged(tile);
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
}

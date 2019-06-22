using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FurnitureSpriteController : MonoBehaviour
{
    Dictionary<Furniture, GameObject> furnitureGameObjectMap;
    Dictionary<string, Sprite> furnitureSprites;
    const string FURNITURESORTINGLAYER = "Furniture";


    World world
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();
        //Instantiate our dictionary that tracks which gameobject is rendering which tile data
        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        //Register our callback so that our gameobject gets updated whenever the tile's type changes
        world.RegisterFurnitureCreated(OnFurnitureCreated);

        //Go through any existing furniture and call the oncreated event manually (ie from a save that was loaded)
        foreach(Furniture furniture in world.furnitures)
        {
            OnFurnitureCreated(furniture);
        }
    }

    private void LoadSprites()
    {
        furnitureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("BaseBuilder");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            furnitureSprites[s.name] = s;
        }
    }

    public void OnFurnitureCreated(Furniture furniture)
    {
        //Create a visual gameobject linked to this data
        //Debug.Log("OnFurnitureCreated");

        //Todo FixMe does not consider multi tile objects or rotated objects

        //Creates a new gameobject and adds it to our scene
        GameObject furnGameObject = new GameObject();


        //Add tile//Gameobject to dictionary
        furnitureGameObjectMap.Add(furniture, furnGameObject);
        furnGameObject.name = furniture.objectType + "(" + furniture.tile.X + "," + furniture.tile.Y + ")";
        furnGameObject.transform.position = new Vector3(furniture.tile.X + ((furniture.width -1) /2f), furniture.tile.Y + ((furniture.height - 1) / 2f), 0);
        furnGameObject.transform.SetParent(this.transform, true);

        if (furniture.objectType == Config.DOOR)
        {
            //By default the door graphic is ment for walls to the east and west
            //Check to see if we actually have a wall north south and rotate by 90d
            Tile north = world.GetTileAt(furniture.tile.X, furniture.tile.Y + 1);
            Tile south = world.GetTileAt(furniture.tile.X, furniture.tile.Y - 1);
            if (north != null && south != null
                && north.furniture != null && south.furniture != null
                && north.furniture.objectType == Config.WALL && south.furniture.objectType == Config.WALL)
            {
                furnGameObject.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }
        //Add a sprite renderer -> don't bother setting sprite because all tiles are empty
        SpriteRenderer objGameObjectSpriteRenderer = furnGameObject.AddComponent<SpriteRenderer>();

        //todo fixme assume that object must be a wall so use the hardcoded refrence to the wall sprite
        objGameObjectSpriteRenderer.sprite = GetFurnitureSprite(furniture);
        objGameObjectSpriteRenderer.sortingLayerName = FURNITURESORTINGLAYER;
        objGameObjectSpriteRenderer.color = furniture.tint;

        //Register our callback so that our gameobject gets updated whenever the object's into changes
        furniture.RegisterOnChangedCallback(OnFurnitureChanged);
        furniture.RegisterOnRemovedCallback(OnFurnitureRemoved);
    }
    void OnFurnitureRemoved(Furniture furniture)
    {
        if (furnitureGameObjectMap.ContainsKey(furniture) == false)
        {
            Debug.LogError("OnFurnitureRemoved -- trying to change visuals for furniture not in our map");
            return;
        }

        GameObject furnitureGameObject = furnitureGameObjectMap[furniture];
        Destroy(furnitureGameObject);
        furnitureGameObjectMap.Remove(furniture);
    }

    void OnFurnitureChanged(Furniture furniture)
    {
        //Debug.Log("OnFurnitureChanged " + furniture);

        //Make sure the furniture graphics are correct.

        if (furnitureGameObjectMap.ContainsKey(furniture) == false)
        {
            Debug.LogError("OnFurnitureChanged -- trying to change visuals for furniture not in our map");
            return;
        }

        GameObject furnitureGameObject = furnitureGameObjectMap[furniture];
        furnitureGameObject.GetComponent<SpriteRenderer>().sprite = GetFurnitureSprite(furniture);
        furnitureGameObject.GetComponent<SpriteRenderer>().color = furniture.tint;
        //If this is a door lets check openness and update the sprite


    }

    public Sprite GetFurnitureSprite(Furniture furniture)
    {
        string spriteName = furniture.objectType;
        //Debug.Log("GetFurnitureSprite " + furniture);
        if (furniture.linksToNeighbour == false)
        {
            if (furniture.objectType == Config.DOOR)
            {
                if (furniture.GetParameter(Config.OPENNESS) < .1f)
                {
                    //Door is closed
                    spriteName = Config.DOOR;
                }
                else if (furniture.GetParameter(Config.OPENNESS) < .5f)
                {
                    spriteName = Config.DOOR + "1";
                }
                else if (furniture.GetParameter(Config.OPENNESS) < .9f)
                {
                    spriteName = Config.DOOR + "2";
                }
                else
                {
                    spriteName = Config.DOOR + "3";
                }
            }
            return furnitureSprites[spriteName];
        }
        int x = furniture.tile.X;
        int y = furniture.tile.Y;

        //otherwise the sprite name is more complicated
        
        //Check for neighbours North, East, South, West
        Tile tile;
        tile = world.GetTileAt(x, y + 1);
        if (tile != null && tile.furniture != null && tile.furniture.objectType == furniture.objectType)
        {
            spriteName += "N";
        }
        tile = world.GetTileAt(x, y - 1);
        if (tile != null && tile.furniture != null && tile.furniture.objectType == furniture.objectType)
        {
            spriteName += "S";
        }
        tile = world.GetTileAt(x + 1, y);
        if (tile != null && tile.furniture != null && tile.furniture.objectType == furniture.objectType)
        {
            spriteName += "E";
        }
        tile = world.GetTileAt(x - 1, y);
        if (tile != null && tile.furniture != null && tile.furniture.objectType == furniture.objectType)
        {
            spriteName += "W";
        }



        if (furnitureSprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("GetFurnitureSprite -- No sprites with name " + spriteName);
            return furnitureSprites[furniture.objectType];
        }
        //If this is a door lets check openness and update the sprite
        //Fixme all this hardcoding needs to be generalized later


        return furnitureSprites[spriteName];

    }


    public Sprite GetFurnitureSprite(string objectType)
    {
        //Debug.Log("GetFurnitureSprite objectType " + objectType);

        if(furnitureSprites.ContainsKey(objectType))
        {
            return furnitureSprites[objectType];
        }

        if (furnitureSprites.ContainsKey(objectType + "_"))
        {
            return furnitureSprites[objectType + "_"];
        }

        Debug.LogError("GetSpriteForFurniture -- No sprites with name " + furnitureSprites[objectType]);
        return null;
    }
}

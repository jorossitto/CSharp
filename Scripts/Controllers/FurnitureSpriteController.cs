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
        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();
        world.RegisterFurnitureCreated(OnFurnitureCreated);
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

    public void OnFurnitureCreated(Furniture objFurniture)
    {
        //Create a visual gameobject linked to this data
        Debug.Log("OnFurnitureCreated");

        //Todo FixMe does not consider multi tile objects or rotated objects

        //Creates a new gameobject and adds it to our scene
        GameObject furnGameObject = new GameObject();
        //Add tile//Gameobject to dictionary
        furnitureGameObjectMap.Add(objFurniture, furnGameObject);
        furnGameObject.name = objFurniture.objectType + "(" + objFurniture.tile.X + "," + objFurniture.tile.Y + ")";
        furnGameObject.transform.position = new Vector3(objFurniture.tile.X, objFurniture.tile.Y, 0);
        furnGameObject.transform.SetParent(this.transform, true);

        //Add a sprite renderer -> don't bother setting sprite because all tiles are empty
        SpriteRenderer objGameObjectSpriteRenderer = furnGameObject.AddComponent<SpriteRenderer>();

        //todo fixme assume that object must be a wall so use the hardcoded refrence to the wall sprite
        objGameObjectSpriteRenderer.sprite = GetFurnitureSprite(objFurniture);
        objGameObjectSpriteRenderer.sortingLayerName = FURNITURESORTINGLAYER;

        //Register our callback so that our gameobject gets updated whenever the object's into changes
        objFurniture.RegisterOnChangedCallback(OnFurnitureChanged);
    }

    void OnFurnitureChanged(Furniture furniture)
    {
        Debug.Log("OnFurnitureChanged " + furniture);

        //Make sure the furniture graphics are correct.

        if (furnitureGameObjectMap.ContainsKey(furniture) == false)
        {
            Debug.LogError("OnFurnitureChanged -- trying to change visuals for furniture not in our map");
            return;
        }

        GameObject furnitureGameObject = furnitureGameObjectMap[furniture];
        furnitureGameObject.GetComponent<SpriteRenderer>().sprite = GetFurnitureSprite(furniture);

    }

    public Sprite GetFurnitureSprite(Furniture furniture)
    {
        Debug.Log("GetFurnitureSprite " + furniture);
        if (furniture.linksToNeighbour == false)
        {
            return furnitureSprites[furniture.objectType];
        }
        int x = furniture.tile.X;
        int y = furniture.tile.Y;

        //otherwise the sprite name is more complicated
        string spriteName = furniture.objectType;
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

        return furnitureSprites[spriteName];

    }


    public Sprite GetFurnitureSprite(string objectType)
    {
        Debug.Log("GetFurnitureSprite objectType " + objectType);

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

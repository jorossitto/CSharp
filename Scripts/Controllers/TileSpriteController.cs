using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileSpriteController : MonoBehaviour
{

    public Sprite floorSprite; //todo Fixme
    public Sprite emptySprite; //todo fixme
    const string TILESORTINGLAYER = "Tiles";
    Dictionary<Tile, GameObject> tileGameObjectMap;

    World world
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        //Tracks which gameobject is rendering which tile data.
        tileGameObjectMap = new Dictionary<Tile, GameObject>();

        //Create a gameobject for each of our tiles so they will show visually
        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                //Get the tile Data
                Tile tileData = world.GetTileAt(x, y);

                //Creates a new gameobject and adds it to our scene
                GameObject tileGameObject = new GameObject();
                //Add tile//Gameobject to dictionary
                tileGameObjectMap.Add(tileData, tileGameObject);
                tileGameObject.name = "Tile(" + x + "," + y + ")";
                tileGameObject.transform.position = new Vector3(tileData.X, tileData.Y, 0);
                tileGameObject.transform.SetParent(this.transform, true);

                //Add a sprite renderer and a default sprite for empty tiles
                SpriteRenderer tileSpriteRenderer = tileGameObject.AddComponent<SpriteRenderer>();
                tileSpriteRenderer.sprite = emptySprite;
                tileSpriteRenderer.sortingLayerName = TILESORTINGLAYER;
                //tileData.RegisterTileTypeChangedCallback((tile) => { OnTileTypeChanged(tile, tileGameObject); });

                //SetFloorSprite(tileData, tileSpriteRenderer);
            }
        }

        //Register our callback so that our gameobject gets updated whenever the tile changes
        world.RegisterTileChanged(OnTileChanged);
    }

    //Sets the floor sprite
    private void SetFloorSprite(Tile tileData, SpriteRenderer tileSpriteRenderer)
    {
        if (tileData.Type == TileType.Floor)
        {
            tileSpriteRenderer.sprite = floorSprite;
        }
    }

    //This is an example not currently used
    void DestroyAllTileGameObjects()
    {
        //this function might get called when we are changing floors/levels
        //We need to destroy all visual gameobjects but not the tile data
        while (tileGameObjectMap.Count > 0)
        {
            Tile tileData = tileGameObjectMap.Keys.First();
            GameObject tileGameObject = tileGameObjectMap[tileData];

            //Remove the pair from the map
            tileData.UnRegisterTileTypeChangedCallback(OnTileChanged);

            //Destroy visual gameobject
            Destroy(tileGameObject);
        }
        //Presumaly, after this function gets called we'd be calling another function
        // to build all gameobjects for the tiles on the floor/level
    }

    //Called whenever a tile's type gets changed
    void OnTileChanged(Tile tileData)
    {
        if (tileGameObjectMap.ContainsKey(tileData) == false)
        {
            Debug.LogError("Tile gameobject does not contrain the tileData");
            return;
        }

        GameObject tileGameObject = tileGameObjectMap[tileData];

        if (tileGameObject == null)
        {
            Debug.LogError("Tile gameobject is null");
            return;
        }

        if (tileData.Type == TileType.Floor)
        {
            tileGameObject.GetComponent<SpriteRenderer>().sprite = floorSprite;
        }
        else if (tileData.Type == TileType.Empty)
        {
            tileGameObject.GetComponent<SpriteRenderer>().sprite = emptySprite;
        }
        else
        {
            Debug.LogError("TileTypeChanged - Unrecognized tile type");
        }
    }

}

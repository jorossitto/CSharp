using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySpriteController : MonoBehaviour
{
    public GameObject InventoryUIPrefab;

    Dictionary<Inventory, GameObject> inventoryGameObjectMap;
    Dictionary<string, Sprite> inventorySprites;
    const string INVENTORY_SORTING_LAYER = "Inventory";

    World world
    {
        get { return WorldController.Instance.World; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();
        //Instantiate our dictionary that tracks which gameobject is rending which tile data
        inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();
        //Register our callback so our gameobject gets updated whenever the tile's type changes
        world.RegisterInventoryCreated(OnInventoryCreated);

        //check for preexisting inventory, which won't do the callback.
        foreach(string objectType in world.inventoryManager.inventories.Keys)
        {
            foreach (Inventory inventory in world.inventoryManager.inventories[objectType])
            {
                OnInventoryCreated(inventory);
            }
        }
    }

    private void LoadSprites()
    {
        inventorySprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Inventory");
        foreach (Sprite s in sprites)
        {
            //Debug.Log(s);
            inventorySprites[s.name] = s;
        }
    }

    public void OnInventoryCreated(Inventory inventory)
    {
        //Create a visual gameobject linked to this data
        //Debug.Log("OnCharacterCreated");

        //Todo FixMe does not consider multi tile objects or rotated objects

        //Creates a new gameobject and adds it to our scene
        GameObject inventoryGameObject = new GameObject();
        //Add tile//Gameobject to dictionary
        inventoryGameObjectMap.Add(inventory, inventoryGameObject);
        inventoryGameObject.name = inventory.objectType + "(" + inventory.tile.X + "," + inventory.tile.Y + ")";
        inventoryGameObject.transform.position = new Vector3(inventory.tile.X, inventory.tile.Y, 0);
        inventoryGameObject.transform.SetParent(this.transform, true);

        //Add a sprite renderer -> don't bother setting sprite because all tiles are empty
        SpriteRenderer characterSpriteRenderer = inventoryGameObject.AddComponent<SpriteRenderer>();

        //todo fixme assume that object must be a wall so use the hardcoded refrence to the wall sprite
        characterSpriteRenderer.sprite = inventorySprites[inventory.objectType];
        characterSpriteRenderer.sortingLayerName = INVENTORY_SORTING_LAYER;

        if(inventory.maxStackSize > 1)
        {
            //this is a stackable object so lets add the inventoryui component
            GameObject inventoryUIGameObject = Instantiate(InventoryUIPrefab);
            inventoryUIGameObject.transform.SetParent(inventoryGameObject.transform);
            inventoryUIGameObject.transform.localPosition = Vector3.zero;
            inventoryUIGameObject.GetComponentInChildren<Text>().text = inventory.stackSize.ToString();
        }

        //Register our callback so that our gameobject gets updated whenever the object's into changes
        //todo fixme add on changed callbacks
        inventory.RegisterInventoryChangedCallback(OnInventoryChanged);
    }

    void OnInventoryChanged(Inventory inventory)
    {
        
        //Debug.Log("OnCharacterChanged " + character);

        //Make sure the furniture graphics are correct.

        if (inventoryGameObjectMap.ContainsKey(inventory) == false)
        {
            Debug.LogError("OnInventoryChanged -- trying to change visuals for inventory not in our map");
            return;
        }

        GameObject inventoryGameObject = inventoryGameObjectMap[inventory];

        if (inventory.stackSize > 0)
        {
            Text text = inventoryGameObject.GetComponentInChildren<Text>();

            //todo fixme: if maxstacksize changed to/from 1, then we either need to create or destroy the text
            if (text != null)
            {
                text.text = inventory.stackSize.ToString();
            }

        }
        else
        {
            //This stack has gone to zero so remove the sprite
            Destroy(inventoryGameObject);
            inventoryGameObjectMap.Remove(inventory);
            inventory.UnRegisterInventoryChangedCallback(OnInventoryChanged);
        }

    }

}

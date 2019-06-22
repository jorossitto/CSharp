using UnityEngine;
using UnityEngine.UI;

public class MouseOverFurnitureType : MonoBehaviour
{
    //Every frame, this script checks to see which tile
    //is under the mouse and then updates the getcomponent text.text
    //parameter of the object is attached to

    Text text;

    MouseController mouseController;

    void Start()
    {
        text = GetComponent<Text>();
        if(text == null)
        {
            Debug.LogError("MouseOverTileTypeText: has no text");
            this.enabled = false;
            return;
        }
        mouseController = GameObject.FindObjectOfType<MouseController>();
        if(mouseController == null)
        {
            Debug.LogError("How do we not have an instance of mouse controller?");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Tile tile = mouseController.GetMouseOverTile();
        string stringDefault = "NULL";
        if(tile.furniture != null)
        {
            stringDefault = tile.furniture.objectType;
        }
        text.text = "Furniture Type: " + stringDefault;
    }
}

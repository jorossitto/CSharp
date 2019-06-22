using UnityEngine;
using UnityEngine.UI;

public class MouseOverRoomDetails : MonoBehaviour
{
    //Every frame, this script checks to see which tile
    //is under the mouse and then updates the getcomponent text.text
    //parameter of the object is attached to

    Text myText;
    MouseController mouseController;

    void Start()
    {
        myText = GetComponent<Text>();
        if(myText == null)
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

        if(tile == null || tile.room == null)
        {
            myText.text = "";
            return;
        }

        string tempString = "";
        foreach (string gas in tile.room.GetGasNames())
        {
            tempString += gas + ": " + tile.room.GetGasAmount(gas) + " (" + tile.room.GetGasPercentage(gas) * 100 + "%) ";
        }

        myText.text = tempString;
    }
}

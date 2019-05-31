using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class WorldController : MonoBehaviour
{
    
    public static WorldController Instance { get; protected set; }

    //The World and tile Data
    private World world;
    public World World { get => world; protected set => world = value; }

    Vector3 cameraPosition;

    // Use this for initialization
    void OnEnable ()
    {

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers");
        }

        Instance = this;

        //Create a world with empty tiles
        World = new World();

        //Center the camera
        cameraPosition = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);
        Camera.main.transform.position = cameraPosition;
    }

    private void Update()
    {
        //todo add pause/unpause speed controls, etc
        world.Update(Time.deltaTime);
    }


    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x);
        int y = Mathf.FloorToInt(coord.y);

        return WorldController.Instance.World.GetTileAt(x, y);
    }

}

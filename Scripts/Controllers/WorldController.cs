using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour
{
    const string SAVEFILE = "SaveGame00";

    public static WorldController Instance { get; protected set; }

    //The World and tile Data
    private World world;
    public World World { get => world; protected set => world = value; }

    static bool loadWorld = false;

    Vector3 cameraPosition;

    // Use this for initialization
    void OnEnable ()
    {

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers");
        }

        Instance = this;

        if(loadWorld)
        {
            loadWorld = false;
            CreateWorldFromSaveFile();
        }
        else
        {
            CreateEmptyWorld();
        }
        
    }

    private void Update()
    {
        //todo add pause/unpause speed controls, etc
        world.Update(Time.deltaTime);
    }

    /// <summary>
    /// Gets the tile at the unity-space coordinates
    /// </summary>
    /// <param name="coord">Unity world space coordinates</param>
    /// <returns>The tile at world coordinates</returns>
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + .5f);
        int y = Mathf.FloorToInt(coord.y + .5f);

        return World.GetTileAt(x, y);
    }

    public void NewWorldButton()
    {
        Debug.Log("New world BABY!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //CreateEmptyWorld();
    }

    public void SaveWorld()
    {
        Debug.Log("Save world button was clicked!");
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, world);
        writer.Close();
        Debug.Log(writer.ToString());
        PlayerPrefs.SetString(SAVEFILE, writer.ToString());
    }

    public void LoadWorld()
    {
        //Debug.Log("Load world button was clicked!");
        //Reload the scene to reset all data(and purge old refrences)
        loadWorld = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void CreateEmptyWorld()
    {
        //Create a world with empty tiles
        World = new World(100, 100);

        //Center the camera
        cameraPosition = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);
        Camera.main.transform.position = cameraPosition;
    }

    private void CreateWorldFromSaveFile()
    {
        //Debug.Log("CreateWorldFromSaveFile");
        //Create a world from our save file data
        //PlayerPrefs.SetString("SameGame00", writer.ToString());
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(PlayerPrefs.GetString(SAVEFILE));
        world = (World)serializer.Deserialize(reader);
        reader.Close();

        //Center the camera
        cameraPosition = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);
        Camera.main.transform.position = cameraPosition;
    }

}

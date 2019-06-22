using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    const string MOUSESCROLLWHEEL = "Mouse ScrollWheel";

    public GameObject circleCursorPrefab;

    //The world position of the mouse last frame
    Vector3 lastFramePosition;
    Vector3 currFramePosition;
    Vector3 mainCamera;

    //The world position start of our drag operation
    Vector3 dragStartPosition;
    List<GameObject> dragPreviewGameObjects;

    BuildModeController buildModeController;
    FurnitureSpriteController furnitureSpriteController;

    bool isDragging = false;

    enum MouseMode
    {
        SELECT,
        BUILD
    }

    MouseMode currentMode = MouseMode.SELECT;

    float y = 0f;
    float x = 0f;
    [SerializeField] float minZoom = 3f;
    [SerializeField] float maxZoom = 10f;




    // Start is called before the first frame update
    void Start()
    {
        //Cashe bulid mode controller
        buildModeController = GameObject.FindObjectOfType<BuildModeController>();
        furnitureSpriteController = GameObject.FindObjectOfType<FurnitureSpriteController>();

        //Get Starting location for camera
        y = Camera.main.transform.position.y;
        x = Camera.main.transform.position.x;

        //Initialize game object List
        dragPreviewGameObjects = new List<GameObject>();



    }
    /// <summary>
    /// Gets mouse position in world space.
    /// </summary>
    public Vector3 GetMousePosition()
    {
        return currFramePosition;
    }

    public Tile GetMouseOverTile()
    {
        return WorldController.Instance.GetTileAtWorldCoord(currFramePosition);
    }
    // Update is called once per frame
    void Update()
    {
        //Set Current Mouse Position
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;

        if(Input.GetKeyUp(KeyCode.Escape))
        {
            if(currentMode == MouseMode.BUILD)
            {
                currentMode = MouseMode.SELECT;
            }
            else if(currentMode == MouseMode.SELECT)
            {
                Debug.Log("Show game menu?");
            }
        }

        UpdateDragging(currFramePosition);


        MoveScreenWithMouseDrag(currFramePosition);
        MoveCameraWithWASD();

        //Zoom In and Out
        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis(MOUSESCROLLWHEEL);
        //set clamp to minzoom, maxzoom
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);

    }

    private void UpdateDragging(Vector3 currFramePosition)
    {
        // if we are over a ui element bail out
        if(EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        CleanupDragPreview();

        if(currentMode != MouseMode.BUILD)
        {
            return;
        }

        //Start Drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = currFramePosition;
            isDragging = true;
        }
        else if(isDragging == false)
        {
            dragStartPosition = currFramePosition;
        }

        if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.Escape))
        {
            //The right mouse button was released so we cancel the dragging
            isDragging = false;
        }

        if(buildModeController.IsObjectDraggable() == false)
        {
            dragStartPosition = currFramePosition;
        }

        //Setup Y
        int startY = Mathf.FloorToInt(dragStartPosition.y + .5f);
        int endY = Mathf.FloorToInt(currFramePosition.y + .5f);
        //Setup X
        int startX = Mathf.FloorToInt(dragStartPosition.x + .5f);
        int endX = Mathf.FloorToInt(currFramePosition.x + .5f);

        //Check Drag Direction
        CheckDragDirection(ref startX, ref endX);
        CheckDragDirection(ref startY, ref endY);



        //Display Drag Area
        DisplayDragArea(startY, endY, startX, endX);

        //End Drag and Create The Floor
        EndDragAndCreateFloor(startY, endY, startX, endX);
    }

    private void EndDragAndCreateFloor(int startY, int endY, int startX, int endX)
    {
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            //Loop through the tiles
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile tile = WorldController.Instance.World.GetTileAt(x, y);

                    if (tile != null)
                    {
                        //CallBuildModeController DoBuild
                        buildModeController.DoBuild(tile);
                    }
                }
            }
        }
    }

    private void DisplayDragArea(int startY, int endY, int startX, int endX)
    {
        //if (isDragging)
        //{
            //display preview of the drag area
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile tile = WorldController.Instance.World.GetTileAt(x, y);
                    if (tile != null)
                    {
                        //Display the building hint on top of this tiles position
                        if(buildModeController.buildMode == BuildMode.FURNITURE)
                        {
                            ShowFurnitureSpriteAtTile(buildModeController.buildModeObjectType, tile);
                        }
                        else
                        {
                            //Show the generic dragging visuals
                            GameObject CirclePrefab = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                            CirclePrefab.transform.SetParent(this.transform, true);
                            dragPreviewGameObjects.Add(CirclePrefab);
                        }
                    }
                }
            }
        //}
    }

    private void CleanupDragPreview()
    {
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject gameObject = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(gameObject);
        }
    }

    private static void CheckDragDirection(ref int startX, ref int endX)
    {
        if (endX < startX)
        {
            Swap(ref startX, ref endX);
        }
    }

    private void MoveScreenWithMouseDrag(Vector3 currFramePosition)
    {
        //handle screen moving with mouse drag
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 diff = lastFramePosition - currFramePosition;
            Camera.main.transform.Translate(diff);
        }

        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    private static void Swap(ref int startY, ref int endY)
    {
        //Swap
        int temp = endY;
        endY = startY;
        startY = temp;
    }

    private void MoveCameraWithWASD()
    {
        if (Input.GetKey("w"))
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y + .1f, -10);
        }
        if (Input.GetKey("s"))
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y - .1f, -10);
        }
        if (Input.GetKey("a"))
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x-.1f, Camera.main.transform.position.y, -10);
        }
        if (Input.GetKey("d"))
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x + .1f, Camera.main.transform.position.y, -10);
        }
    }

    void ShowFurnitureSpriteAtTile(string furnitureType, Tile tile)
    {
        //Show the generic dragging visuals
        GameObject gameObject = new GameObject();
        gameObject.transform.SetParent(this.transform, true);
        dragPreviewGameObjects.Add(gameObject);

        SpriteRenderer jobSpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        jobSpriteRenderer.sprite = furnitureSpriteController.GetFurnitureSprite(furnitureType);
        jobSpriteRenderer.sortingLayerName = Config.JOBSORTINGLAYER;

        if (WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, tile))
        {
            jobSpriteRenderer.color = new Color(.5f, 1f, .5f, .25f);
        }
        else
        {
            jobSpriteRenderer.color = new Color(1f, .5f, .5f, .25f);
        }
        Furniture furniturePrototype = tile.world.furniturePrototypes[furnitureType];
        gameObject.transform.position = new Vector3(tile.X + (furniturePrototype.width - 1) / 2f, tile.Y + (furniturePrototype.width - 1) / 2f, 0);
    }

    public void StartBuildMode()
    {
        currentMode = MouseMode.BUILD;
    }
}

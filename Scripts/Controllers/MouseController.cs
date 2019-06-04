using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.Events;

public class MouseController : MonoBehaviour
{
    public GameObject circleCursorPrefab;

    //The world position of the mouse last frame
    Vector3 lastFramePosition;
    Vector3 currFramePosition;
    Vector3 mainCamera;

    //The world position start of our drag operation
    Vector3 dragStartPosition;
    List<GameObject> dragPreviewGameObjects;

    float y = 0f;
    float x = 0f;
    [SerializeField] float minZoom = 3f;
    [SerializeField] float maxZoom = 10f;

    const string MOUSESCROLLWHEEL = "Mouse ScrollWheel";


    // Start is called before the first frame update
    void Start()
    {
        //Get Starting location for camera
        y = Camera.main.transform.position.y;
        x = Camera.main.transform.position.x;

        //Initialize game object List
        dragPreviewGameObjects = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        //Set Current Mouse Position
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;
        //Debug.Log(currFramePosition.x);

        //UpdateCursorPosition(currFramePosition);

        CheckAndCalculateBuildingRectangle(currFramePosition);

        MoveScreenWithMouseDrag(currFramePosition);
        MoveCameraWithWASD();

        //Zoom In and Out
        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis(MOUSESCROLLWHEEL);
        //set clamp to minzoom, maxzoom
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);

    }

    private void CheckAndCalculateBuildingRectangle(Vector3 currFramePosition)
    {
        // if we are over a ui element bail out
        if(EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        //Start Drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = currFramePosition;
        }

        //Setup Y
        int startY = Mathf.FloorToInt(dragStartPosition.y);
        int endY = Mathf.FloorToInt(currFramePosition.y);
        //Setup X
        int startX = Mathf.FloorToInt(dragStartPosition.x);
        int endX = Mathf.FloorToInt(currFramePosition.x);

        //Check Drag Direction
        CheckDragDirection(ref startX, ref endX);
        CheckDragDirection(ref startY, ref endY);

        //Cleanup Drag Prieview
        CleanupDragPreview();

        //Display Drag Area
        DisplayDragArea(startY, endY, startX, endX);

        //End Drag and Create The Floor
        EndDragAndCreateFloor(startY, endY, startX, endX);
    }

    private void EndDragAndCreateFloor(int startY, int endY, int startX, int endX)
    {
        if (Input.GetMouseButtonUp(0))
        {
            BuildModeController buildModeController = GameObject.FindObjectOfType<BuildModeController>();
            //Debug.Log("EndDragAndCreateFloor");
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
        if (Input.GetMouseButton(0))
        {
            //Debug.Log("DisplayDragArea");
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile tile = WorldController.Instance.World.GetTileAt(x, y);
                    if (tile != null)
                    {
                        //Display the Circle Cursor
                        GameObject CirclePrefab = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                        CirclePrefab.transform.SetParent(this.transform, true);
                        dragPreviewGameObjects.Add(CirclePrefab);
                    }
                }
            }
        }
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
}

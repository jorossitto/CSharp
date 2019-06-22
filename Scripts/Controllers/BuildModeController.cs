using UnityEngine;

public enum BuildMode
{
    FLOOR,
    FURNITURE,
    DECONSTRUCT
}
public class BuildModeController : MonoBehaviour
{
    public BuildMode buildMode = BuildMode.FLOOR;
    TileType buildModeTile = TileType.Floor;
    public string buildModeObjectType;

    public bool IsObjectDraggable()
    {
        if(buildMode == BuildMode.FLOOR ||buildMode == BuildMode.DECONSTRUCT )
        {
            //floors are draggable
            return true;
        }

        Furniture furniturePrototype =WorldController.Instance.World.furniturePrototypes[buildModeObjectType];
        return furniturePrototype.width == 1 && furniturePrototype.height == 1;
    }

    public void SetModeBuildFloor()
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = TileType.Floor;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetModeBulldoze()
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = TileType.Empty;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetModeBuildFurniture(string objectType)
    {
        //Wall is not a tile it is Furniture that exists on top of a tile
        buildMode = BuildMode.FURNITURE;
        buildModeObjectType = objectType;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetModeDeconstruct()
    {
        buildMode = BuildMode.DECONSTRUCT;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }


    public void DoPathfindingTest()
    {
        WorldController.Instance.World.SetupPathFindingExample();
        //PathTileGraph tileGraph = new PathTileGraph(WorldController.Instance.World);
    }
    public void DoBuild(Tile tile)
    {
        //Debug.Log("DoBuild");
        if (buildMode == BuildMode.FURNITURE)
        {
            //create the Furniture and assign it to the tile

            //Can we burild the furniture in the selected tile?
            string furnitureType = buildModeObjectType;
            if (WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, tile) && tile.pendingFurnitureJob == null)
            {
                //this is a valid tile
                //add the job to the queue
                Job job;
                
                if(WorldController.Instance.World.furnitureJobPrototypes.ContainsKey(furnitureType))
                {
                    //Make a clone of the job prototype
                    job = WorldController.Instance.World.furnitureJobPrototypes[furnitureType].Clone();
                    //assign the correct tile
                    job.tile = tile;
                }
                else
                {
                    Debug.LogError("There is no furniture job prototype for " + furnitureType);
                    job = new Job(tile, furnitureType, FurnitureActions.JobCompleteFurnitureBuilding, .1f, null);
                }

                job.furniturePrototype = WorldController.Instance.World.furniturePrototypes[furnitureType];

                //Fixme I don't like having to manually set flags to prevent conflicts
                //to easy to forget to set and clear
                tile.pendingFurnitureJob = job;
                job.RegisterJobCancelCallback((theJob) => { theJob.tile.pendingFurnitureJob = null; });
                WorldController.Instance.World.jobQueue.Enqueue(job);
                //Debug.Log("Job Queue Size: " + WorldController.Instance.World.jobQueue.Count);
            }
        }
        else if (buildMode == BuildMode.FLOOR)
        {
            //We are in tile changing mode
            tile.Type = buildModeTile;
        }
        else if(buildMode == BuildMode.DECONSTRUCT)
        {
            //Todo
            if(tile.furniture != null)
            {
                tile.furniture.Deconstruct();
            }
            
        }
        else
        {
            Debug.Log("Unimplemented build mode");
        }
    }
}

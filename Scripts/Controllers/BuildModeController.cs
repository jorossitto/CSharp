using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.Events;

public class BuildModeController : MonoBehaviour
{

    TileType buildModeTile = TileType.Floor;
    bool BuildModeIsObjects = false;
    string buildModeObjectType;

    public void SetModeBuildFloor()
    {
        BuildModeIsObjects = false;
        buildModeTile = TileType.Floor;
    }

    public void SetModeBulldoze()
    {
        BuildModeIsObjects = false;
        buildModeTile = TileType.Empty;
    }

    public void SetModeBuildFurniture(string objectType)
    {
        //Wall is not a tile it is Furniture that exists on top of a tile
        BuildModeIsObjects = true;
        buildModeObjectType = objectType;
    }

    public void DoPathfindingTest()
    {
        WorldController.Instance.World.SetupPathFindingExample();
        //PathTileGraph tileGraph = new PathTileGraph(WorldController.Instance.World);
    }
    public void DoBuild(Tile tile)
    {
        //Debug.Log("DoBuild");
        if (BuildModeIsObjects == true)
        {
            //create the Furniture and assign it to the tile

            //Can we burild the furniture in the selected tile?
            string furnitureType = buildModeObjectType;
            if (WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, tile) && tile.pendingFurnitureJob == null)
            {
                //this is a valid tile
                //add the job to the queue
                Job job = new Job(tile, furnitureType, (theJob) =>
                {
                    WorldController.Instance.World.PlaceFurniture(furnitureType, theJob.tile);
                    tile.pendingFurnitureJob = null;
                }
                );
                //Fixme I don't like having to manually set flags to prevent conflicts
                //to easy to forget to set and clear
                tile.pendingFurnitureJob = job;
                job.RegisterJobCancelCallback((theJob) => { theJob.tile.pendingFurnitureJob = null; });
                WorldController.Instance.World.jobQueue.Enqueue(job);
                //Debug.Log("Job Queue Size: " + WorldController.Instance.World.jobQueue.Count);
            }
        }
        else
        {
            //We are in tile changing mode
            tile.Type = buildModeTile;
        }
    }
}

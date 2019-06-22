using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobSpriteController : MonoBehaviour
{
    //This bare bones controller is mostly just going to piggyback on furnituresprite controller 
    //because we don't yet fully know what our job system is going to look like in the end

    FurnitureSpriteController furnitureSpriteController;
    Dictionary<Job, GameObject> jobGameObjectMap;
    const string JOBSORTINGLAYER = Config.JOBSORTINGLAYER;

    // Start is called before the first frame update
    void Start()
    {
        jobGameObjectMap = new Dictionary<Job, GameObject>();
        furnitureSpriteController = GameObject.FindObjectOfType<FurnitureSpriteController>();

        //Todo Fixme there is no job queue yet
        WorldController.Instance.World.jobQueue.RegisterJobCreationCallback(OnJobCreated);
    }

    void OnJobCreated(Job job)
    {
        if(job.jobObjectType == null)
        {
            //This job does not really have an associated sprite so no need to render
            return;
        }

        //Debug.Log("OnJobCreated");
        //todo fixme we can only do furniture building jobs
        //Todo sprite


        if(jobGameObjectMap.ContainsKey(job))
        {
            Debug.LogError("OnJobCreated: Job created for a job that already exists most likely job is requeued");
            return;
        }

        //Creates a new gameobject and adds it to our scene
        GameObject jobGameObject = new GameObject();

        //Add tile//Gameobject to dictionary
        jobGameObjectMap.Add(job, jobGameObject);
        jobGameObject.name = "JOB_" + job.jobObjectType + "(" + job.tile.X + "," + job.tile.Y + ")";
        jobGameObject.transform.position = new Vector3(job.tile.X + (job.furniturePrototype.width - 1)/2f, job.tile.Y+(job.furniturePrototype.width - 1) / 2f, 0);
        jobGameObject.transform.SetParent(this.transform, true);

        SpriteRenderer jobSpriteRenderer = jobGameObject.AddComponent<SpriteRenderer>();
        jobSpriteRenderer.sprite = furnitureSpriteController.GetFurnitureSprite(job.jobObjectType);
        jobSpriteRenderer.color = new Color(.5f, 1f, .5f, .25f);
        jobSpriteRenderer.sortingLayerName = JOBSORTINGLAYER;

        if (job.jobObjectType == Config.DOOR)
        {
            //By default the door graphic is ment for walls to the east and west
            //Check to see if we actually have a wall north south and rotate by 90d
            Tile north = job.tile.world.GetTileAt(job.tile.X, job.tile.Y + 1);
            Tile south = job.tile.world.GetTileAt(job.tile.X, job.tile.Y - 1);
            if (north != null && south != null
                && north.furniture != null && south.furniture != null
                && north.furniture.objectType == Config.WALL && south.furniture.objectType == Config.WALL)
            {
                jobGameObject.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        job.RegisterJobCompleteCallback(OnJobEnded);
        job.RegisterJobCancelCallback(OnJobEnded);
    }

    void OnJobEnded(Job job)
    {
        //This executes weather a job was completed or canceled
        //todo fixme we can only do furniture building jobs

        //todo delete sprites

        GameObject jobGameObject = jobGameObjectMap[job];
        job.UnregisterJobCompleteCallback(OnJobEnded);
        job.UnregisterJobCancelCallback(OnJobEnded);
        Destroy(jobGameObject);
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobSpriteController : MonoBehaviour
{
    //This bare bones controller is mostly just going to piggyback on furnituresprite controller 
    //because we don't yet fully know what our job system is going to look like in the end

    FurnitureSpriteController furnitureSpriteController;
    Dictionary<Job, GameObject> jobGameObjectMap;
    const string JOBSORTINGLAYER = "Jobs";

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
        Debug.Log("OnJobCreated");
        //todo fixme we can only do furniture building jobs
        //Todo sprite
        //Creates a new gameobject and adds it to our scene
        GameObject jobGameObject = new GameObject();
        //Add tile//Gameobject to dictionary
        jobGameObjectMap.Add(job, jobGameObject);
        jobGameObject.name = "JOB_" + job.jobObjectType + "(" + job.tile.X + "," + job.tile.Y + ")";
        jobGameObject.transform.position = new Vector3(job.tile.X, job.tile.Y, 0);
        jobGameObject.transform.SetParent(this.transform, true);

        SpriteRenderer jobSpriteRenderer = jobGameObject.AddComponent<SpriteRenderer>();
        jobSpriteRenderer.sprite = furnitureSpriteController.GetFurnitureSprite(job.jobObjectType);
        jobSpriteRenderer.color = new Color(.5f, 1f, .5f, .25f);
        jobSpriteRenderer.sortingLayerName = JOBSORTINGLAYER;

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

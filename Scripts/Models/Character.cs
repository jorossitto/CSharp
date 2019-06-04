using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Character:IXmlSerializable
{
    public float X
    {
        get
        {
            return Mathf.Lerp(currTile.X, nextTile.X, movementPercentage);
        }
    }

    public float Y
    {
        get
        {
            return Mathf.Lerp(currTile.Y, nextTile.Y, movementPercentage);
        }
    }

    public Tile currTile
    {
        get; protected set;
    }

    
    Tile destTile; //if we aren't moving then destTile = curtile
    Tile nextTile; //The next tile in the pathfinding sequence
    PathAStar pathAStar;

    
    float movementPercentage; //goes from 0 to 1 as we move from currTile to destTile

    //Tiles per second
    float speed = 2f;

    Action<Character> callbackCharacterChanged;

    Job myJob;

    public Character()
    {
        //Use only for serialization
    }

    public Character(Tile tile)
    {
        currTile = destTile = nextTile = tile;
    }

    void UpdateDoJob(float deltaTime)
    {
        //Debug.Log("Character update");
        if (myJob == null)
        {
            //grab a new job
            myJob = currTile.world.jobQueue.Dequeue();
            if (myJob != null)
            {
                //we have a job!

                //Todo fixme make sure the job is reachable
                destTile = myJob.tile;
                myJob.RegisterJobCancelCallback(OnJobEnded);
                myJob.RegisterJobCompleteCallback(OnJobEnded);
            }
        }


        //if (pathAStar.Length() == 1 && pathAStar != null) // we are adjacent to the jobsite
        //Are we there yet?
        if (currTile == destTile)
        {

            if (myJob != null)
            {
                myJob.DoWork(deltaTime);
            }
        }

    }

    public void AbandonJob()
    {
        nextTile = destTile = currTile;
        pathAStar = null;
        currTile.world.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    void UpdateDoMovement(float deltaTime)
    {
        if(currTile == destTile)
        {
            pathAStar = null;
            return; // we are already there
        }

        if(nextTile == null || nextTile == currTile)
        {
            //get the next tile from the path finder;
            if(pathAStar == null||pathAStar.Length() == 0)
            {
                //generate path to our destination
                pathAStar = new PathAStar(currTile.world, currTile, destTile); // this will calculate a path from current to destination
                if(pathAStar.Length() == 0)
                {
                    Debug.LogError("PathAStar returned no path to destination");
                    AbandonJob();
                    pathAStar = null;
                    return;
                }
            }
            //Grab the next waypoint
            nextTile = pathAStar.Dequeue();
            if(nextTile == currTile)
            {
                Debug.LogError("UpdateDoMovement - Next tile is curr tile");
            }
        }

        //if(pathAStar.Length() == 1)
        //{
        //    return;
        //}

        //At this point we should have a valid tile to move to
        //Whats the total distance from point a to point b
        float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.X - nextTile.X, 2) + Mathf.Pow(currTile.Y - nextTile.Y, 2));
        //How much distance can we travel this update?
        float distThisFrame = speed * deltaTime;
        //How much is that in terms of percentage to our destination?
        float percThisFram = distThisFrame / distToTravel;
        //Add that to overall percentage traveled
        movementPercentage += percThisFram;
        if (movementPercentage >= 1)
        {
            //We have reached our destination

            //ToDo: Get the next Tile from the pathfinding system.
            //If there are no more tiles then we have reached our destination
            currTile = nextTile;
            movementPercentage = 0;
            //Fixme do we want to retain any overshot movement?
        }


    }
    public void Update(float deltaTime)
    {
        UpdateDoJob(deltaTime);
        UpdateDoMovement(deltaTime);
        if (callbackCharacterChanged != null)
        {
            callbackCharacterChanged(this);
        }

    }



    public void SetDestination(Tile tile)
    {
        if(currTile.IsNeighbour(tile, true) == false)
        {
            Debug.Log("Character::SetDestination  -- Our Destination tile isn't our neighbour");
        }

        destTile = tile;
    }

    public void RegisterOnChangedCallback(Action<Character> callback)
    {
        callbackCharacterChanged += callback;
    }

    public void UnregisterOnChangedCallback(Action<Character> callback)
    {
        callbackCharacterChanged -= callback;
    }

    public void OnJobEnded(Job job)
    {
        //job completed or was canceled.
        if(job != myJob)
        {
            Debug.LogError("Character being told about job that is not his. You forgot to unregister something.");
            return;
        }
        myJob = null;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    ///                                                Saving and Loading
    /// 
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public XmlSchema GetSchema()
    {
        return null;
    }
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", currTile.X.ToString());
        writer.WriteAttributeString("Y", currTile.Y.ToString());
    }
    public void ReadXml(XmlReader reader)
    {

    }
}

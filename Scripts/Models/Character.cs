using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Character
{
    public float X
    {
        get
        {
            return Mathf.Lerp(currTile.X, destTile.X, movementPercentage);
        }
    }

    public float Y
    {
        get
        {
            return Mathf.Lerp(currTile.Y, destTile.Y, movementPercentage);
        }
    }

    public Tile currTile
    {
        get; protected set;
    }

    //if we aren't moving then destTile = curtile
    Tile destTile;

    //goes from 0 to 1 as we move from currTile to destTile
    float movementPercentage;

    //Tiles per second
    float speed = 2f;

    Action<Character> callbackCharacterChanged;

    Job myJob;

    public Character(Tile tile)
    {
        currTile = destTile = tile;
    }
    public void Update(float deltaTime)
    {
        //Debug.Log("Character update");
        if(myJob == null)
        {
            //grab a new job
            myJob = currTile.world.jobQueue.Dequeue();
            if(myJob != null)
            {
                //we have a job!
                destTile = myJob.tile;
                myJob.RegisterJobCancelCallback(OnJobEnded);
                myJob.RegisterJobCompleteCallback(OnJobEnded);
            }
        }

        //Are we there yet?
        if (currTile == destTile)
        {
            if(myJob != null)
            {
                myJob.DoWork(deltaTime);
            }
            return;
        }
        //Whats the total distance from point a to point b
        float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.X - destTile.X, 2) + Mathf.Pow(currTile.Y - destTile.Y, 2));
        //How much distance can we travel this update?
        float distThisFrame = speed * deltaTime;
        //How much is that in terms of percentage to our destination?
        float percThisFram = distThisFrame / distToTravel;
        //Add that to overall percentage traveled
        movementPercentage += percThisFram;
        if(movementPercentage >= 1)
        {
            currTile = destTile;
            movementPercentage = 0;
            //Fixme do we want to retain any overshot movement?
        }

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
}

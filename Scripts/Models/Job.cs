using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Job 
{
    //This class holds info for a queued job which can include things like
    //placing furniture, moving inventory, working, fighting enemies

    public Tile tile
    {
        get; protected set;
    }

    float jobTime;
    public string jobObjectType
    {
        get; protected set;
    }

    Action<Job> callBackJobComplete;
    Action<Job> callBackJobCanceled;

    public Job(Tile tile, string jobObjectType,  Action<Job> callBackJobComplete, float jobTime = 1f)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.callBackJobComplete += callBackJobComplete;
        this.jobTime = jobTime;
    }

    public void RegisterJobCompleteCallback(Action<Job> callback)
    {
        callBackJobComplete += callback;
    }

    public void UnregisterJobCompleteCallback(Action<Job> callback)
    {
        callBackJobCanceled -= callback;
    }

    public void RegisterJobCancelCallback(Action<Job> callback)
    {
        callBackJobCanceled += callback;
    }

    public void UnregisterJobCancelCallback(Action<Job> callback)
    {
        callBackJobCanceled -= callback;
    }

    public void DoWork(float workTime)
    {
        jobTime -= workTime;

        if(jobTime<= 0)
        {
            if(callBackJobComplete != null)
            {
                callBackJobComplete(this);
            }
        }
    }

    public void CancelJob()
    {
        if(callBackJobCanceled != null)
        {
            callBackJobCanceled(this);
        }
    }
}

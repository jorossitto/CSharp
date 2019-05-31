using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class JobQueue
{
    Queue<Job> jobQueue;
    Action<Job> callBackJobCreated;

    public JobQueue()
    {
        jobQueue = new Queue<Job>();
    }

    public void Enqueue(Job job)
    {
        jobQueue.Enqueue(job);
        //todo fixme do callbacks

        if(callBackJobCreated != null)
        {
            callBackJobCreated(job);
        }
    }
    public Job Dequeue()
    {
        if(jobQueue.Count == 0)
        {
            return null;
        }

        return jobQueue.Dequeue();
    }

    public void RegisterJobCreationCallback(Action<Job> callback)
    {
        callBackJobCreated += callback;
    }

    public void UnRegisterJobCreationCallback(Action<Job> callback)
    {
        callBackJobCreated -= callback;
    }
}

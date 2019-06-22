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
        Debug.Log("Adding job to queue.  Existing queue size: " + jobQueue.Count);
        if(job.jobTime < 0 )
        {
            //Job has a negitive job time so instantly complete it instead of enqueing
            job.DoWork(0);
            return;
        }

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

    public void Remove(Job job)
    {
        //Todo check docs to see if theres a less memory swappy solution
        List<Job> jobs = new List<Job>(jobQueue);

        if(jobs.Contains(job) == false )
        {
            //Debug.LogError("Trying to remove a job that doesn't exist on the queue");
            //Most likely the job is not on the queue because the character has it
            return;
        }

        jobs.Remove(job);
        jobQueue = new Queue<Job>(jobs);
    }
}

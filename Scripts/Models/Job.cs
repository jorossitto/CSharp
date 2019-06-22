using System.Collections.Generic;
using System;
using UnityEngine;

public class Job 
{
    //This class holds info for a queued job which can include things like
    //placing furniture, moving inventory, working, fighting enemies

    public Tile tile;

    public float jobTime
    {
        get; protected set;
    }
    public string jobObjectType
    {
        get; protected set;
    }

    public Furniture furniturePrototype;

    public bool acceptsAnyInventoryItem = false;

    Action<Job> callBackJobComplete;
    Action<Job> callBackJobCanceled;
    Action<Job> callBackJobWorked;

    public bool canTakeFromStockpile = true;

    public Dictionary<string, Inventory> inventoryRequirements;

    public Job(Tile tile, string jobObjectType,  Action<Job> callBackJobComplete, float jobTime, Inventory[] inventoryRequirements)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.callBackJobComplete += callBackJobComplete;
        this.jobTime = jobTime;
        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if(inventoryRequirements != null)
        {
            foreach (Inventory inventory in inventoryRequirements)
            {
                this.inventoryRequirements[inventory.objectType] = inventory.Clone();
            }
        }

    }

    protected Job (Job other)
    {
        this.tile = other.tile;
        this.jobObjectType = other.jobObjectType;
        this.callBackJobComplete = other.callBackJobComplete;
        this.jobTime = other.jobTime;
        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements != null)
        {
            foreach (Inventory inventory in other.inventoryRequirements.Values)
            {
                this.inventoryRequirements[inventory.objectType] = inventory.Clone();
            }
        }
    }
    virtual public Job Clone()
    {
        return new Job(this);
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

    public void RegisterJobWorkedCallback(Action<Job> callback)
    {
        callBackJobWorked += callback;
    }

    public void UnregisterJobWorkedCallback(Action<Job> callback)
    {
        callBackJobWorked -= callback;
    }


    public void DoWork(float workTime)
    {
        //Check to make sure we actually have everything we need
        //if not don't register the work time

        if(HasAllMaterial() == false)
        {
            //Debug.LogError("Tried to do work o a job that doesn't have all the materials");

            //Job can't actually be worked but still call the callbacks
            if (callBackJobWorked != null)
            {
                callBackJobWorked(this);
            }
            return;
        }

        jobTime -= workTime;

        if(callBackJobWorked != null)
        {
            callBackJobWorked(this);
        }

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

        tile.world.jobQueue.Remove(this);
    }

    public bool HasAllMaterial()
    {
        foreach(Inventory inventory in inventoryRequirements.Values)
        {
            if(inventory.maxStackSize > inventory.stackSize)
            {
                return false;
            }
        }
        return true;
    }

    public int DesiresInventoryType(Inventory inventory)
    {
        if(acceptsAnyInventoryItem)
        {
            return inventory.maxStackSize;
        }

        if(inventoryRequirements.ContainsKey(inventory.objectType) ==  false)
        {
            return 0;
        }

        if(inventoryRequirements[inventory.objectType].stackSize >= inventoryRequirements[inventory.objectType].maxStackSize)
        {
            //we already have enough
            return 0;
        }

        //The inventory is of a type we want and we still need more
        return inventoryRequirements[inventory.objectType].maxStackSize - inventoryRequirements[inventory.objectType].stackSize;
    }

    public Inventory GetFirstDesiredInventory()
    {
        foreach(Inventory inventory in inventoryRequirements.Values)
        {
            if(inventory.maxStackSize > inventory.stackSize)
            {
                return inventory;
            }
        }

        return null;
    }

}

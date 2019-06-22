using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FurnitureActions
{
    //This file contains code that will be moved into lua in the future
    const float doorOpenSpeed = 1f;
    const float doorCloseSpeed = -1f;
    const string O2 = "O2";

    public static void DoorUpdateAction(Furniture furniture, float deltaTime)
    {
        //Debug.Log("DoorUpdateAction " + furniture.furnitureParamaters[Config.OPENNESS]);

        if (furniture.GetParameter(Config.IS_OPENING) >= 1)
        {
            furniture.ChangeParameter(Config.OPENNESS, deltaTime * doorOpenSpeed);
            if (furniture.GetParameter(Config.OPENNESS) >= 1)
            {
                furniture.SetParameter(Config.IS_OPENING, 0);
            }
        }
        else
        {
            furniture.ChangeParameter(Config.OPENNESS, deltaTime * doorCloseSpeed);
        }

        furniture.SetParameter(Config.OPENNESS, Mathf.Clamp01(furniture.GetParameter(Config.OPENNESS)));

        if (furniture.callBackOnChanged != null)
        {
            furniture.callBackOnChanged(furniture);
        }

    }

    public static Enterability DoorIsEnterable(Furniture furniture)
    {
        //Debug.Log("DoorIsEnterable");
        furniture.SetParameter(Config.IS_OPENING, 1);

        if (furniture.GetParameter(Config.OPENNESS) >= 1)
        {
            return Enterability.Yes;
        }

        return Enterability.Soon;
    }

    public static void JobCompleteFurnitureBuilding(Job theJob)
    {
        WorldController.Instance.World.PlaceFurniture(theJob.jobObjectType, theJob.tile);
        theJob.tile.pendingFurnitureJob = null;
    }

    public static Inventory[] StockpileGetItemsFromFilter()
    {
        //todo fixme: should read from some kind of ui for this particular stockpile

        //Since jobs copy arrays automatically we could already have an inventory[] prepaired and just return that as an example filter
        return new Inventory[1] { new Inventory(Config.STEEL_PLATE, 50, 0) };

    }
    public static void StockpileUpdateAction(Furniture furniture, float deltaTime)
    {
        //Debug.Log("StockpileUpdateAction");

        //We need to ensure that we have a job on the queue asking for:
        //If we are empty: that any loose inventory be brought to us
        //If we have something:
        //if we are below the max stacksize: bring us stuff

        //Todo fixme: this function doesn't need to run each update
        //Only run when:
        //              --gets created
        //              --good gets delivered
        //              --good gets picked up
        //              --Ui filter of allowed items gets changed

        if (furniture.tile.inventoryTile != null && furniture.tile.inventoryTile.stackSize >= furniture.tile.inventoryTile.maxStackSize)
        {
            //we are full
            furniture.ClearJobs();
            return;
        }

        //Maybe we already have a job Queued
        if (furniture.JobCount() > 0)
        {
            //Cool, all done
            return;
        }

        //Currently not full but we don't have a job either
        //Two possibilites Some Inventory or No inventory

        //Third possiblity something is wrong
        if (furniture.tile.inventoryTile != null && furniture.tile.inventoryTile.stackSize == 0)
        {
            Debug.LogError("Stockpile has a zero-size stack. This is clearly wrong!");
            furniture.ClearJobs();
            return;
        }

        //Todo fixme: make stockpiles into multitile furniture
        //Also fix holes in stockpiles
        Inventory[] itemsDesired;

        if (furniture.tile.inventoryTile == null)
        {
            //Debug.Log("furn.tile.inventory == null");
            //We are empty -- just ask for anything
            Debug.Log("Creating job for new stack");
            itemsDesired = StockpileGetItemsFromFilter();
        }
        else
        {
            Debug.Log("Creating job for existing stack");
            //We have a stack of something but we are not full
            Inventory desiredInventory = furniture.tile.inventoryTile.Clone();
            desiredInventory.maxStackSize -= desiredInventory.stackSize;
            desiredInventory.stackSize = 0;
            itemsDesired = new Inventory[] { desiredInventory };
        }

        Job job = new Job(furniture.tile, null, null, 0f, itemsDesired);
        //todo fixme: add stockpile priorties to take from a lower priority stockpile
        job.canTakeFromStockpile = false;
        job.RegisterJobWorkedCallback(StockpileJobWorked);
        furniture.AddJob(job);
    }

    static void StockpileJobWorked(Job job)
    {
        job.tile.furniture.RemoveJob(job);

        //todo fixme: change this when we figure out what we are doing with the all/any pickup job

        foreach (Inventory inventory in job.inventoryRequirements.Values)
        {
            if (inventory.stackSize > 0)
            {
                job.tile.world.inventoryManager.PlaceInventory(job.tile, inventory);
                return;
            }
        }
    }
    public static void OxygenGeneratorUpdateAction(Furniture furniture, float deltaTime)
    {
        if(furniture.tile.room.GetGasAmount(O2) < .20f)
        {
            furniture.tile.room.ChangeGas(O2, 0.01f * deltaTime); //Todo fixme: Replace hardcoded value
            //Todo: consume power
        }
        else
        {
            //Todo: stand-by electric usage
        }
    }
}

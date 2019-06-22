using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

//Test
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

    Tile _destTile;
    //if we aren't moving then destTile = curtile
    Tile destTile
    {
        get
        {
            return _destTile;
        }

        set
        {
            if(_destTile != value)
            {
                _destTile = value;
                pathAStar = null; //if this is a new destination, then we need to invalidate pathfinding
            }
        }
    }
    Tile nextTile; //The next tile in the pathfinding sequence
    PathAStar pathAStar;

    
    float movementPercentage; //goes from 0 to 1 as we move from currTile to destTile

    //Tiles per second
    float speed = 2f;

    Action<Character> callbackCharacterChanged;

    Job myJob;

    //Item we are carrying(not gear or equip)
    public Inventory myInventory;

    public Character()
    {
        //Use only for serialization
    }

    public Character(Tile tile)
    {
        currTile = destTile = nextTile = tile;
    }

    void GetNewJob()
    {
        //grab a new job
        myJob = currTile.world.jobQueue.Dequeue();

        if(myJob == null)
        {
            return;
        }

        destTile = myJob.tile;
        myJob.RegisterJobCancelCallback(OnJobEnded);
        myJob.RegisterJobCompleteCallback(OnJobEnded);

        //Check if the job tile is reachable.
        //Note we might not be pathing to it right away due to materials
        //but we still need to verifiy that the final location can be reached
        //generate path to our destination
        pathAStar = new PathAStar(currTile.world, currTile, destTile); // this will calculate a path from current to destination
        if (pathAStar.Length() == 0)
        {
            Debug.LogError("PathAStar returned no path to target job tile");
            AbandonJob();
            destTile = currTile;
        }
    }

    void UpdateDoJob(float deltaTime)
    {
        //Debug.Log("Character update");
        //Do I have a job
        if (myJob == null)
        {
            GetNewJob();
            if (myJob == null)
            {
                //There was no job on the queue so just return
                destTile = currTile;
                return;
            }
        }
        //We have a job and its reachable
        //Step 1 does the job have all the materials it needs?
        if(myJob.HasAllMaterial() == false)
        {
            //no, we are missing something!

            //Step 2: Are we carrying anything that the job location wants?
            if(myInventory != null)
            {
                if(myJob.DesiresInventoryType(myInventory) > 0)
                {
                    //If so deliver the goods
                    //Walk to the job tile and drop off the stack into the job
                    if(currTile == myJob.tile)
                    {
                        //We are at the job's site, so drop the inventory
                        currTile.world.inventoryManager.PlaceInventory(myJob, myInventory);
                        //This will call all cbjobworked callbacks, because even though we aren't progressing it might want to do something with the 
                        //fact that the requirements are being met
                        myJob.DoWork(0); 

                        //Are we still Carrying Things?
                        if(myInventory.stackSize == 0)
                        {
                            myInventory = null;
                        }
                        else
                        {
                            Debug.LogError("Character is still carrying inventory which shouldn't be (Leaking inventory)");
                            myInventory = null;
                        }

                    }
                    else
                    {
                        //We still need to walk to the job site
                        destTile = myJob.tile;
                        return; //Nothing to do 
                    }
                    
                }
                else
                {
                    //We are carrying something, but the job doesn't want it!
                    //Dump the inventory at our feet.(or wherever is closest)
                    //Todo fixme -> walk to the nearest empty tile and dump it there
                    if(currTile.world.inventoryManager.PlaceInventory(currTile, myInventory)==false)
                    {
                        Debug.LogError("Character tried to dump inventory into an invalid tile(maybe something is already here)");
                        //todo fixme: for the sake of continuing on we are still going to dump our inventory here but this means
                        //we are leaking inventory and is permantly lost
                        myInventory = null;

                    }
                }
            }
            else
            {
                //At this point, the job still requires inventory, but we aren't carrying it

                //Are we standing on a tile with goods that are desired by the job?
                if(currTile.inventoryTile != null 
                    && (myJob.canTakeFromStockpile || currTile.furniture == null || currTile.furniture.IsStockpile() == false)
                    && myJob.DesiresInventoryType(currTile.inventoryTile) > 0)
                {
                    //Pickup the stuff
                    currTile.world.inventoryManager.PlaceInventory(this, currTile.inventoryTile, myJob.DesiresInventoryType(currTile.inventoryTile));
                }
                else
                {
                    //walk twards a tile containing the required goods.

                    //Find the first thing in the job that isn't satisfied.
                    Inventory desiredInventory = myJob.GetFirstDesiredInventory();

                    Inventory supplier = currTile.world.inventoryManager.GetClosestInventoryOfType(
                        desiredInventory.objectType, currTile, desiredInventory.maxStackSize - desiredInventory.stackSize, myJob.canTakeFromStockpile);

                    if (supplier == null)
                    {
                        Debug.Log("No tile contains objects of type " + desiredInventory.objectType + "to satisfy job requirements");
                        AbandonJob();
                        return;
                    }

                    destTile = supplier.tile;
                    return;
                }
            }
            return;//We can't continue until all materials are satisfied
        }

        //If we got here the job has all the material that it needs.
        //Lets make sure that our destination tile is the job site tile
        destTile = myJob.tile;

        //if (pathAStar.Length() == 1 && pathAStar != null) // we are adjacent to the jobsite

        //Are we there yet?
        if (currTile == destTile)
        {
            //We are at the correct tile for our job so execute the job's "dowork" which is mostly going to countdown jobtime
            //Also call its job complete callback
            myJob.DoWork(deltaTime);
        }

        //Nothing left for us to do here, we mostly just need UpdateDoMovement to get us where we want to go
    }

    public void AbandonJob()
    {
        nextTile = destTile = currTile;
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

        //Curr tile is the tile i'm currently in
        //NextTile is the tile I'm currently entering
        //DestTile = final destination

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
                    return;
                }
                //Lets ignore the first tile because we are already there
                nextTile = pathAStar.Dequeue();
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

        if(nextTile.IsEnterable() == Enterability.Never)
        {
            //Most likely a wall got built so we just need to reset our pathfinding
            //Fixme -- when a wall gets spawned we should invalidate our path immediatly so we don't 
            // waste time walking twards a dead end, to save cpu maybe we can onlny check every so often
            // Maybe register a callback to the ontilechanged event
            Debug.LogError("Fixme: a character was trying to enter an unwalkable tile");
            nextTile = null; //our next tile is not walkable 
            pathAStar = null; //our pathfinding info is out of date
            return;
        }
        else if(nextTile.IsEnterable() == Enterability.Soon)
        {
            //Debug.Log("WE are waiting for the door to open");
            //Tile is technically walkable but are we actually allowed to enter it right now
            return;
        }

        //How much distance can we travel this update?
        float distThisFrame = speed /nextTile.movementCost * deltaTime;
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
        job.UnregisterJobCancelCallback(OnJobEnded);
        job.UnregisterJobCompleteCallback(OnJobEnded);

        if (job != myJob)
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

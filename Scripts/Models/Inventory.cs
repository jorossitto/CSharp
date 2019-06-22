using UnityEngine;
using System.Collections;
using System;

//Lose objects are things that are lying on the floor//stockpile, like a bunch of metal bars
public class Inventory
{
    public string objectType = Config.STEEL_PLATE;

    public int maxStackSize = 50;

    protected int _stackSize = 1;
    public int stackSize
    {
        get
        {
            return _stackSize;
        }
        set
        {
            if(_stackSize != value)
            {
                _stackSize = value;
                if(callbackInventoryChanged != null)
                {
                    callbackInventoryChanged(this);
                }
            }
        }
    }

    // the function we callback any time our tile's data changes
    Action<Inventory> callbackInventoryChanged;

    public Tile tile;
    public Character character;

    public Inventory( )
    {

    }

    public Inventory(string objectType, int maxStackSize, int stackSize)
    {
        this.objectType = objectType;
        this.maxStackSize = maxStackSize;
        this.stackSize = stackSize;
    }
    protected Inventory(Inventory otherInventory)
    {
        objectType = otherInventory.objectType;
        maxStackSize = otherInventory.maxStackSize;
        stackSize = otherInventory.stackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }

    //Register a function to be called back when our tile type changes
    public void RegisterInventoryChangedCallback(Action<Inventory> callback)
    {
        callbackInventoryChanged += callback;
    }

    public void UnRegisterInventoryChangedCallback(Action<Inventory> callback)
    {
        callbackInventoryChanged -= callback;
    }
}

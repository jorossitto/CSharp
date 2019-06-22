using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager
{
    //This is a list of all live inventories
    //Later on this will likely be organized by rooms
    //in addition to a single master list
    public Dictionary<string, List<Inventory>> inventories;

    public InventoryManager()
    {
        inventories = new Dictionary<string, List<Inventory>>();
    }

    void CleanupInventory(Inventory inventory)
    {
        if (inventory.stackSize == 0)
        {
            if (inventories.ContainsKey(inventory.objectType))
            {
                inventories[inventory.objectType].Remove(inventory);
            }
            if(inventory.tile != null)
            {
                inventory.tile.inventoryTile = null;
                inventory.tile = null;
            }
            if(inventory.character != null)
            {
                inventory.character.myInventory = null;
                inventory.character = null;
            }
        }
    }
    public bool PlaceInventory(Tile tile, Inventory inventory)
    {
        bool tileWasEmpty = tile.inventoryTile == null;

        if( tile.PlaceInventory(inventory) == false)
        {
            //The tile did not accept the inventory for whatever reason therefore stop.
            return false;
        }
        //At this point, "inv" might be an empty stack if it was merged to another stack
        CleanupInventory(inventory);

        //we may have also created a new stack on the tile, if the tile was previously empty
        if(tileWasEmpty)
        {
            if(inventories.ContainsKey(tile.inventoryTile.objectType) == false)
            {
                inventories[tile.inventoryTile.objectType] = new List<Inventory>();
            }
            inventories[tile.inventoryTile.objectType].Add(tile.inventoryTile);
            tile.world.OnInventoryCreated(tile.inventoryTile);
        }
        return true;
    }

    public bool PlaceInventory(Job job, Inventory inventory)
    {
        if(job.inventoryRequirements.ContainsKey(inventory.objectType) == false)
        {
            Debug.LogError("Trying to add inventory to a job that it doesn't want.");
            return false;
        }

        job.inventoryRequirements[inventory.objectType].stackSize += inventory.stackSize;
        if(job.inventoryRequirements[inventory.objectType].maxStackSize < job.inventoryRequirements[inventory.objectType].stackSize)
        {
            inventory.stackSize = job.inventoryRequirements[inventory.objectType].stackSize - job.inventoryRequirements[inventory.objectType].maxStackSize;
            job.inventoryRequirements[inventory.objectType].stackSize = job.inventoryRequirements[inventory.objectType].maxStackSize;
        }
        else
        {
            inventory.stackSize = 0;
        }


        //At this point, "inv" might be an empty stack if it was merged to another stack
        CleanupInventory(inventory);
        return true;
    }

    public bool PlaceInventory(Character character, Inventory sourceInventory, int amount = -1)
    {
        if(amount < 0)
        {
            amount = sourceInventory.stackSize;
        }
        else
        {
            amount = Mathf.Min(amount, sourceInventory.stackSize);
        }

        if(character.myInventory == null)
        {
            character.myInventory = sourceInventory.Clone();
            character.myInventory.stackSize = 0;
            inventories[character.myInventory.objectType].Add(character.myInventory);
        }
        else if(character.myInventory.objectType != sourceInventory.objectType)
        {
            Debug.LogError("Character is trying to pickup a mismatched inventory object type");
            return false;
        }

        character.myInventory.stackSize += amount;

        if (character.myInventory.maxStackSize < character.myInventory.stackSize)
        {
            sourceInventory.stackSize = character.myInventory.stackSize - character.myInventory.maxStackSize;
            character.myInventory.stackSize = character.myInventory.maxStackSize;
        }
        else
        {
            sourceInventory.stackSize -= amount;
        }


        //At this point, "inv" might be an empty stack if it was merged to another stack
        CleanupInventory(sourceInventory);
        return true;
    }

    /// <summary>
    /// Gets the type of the closest inventory of
    /// </summary>
    /// <param name="objectType">Object type</param>
    /// <param name="tile">Tile location</param>
    /// <param name="desiredAmount">Desired amount. if no stack has enough, it instead returns the largest</param>
    /// <param name="canTakeFromStockpile">Can the items come from the stockpile</param>
    /// <returns>The closest inventory of type</returns>
    public Inventory GetClosestInventoryOfType(string objectType, Tile tile, int desiredAmount, bool canTakeFromStockpile)
    {
        //todo fixme - lying about returning the closest item
        //no way to return the closest item in an optimal manner until the database is more sophisticated
        if (inventories.ContainsKey(objectType) == false)
        {
            Debug.LogError("GetClosestInventoryOfType -- no items of desired type.");
            return null;
        }

        foreach (Inventory inventory in inventories[objectType])
        {
            if(inventory.tile != null 
                && (canTakeFromStockpile || inventory.tile.furniture == null || inventory.tile.furniture.IsStockpile() == false))
            {
                return inventory;
            }
        }
        return null;
    }
}

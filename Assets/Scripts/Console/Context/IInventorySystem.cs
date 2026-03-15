namespace Console.Context
{
    public interface IInventorySystem
    {
        /// <summary>
        /// Gives the player an item by data ID. Returns true if the item was found and added.
        /// </summary>
        bool GiveItem(string itemId, int amount);

        /// <summary>
        /// Returns true if an item with the given data ID exists.
        /// </summary>
        bool ItemExists(string itemId);
    }
}

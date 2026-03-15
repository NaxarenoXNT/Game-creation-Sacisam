namespace Console.Context
{
    public interface IPlayerProgression
    {
        int CurrentLevel { get; }
        int CurrentHealth { get; }
        int MaxHealth { get; }

        void LevelUp(int amount);
        void HealToFull();
    }
}

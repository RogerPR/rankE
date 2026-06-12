namespace RankE.Sim
{
    /// <summary>Decides one fighter's intent each tick (null = do nothing).
    /// Any randomness must come from battle.Rng to keep fights deterministic.</summary>
    public interface IBehavior
    {
        string Decide(Battle battle, int selfIndex);
    }
}

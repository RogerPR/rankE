namespace RankE.Sim
{
    /// <summary>
    /// Engine-level simulation constants. The reference values live in
    /// functional_POC/constants.py; gameplay numbers belong in data definitions,
    /// not here.
    /// </summary>
    public static class SimConstants
    {
        public const int TicksPerSecond = 20;
        public const float TickDuration = 1f / TicksPerSecond;
    }
}

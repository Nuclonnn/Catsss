namespace Catsss.Core.WorldHints
{
    /// <summary>Куда «приклеивается» world-space подсказка.</summary>
    public enum WorldTextHintAnchorMode : byte
    {
        CustomTransform = 0,
        LocalPlayerRoot = 1,
        LocalPlayerHead = 2,
    }
}

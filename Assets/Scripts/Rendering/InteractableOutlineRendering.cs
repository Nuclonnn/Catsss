namespace Catsss.Rendering
{
    /// <summary>
    /// Маска Rendering Layer для контура. В URP Asset добавь слой "InteractableOutline" (обычно index 1 → mask = 2).
    /// </summary>
    public static class InteractableOutlineRendering
    {
        public const uint LayerMask = 2u;
    }
}

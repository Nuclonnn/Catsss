namespace Catsss.Interaction
{
    /// <summary>Опциональная подсветка на интерактивном объекте (промпт — на игроке).</summary>
    public interface IInteractableViewHost
    {
        InteractableHighlightStub HighlightStub { get; }
    }
}

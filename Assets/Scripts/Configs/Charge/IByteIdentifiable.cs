namespace Catsss.Configs.Charge
{
    /// <summary>Типы зарядов и перманентных модификаторов идентифицируются байтом для сети (0 = отсутствует).</summary>
    public interface IByteIdentifiable
    {
        byte Id { get; }
    }
}

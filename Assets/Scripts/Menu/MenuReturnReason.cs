namespace Catsss.Menu
{
    /// <summary>Причина возврата из геймплея в MainMenu.</summary>
    public enum MenuReturnReason
    {
        None = 0,
        GuestConnectionFailed = 1,
        GuestInputError = 2,
        HostDisconnected = 3,
        SessionEnded = 4,
        UserLeftSession = 5,
        SceneLoadFailed = 6,
    }
}

namespace PVLib
{
    public enum ShowStatus { New, Ongoing, Complete }
    public enum Channel_Type { TV_Like, Binge_Like }
    public enum PlaylistFormat { m3u, pls }
    /// <summary>
    /// When does your server require credentials
    /// </summary>
    public enum SecurityApplication {Never, Only_Public_Requests, AlwaysUseLogin}
    public enum Access {User, Modderator}
}
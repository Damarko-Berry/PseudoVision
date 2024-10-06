namespace PVLib
{
    public enum ShowStatus { New, Ongoing, Complete }
    public enum Channel_Type { TV_Like, Binge_Like, Movies }
    public enum DirectoryType { Movie, Show}
    public enum PlaylistFormat { m3u, pls }
    /// <summary>
    /// When does your server require credentials
    /// </summary>
    public enum SecurityApplication {Never, Only_Public_Requests}
    public enum Access {User, Modderator}
    public enum DeviceType { MediaServer, InternetGatewayDevice, lighting, reminder }
    public enum ServiceType { ContentDirectory, ConnectionManager }
    public enum MovieMode { WithReruns, Sunday ,Monday, Tuesday, Wednesday, Thursday, Friday, Saturday} 
    
    public static class EnumTranslator<T>
    {
        public static T fromString(string str)
        {
            try
            {
                var enums = Enum.GetValues(typeof(T));
                for (int i = 0; i < enums.Length; i++)
                {
                    if (str == enums.GetValue(i).ToString())
                        return (T)enums.GetValue(i);
                }
            }
            catch
            {
                
            }
            throw new Exception($"Not a valid {typeof(T).Name}");
        }

    }
}
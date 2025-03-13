namespace PVLib
{
    public enum ShowStatus { New, Ongoing, Complete }
    public enum Channel_Type { TV_Like, Binge_Like, Movies }
    public enum Schedule_Type { TV_Like, Binge_Like, LiveStream, PerRequest}
    public enum DirectoryType { Movie, Show}
    public enum PlaylistFormat { m3u, pls }
    /// <summary>
    /// When does your server require credentials
    /// </summary>
    public enum SecurityApplication {Never, Only_Public_Requests}
    public enum Access {User, Moderator}
    public enum DeviceType { MediaServer, InternetGatewayDevice, lighting, reminder }
    public enum ServiceType { ContentDirectory, ConnectionManager }
    public enum MovieMode { WithReruns, Sunday ,Monday, Tuesday, Wednesday, Thursday, Friday, Saturday}
    public enum LiveHandling { Storage_Saver, CPU_Saver }
    public enum LiveProtocol { Built_In, HLS }
    public enum BreaksLength { OneWeek, TwoWeeks, ThreeWeeks, FourWeeks, TwoMonths, ThreeMonths, OneYear}
    public enum MessageType { Normal, Error }
    #region Metadata
    [Flags]
    public enum Genre
    {
        Action = 1,
        Adventure = 2,
        Biography = 4,
        Comedy = 8,
        Crime = 16,
        Documentary = 32,
        Drama = 64,
        Family = 128,
        Fantasy = 256,
        Film_Noir = 512,
        Game_Show = 1024,
        History = 2048,
        Horror = 4096,
        Music = 8192,
        Musical = 16384,
        Mystery = 32768,
        News = 65536,
        Reality_TV = 131072,
        Romance = 262144,
        Sci_Fi = 524288,
        Sport = 1048576,
        Talk_Show = 2097152,
        Thriller = 4194304,
        War = 8388608,
        Western = 16777216,
        Any = Action | Adventure | Biography | Comedy | Crime | Documentary | Drama | Family | Fantasy | Film_Noir | Game_Show | History | Horror | Music | Musical | Mystery | News | Reality_TV | Romance | Sci_Fi | Sport | Talk_Show | Thriller | War | Western
    }

    [Flags]
    public enum Style {
        Animation=1, 
        Anime=2, 
        Black_and_White=4, 
        Color=8, 
        Silent=16, 
        Talkie=32, 
        Any= Animation|Anime|Black_and_White|Color|Silent|Talkie 
    }
    [Flags]
    public enum Demographic
    {
        Kids = 1,
        Teens = 2,
        Adults = 4,
        Seniors = 8,
        Any = Kids | Teens | Adults

    }
    [Flags]
    public enum GenderDemographic
    {
        Male = 1,
        Female = 2,
        Any = Male | Female
    }
    [Flags]
    public enum EpisodeStyle
    {
        Episodic = 1,
        Seialized = 2,
        Any= Episodic | Seialized
    }

    #endregion
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
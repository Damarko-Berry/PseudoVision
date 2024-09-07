namespace PVLib
{
    public enum ShowStatus { New, Ongoing, Complete }
    public enum Channel_Type { TV_Like, Binge_Like }

    public enum PlaylistFormat { m3u, pls }
    /// <summary>
    /// When does your server require credentials
    /// </summary>
    public enum SecurityApplication {Never, Only_Public_Requests}
    public enum Access {User, Modderator}

    static class EnumTranslator
    {
        public static Channel_Type CT_fromString(string str)
        {
            var enums = Enum.GetValues(typeof(Channel_Type));
            for (int i = 0; i < enums.Length; i++)
            {
                if (str == ((Channel_Type)i).ToString()) return (Channel_Type)i;
            }
            throw new Exception("Not a Channel Type");
        }
    }
}
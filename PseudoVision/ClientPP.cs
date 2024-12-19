using System.Net;

namespace PseudoVision
{
    public class ClientPP
    {
        public string Name;
        public Time day;
        Playlist GetPlaylist => new(FileSystem.Archive(channelname, date));
        DateTime date => day;
        public string channelname;
        public int Pos;
        string filename => $"{Name}{channelname}{day.Month}{day.Day}{day.Year}";
        public static string potentialfilename(HttpListenerContext client, string cn, Time time) => client.Request.UserAgent.Replace("/",string.Empty).Replace(".",string.Empty).Replace(" ", string.Empty) + cn + $"{time.Month}{time.Day}{time.Year}";
        public ClientPP(HttpListenerContext client, string channelname, string M, string D, string Y)
        {
            Pos = 0;
            Name = client.Request.UserAgent.Replace("/", string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty);
            this.channelname = channelname;
            day.Month = int.Parse(M);
            day.Year = int.Parse(Y);
            day.Day = int.Parse(D);
            SaveLoad<ClientPP>.Save(this, Path.Combine(FileSystem.Schedules, "PP", filename));
        }
        ClientPP() { }
        public async Task SendMedia(HttpListenerResponse client)
        {
            FileStream fs = new(GetPlaylist[Pos].Media, FileMode.Open, FileAccess.Read);
            try
            {
                client.ContentType = $"video/{new FileInfo(GetPlaylist[Pos].Media).Extension}";
                client.ContentLength64 = fs.Length;
                await fs.CopyToAsync(client.OutputStream);
                Pos++;
                if (Pos >= GetPlaylist.Count)
                {
                    File.Delete(Path.Combine(FileSystem.Schedules, "PP", filename));
                }
                else
                {
                    SaveLoad<ClientPP>.Save(this, Path.Combine(FileSystem.Schedules, "PP", filename));
                }
            }
            catch (Exception ex)
            {
                fs.Close();
                Console.WriteLine(ex.ToString());
            }
            client.Close();
            fs.Close();
        }
    }
}

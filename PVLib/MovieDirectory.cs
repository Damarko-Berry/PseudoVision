using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class MovieDirectory : ContentDirectory
    {
        public ShowStatus status;

        public DirectoryType directoryType = DirectoryType.Movie;
        public override DirectoryType dirtype => directoryType;
        

        public override FileInfo[] Content
        {
            get
            {
                List<FileInfo> VF = new();
                DirectoryInfo directoryInfo = new(HomeDirectory);
                for (int i = 0; i < ValidExtentions.Length; i++)
                {
                    VF.AddRange(directoryInfo.GetFiles("*" + ValidExtentions[i], SearchOption.AllDirectories));
                }
                return VF.ToArray();
            }
        }

        public MovieDirectory() { }
        public override string NextEpisode()
        {
            var files = Content;
            return files[new Random().Next(0, files.Length)].FullName;
        }
    }
}

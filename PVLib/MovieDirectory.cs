using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class MovieDirectory : ContentDirectory
    {
        List<FileInfo> VideoFiles
        {
            get
            {
                List<FileInfo> VF = new();
                DirectoryInfo directoryInfo = new(dir);
                VF.AddRange(directoryInfo.GetFiles("*.mp4", SearchOption.AllDirectories));
                VF.AddRange(directoryInfo.GetFiles("*.MOV", SearchOption.AllDirectories));
                VF.AddRange(directoryInfo.GetFiles("*.MP4", SearchOption.AllDirectories));

                return VF;
            }
        }
        public DirectoryType directoryType = DirectoryType.Movie;
        public override DirectoryType dirtype => directoryType;

        public MovieDirectory() { }
        public override string NextEpisode()
        {
            throw new NotImplementedException();
        }
    }
}

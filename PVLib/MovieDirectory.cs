using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PVLib
{
    public class MovieDirectory : ContentDirectory
    {
        public ShowStatus status;

        internal override FileInfo[] Content
        {
            get
            {
                List<FileInfo> VF = new();
                DirectoryInfo directoryInfo = new(HomeDirectory);
                for (int i = 0; i < ValidExtensions.Length; i++)
                {
                    VF.AddRange(directoryInfo.GetFiles("*" + ValidExtensions[i], SearchOption.AllDirectories));
                }
                return [.. VF];
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

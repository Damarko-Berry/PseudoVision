using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoVision
{
    internal abstract class UpnpService
    {
        public abstract ServiceType serviceType { get; }
    }

    internal class ContentDirectory: UpnpService
    {
        public override ServiceType serviceType => ServiceType.ContentDirectory;

        public string ID, ParentID, Title, Class;
        public Uri res;
        public bool restricted;
        
        int childCount => 0;
    } 
    
}

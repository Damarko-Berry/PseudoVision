using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoVision
{
    static class Security
    {
        public static bool Authorized(string username, string password)
        {
            return false;
        }
        public static CustomAuth Authorization;
    }
    public delegate bool CustomAuth(string username, string password);
}

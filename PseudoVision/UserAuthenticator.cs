using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PseudoVision
{
    static class UserAuthenticator
    {
        static UserInfo[] users = [];
        static DirectoryInfo directory = new(Path.Combine(Directory.GetCurrentDirectory(), "users"));
        public static bool Auth(HttpListenerRequest request, SecurityApplication level)
        {
            if (SecurityApplication.Never == level) return true;
            var userip = request.RemoteEndPoint.Address.ToString();
            bool isPrivate = userip.Contains("192");
            if (level == SecurityApplication.Only_Public_Requests | isPrivate) return true;
            if (request.Headers["Authorization"] != null)
            {
                string authHeader = request.Headers["Authorization"];
                string encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                string decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                string[] credentials = decodedCredentials.Split(':');
                string username = credentials[0];
                string password = credentials[1];
                Authorized(username, password);
            }
            return false;
        }
        static string FindUsernameByEmail(string un)
        {

            return un;
        }
        public static bool Authorized(string username, string password)
        {
            var userpath = Path.Combine(directory.FullName, username);
            if(File.Exists(userpath))
            {
                var user = SaveLoad<UserInfo>.Load(userpath);
                return password == user.Password;
            }
            else
            {
                if(users.Length == 0) LoadArray();
                for (int i = 0; i < users.Length; i++)
                {
                    if(username == users[i].Username | username == users[i].Email)
                        return password == users[i].Password;
                }
            }
            return false;
        } 
        public static bool Authorized(string username, string password, Access levelReq)
        {
            var userpath = Path.Combine(directory.FullName, username);
            if (File.Exists(userpath))
            {
                var user = SaveLoad<UserInfo>.Load(userpath);
                return password == user.Password & user.access >= levelReq;
            }
            else
            {
                if (users.Length == 0)LoadArray();
                for (int i = 0; i < users.Length; i++)
                {
                    if (username == users[i].Username)
                        return password == users[i].Password & users[i].access >= levelReq;
                }
            }
            return false;
        }
        static void LoadArray()
        {
            users = new UserInfo[directory.GetFiles().Length];
            for (int i = 0; i < users.Length; i++)
            {
                users[i] = SaveLoad<UserInfo>.Load(directory.GetFiles()[i].FullName);
            }
        }
    }
}

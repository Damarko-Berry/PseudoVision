using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PseudoVision
{
    static class UserAuthenticator
    {
        static UserInfo[] users = [];
        static DirectoryInfo directory = new(Path.Combine(Directory.GetCurrentDirectory(), "users"));
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
                    if(username == users[i].Username)
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

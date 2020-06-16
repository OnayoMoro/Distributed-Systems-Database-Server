using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DistSysACW.Models
{
    public class User
    {
        #region Task2
        // TODO: Create a User Class for use with Entity Framework
        // Note that you can use the [key] attribute to set your ApiKey Guid as the primary key 
        #endregion
        public User()
        {
            Logs = new List<Log>();
        }

        [Key]
        public string ApiKey { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public virtual ICollection<Log> Logs { get; set; }
    }

    #region Task13?
    // TODO: You may find it useful to add code here for Logging
    #endregion
    public class Log
    {
        public Log()
        {

        }
        [Key]
        public int LogId { get; set; }
        public string LogString { get; set; }
        public DateTime LogDateTime { get; set; }
        [ForeignKey("ApiKey")]
        public string UserApiKey { get; set; }
    }

    public class LogArchive
    {
        public LogArchive()
        {

        }

        public LogArchive(Log p_log)
        {
            LogString = p_log.LogString;
            LogDateTime = p_log.LogDateTime;
            UserApiKey = p_log.UserApiKey;
        }

        #region Task13?
        // TODO: You may find it useful to add code here for Logging
        #endregion
        [Key]
        public int LogId { get; set; }
        public string LogString { get; set; }
        public DateTime LogDateTime { get; set; }
        public string UserApiKey { get; set; }
    }

    public static class UserDatabaseAccess
    {
        #region Task3 
        // TODO: Make methods which allow us to read from/write to the database 

        //Logging functions
        public static void LogData(string command, User user, UserContext ctx)
        {
            Log log = new Log
            {
                LogString = (command),
                LogDateTime = DateTime.Now
            };
            ctx.Logs.Add(log);

            user.Logs.Add(log);
            ctx.Users.Update(user);
            try { ctx.SaveChanges(); }
            catch (Exception e) { Console.WriteLine(e); }
        }

        //Return User Object Using ApiKey
        public static User UserRead(string apikey, UserContext ctx)
        {
            User user = ctx.Users.FirstOrDefault(u => u.ApiKey == apikey);
            return user;
        }

        //Return User Object Using UserName
        public static User UserNameRead(string username, UserContext ctx)
        {
            User user = ctx.Users.FirstOrDefault(u => u.UserName == username);
            return user;
        }

        //Create User Object and Store in Database
        public static User UserCreate(string username, UserContext ctx)
        {
            User user = new User() { UserName = username, ApiKey = Guid.NewGuid().ToString() };
            
            user.Role = "User";
            if (ctx.Users.Count() == 0)
                user.Role = "Admin";

            if (!UserCheckUserName(user.UserName, ctx))
            {
                ctx.Users.Add(user);
                ctx.SaveChanges();

                return user;
            }
            
            return null;
        }

        //Return User Object From Database
        public static User UserReturn(string apikey)
        {
            var ctx = new UserContext();
            if (UserCheckApiKey(apikey, ctx))
            {
                return UserRead(apikey, ctx);
            }
            return null;
        }

        //Delete User Object From Database
        public static void UserDelete(string apikey)
        {
            try
            {
                using (var ctx = new UserContext())
                {
                    User user = UserRead(apikey, ctx);
                    foreach (var l in ctx.Logs)
                    {
                        if (l.UserApiKey == user.ApiKey)
                        {
                            var loga = new LogArchive(l);
                            ctx.LogArchives.Add(loga);
                            ctx.Logs.Remove(l);
                        }
                    }
                    ctx.Users.Remove(user);
                    ctx.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: User does not exist" + e);
            }
        }

        //////////////////////////////////////////////////////////////////
        //Cheack Matches in Database for UserName, ApiKey and Role
        public static bool UserCheckUserName(string username, UserContext ctx)
        {
            if (ctx.Users.Any(o => o.UserName == username))
                return true;
            return false;
        }

        public static bool UserCheckApiKey(string apikey, UserContext ctx)
        {
            if (ctx.Users.Any(o => o.ApiKey == apikey))
                return true;
            return false;
        }

        public static bool UserCheckRole(string role, UserContext ctx)
        {
            if (ctx.Users.Any(o => o.Role == role))
                return true;
            return false;
        }
        //////////////////////////////////////////////////////////////////

        public static string ByteToHexString(byte[] bytearray)
        {
            string bytestring = "";

            for (int i = 0; i < bytearray.Length; i++)
            {
                bytestring += bytearray[i].ToString("x2");
            }

            return bytestring;
        }

        #endregion
    }


}
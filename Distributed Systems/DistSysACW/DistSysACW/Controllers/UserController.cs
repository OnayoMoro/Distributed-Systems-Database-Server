using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DistSysACW.Models;
using System.Collections;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DistSysACW.Controllers
{
    [Route("api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        protected readonly Models.UserContext _context;
        public UserController(Models.UserContext context)
        {
            _context = context;
        }

        // GET: api/<controller>
        [ActionName("New")]
        [HttpGet]
        public IActionResult Get([FromQuery] string username)
        {
            if (UserDatabaseAccess.UserCheckUserName(username, _context))
                return Ok("True - User Does Exist! Did you mean to do a POST to create a new user?");
            
            else
                return Ok("False - User Does Not Exist! Did you mean to do a POST to create a new user?");
        }

        // POST api/<controller>
        [ActionName("New")]
        [HttpPost]
        public IActionResult Post([FromBody] string username)
        {
            if (username == null || username == "")
                return BadRequest("Oops. Make sure your body contains a string with your " +
                    "username and your Content-Type is Content-Type:application/json");

            var user = UserDatabaseAccess.UserCreate(username, _context);
            if (user != null)
            {
                return Ok(user.ApiKey);
            }
            else
                return StatusCode(403, "Oops. This username is already in use. Please try again with a new username.");
        }

        [ActionName("RemoveUser")]
        [Authorize(Roles = "Admin,User")]
        [HttpDelete]
        public IActionResult Delete([FromHeader] string apikey, [FromQuery] string username)
        {
            try
            {
                apikey = apikey.Trim(new char[] { ' ', '"', '/' });
                User user = UserDatabaseAccess.UserReturn(apikey);
                if (user.UserName == username)
                {
                    UserDatabaseAccess.UserDelete(apikey);
                }
                return Ok(true);
            }
            catch
            {
                return Ok(false);
            }
        }
        
        [ActionName("changerole")]
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult ChangeRole([FromBody] JObject _user)
        {
            try
            {
                string username = _user["Username"].ToString();
                string role = _user["Role"].ToString();

                if (role != "Admin" && role != "User")
                {
                    return Ok("NOT DONE: Role does not exist");
                }

                if (UserDatabaseAccess.UserCheckUserName(username, _context))
                {
                    User user = UserDatabaseAccess.UserNameRead(username, _context);
                    user.Role = role;
                    _context.Users.Update(user);
                    _context.SaveChanges();
                    return Ok("Done");
                }
                else
                {
                    return Ok("NOT DONE: Username does not exist");
                }
            }
            catch
            {
                return Ok("NOT DONE: An error occured");
            }
        }
    }
}
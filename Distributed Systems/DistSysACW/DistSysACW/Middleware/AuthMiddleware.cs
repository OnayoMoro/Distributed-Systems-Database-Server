using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DistSysACW.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, Models.UserContext dbContext)
        {
            #region Task5
            // TODO:  Find if a header ‘ApiKey’ exists, and if it does, check the database to determine if the given API Key is valid
            //        Then set the correct roles for the User, using claims

           context.Request.Headers.TryGetValue("ApiKey", out var ApiKey);

            if (Models.UserDatabaseAccess.UserCheckApiKey(ApiKey, dbContext))
            {
                Models.User user = Models.UserDatabaseAccess.UserRead(ApiKey, dbContext);
                string url = context.Request.Path.ToString();
                if (url.Contains("/api"))
                {
                    url = url.Replace("/api", "");
                    Models.UserDatabaseAccess.LogData("User requested " + url, user, dbContext);
                }

                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, user.UserName));
                claims.Add(new Claim(ClaimTypes.Role, user.Role));

                var claimsIdentity = new ClaimsIdentity(claims, ApiKey);
                context.User.AddIdentity(claimsIdentity);
            }
            #endregion

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }

    }
}

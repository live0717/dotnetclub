﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Discussion.Web.Models;
using Jusfr.Persistent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Discussion.Web.Services.Identity
{
    public static class PrincipalContext
    {
        private const string _cookiesAuth = CookieAuthenticationDefaults.AuthenticationScheme;

        public static bool IsAuthenticated(this HttpContext httpContext)
        {
            var isAuthedExpr = httpContext?.User?.Identity?.IsAuthenticated;
            return isAuthedExpr.HasValue && isAuthedExpr.Value; 
        }
        
        public static DiscussionPrincipal DiscussionUser(this HttpContext httpContext)
        {
            if (!IsAuthenticated(httpContext))
            {
                return null;
            }

            return httpContext.User as DiscussionPrincipal;
        }
        
        public static async Task SigninAsync(this HttpContext httpContext, User user, bool isPersistent = false) {
            var claims = new List<Claim> {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(), ClaimValueTypes.Integer32),
                    new Claim(ClaimTypes.Name, user.DisplayName, ClaimValueTypes.String),
                    new Claim("SigninTime", System.DateTime.UtcNow.Ticks.ToString(), ClaimValueTypes.Integer64)
                };

            var identity = new ClaimsIdentity(claims, _cookiesAuth);
            var principal = new ClaimsPrincipal(identity);

            httpContext.User = principal;
            await httpContext.SignInAsync(_cookiesAuth, principal, new AuthenticationProperties { IsPersistent = isPersistent });
            AssignDiscussionPrincipal(httpContext, user);
        }

        public static async Task SignoutAsync(this HttpContext httpContext)
        {
            // didn't remove cookie...
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            await httpContext.SignOutAsync(_cookiesAuth);
        }

        public static void AssignDiscussionPrincipal(this HttpContext httpContext, IUser user = null)
        {
            ClaimsIdentity identity;
            var currentPrincipal = httpContext.User;
            if (!IsAuthenticated(httpContext) || (null == (identity = currentPrincipal.Identity as ClaimsIdentity)))
            {
                return;
            }

            if (user == null)
            {
                var claims = identity.Claims;
                var userIdClaim = claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
                int userId;
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                    return;
                }


                var userRepo = httpContext.RequestServices.GetService(typeof(IRepository<User>)) as IRepository<User>;
                user = userRepo.Retrive(userId);
            }

            if (user != null)
            {
                httpContext.User = new DiscussionPrincipal(identity) { User = user };
            }
        }


    }
}
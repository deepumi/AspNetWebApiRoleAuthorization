namespace AspNetWebApiRoleAuthorization.Controllers
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Web.Http;
    using ViewModels;

    public class BaseApiController : ApiController
    {
        /// <summary>
        /// pupulate user model from Claims Identity
        /// </summary>
        protected UserSessionModel UserSessionModel
        {
            get
            {
                if (User.Identity.IsAuthenticated)
                {
                    var user = User.Identity as ClaimsIdentity;
                    var userId = user?.Claims?.FirstOrDefault(o => o.Type == "sub")?.Value; //get user id from claim
                    Guid tempUserId;
                    Guid.TryParse(userId, out tempUserId);

                    return new UserSessionModel
                    {
                        UserId = tempUserId,
                        Roles = user?.Claims?.FirstOrDefault(o => o.Type == "role")?.Value.Split(',') // get roles from claim
                    };
                }
                return null;
            }
        }

        protected bool IsInRole(string roleName)
        {
            return UserSessionModel != null && UserSessionModel.Roles.Any(r => r == roleName);
        }
    }
}
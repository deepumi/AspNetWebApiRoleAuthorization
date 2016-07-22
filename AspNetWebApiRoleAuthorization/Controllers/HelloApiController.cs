namespace AspNetWebApiRoleAuthorization.Controllers
{
    using System.Web.Http;
    using System.Net;
    using Filters;

    [ApiAuthorize]
    public class HelloApiController : BaseApiController
    {
        public IHttpActionResult Get()
        {
            var userSessionModel = UserSessionModel; 

            var userId = userSessionModel?.UserId; //get userid

            var roles = userSessionModel?.Roles; //get roles

            if (IsInRole("Admin")) //Allow Admin only here
            {
                return Ok(new string[] { "value1", "value2" });
            }
            return Content(HttpStatusCode.Unauthorized, "Not authorized to access the resource");
        }
    }
}
# Asp.Net Web Api Role based Authorization
Sample project illustrate how to handle role based authorization in Asp.Net Web API.

## Step1
* File -> New Project -> Asp.Net Web Api 4.6.2 empty project template.

## Step2
* Install following nuget packages 
  * Microsoft.Owin
  * Microsoft.Owin.Cors
  * Microsoft.Owin.Hosting
  * Microsoft.Owin.Host.SystemWeb
  * Microsoft.Owin.Security
  * Microsoft.Owin.Security.OAuth
  * Owin

## Step3
* Create a Owin Startup class and decorate with assembly attribute OwinStartup.
  ```c#
	using Microsoft.Owin;
	using Microsoft.Owin.Security.OAuth;
	using Owin;
	using System;
	using System.Web.Http;

	[assembly: OwinStartup(typeof(AspNetWebApiRoleAuthorization.Startup))]
	namespace AspNetWebApiRoleAuthorization
	{
		public class Startup
		{
			public void Configuration(IAppBuilder app)
			{
				HttpConfiguration config = new HttpConfiguration();

				ConfigureOAuth(app);

				WebApiConfig.Register(config);
				app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);
				app.UseWebApi(config);
			}

			public void ConfigureOAuth(IAppBuilder app)
			{
				OAuthAuthorizationServerOptions OAuthServerOptions = new OAuthAuthorizationServerOptions()
				{
					AllowInsecureHttp = true, //should be false in production
					TokenEndpointPath = new PathString("/oauth/token"),
					AccessTokenExpireTimeSpan = TimeSpan.FromDays(5),
					Provider = new AuthorizationServerProvider() //custom authorization service provider
				};
				// Token Generation
				app.UseOAuthAuthorizationServer(OAuthServerOptions);
				app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());
			}
		}
	}
  ```
## Step4
* Create authorization provider class for user authentication and set user roles while creating oAuth cookie.
  ```c#
    public class AuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            await Task.FromResult(context.Validated());
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });

            var user = Validate(context.UserName, context.Password); //validate credentials against database.

            if (user == null)
            {
                context.Rejected();
                return;
            }

            // create identity
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim("sub", user.UserId.ToString())); //user id
            identity.AddClaim(new Claim("role", string.Join(",",user.Roles))); //roles
            context.Validated(identity);
            await Task.FromResult(0);
        }

        private UserSessionModel Validate(string username,string password)
        {
            if(username.Equals(password)) //temporary validation need to change this in production
            {
                return new UserSessionModel
                {
                    UserId = Guid.NewGuid(),
                    Roles = new[] { "User","Admin" }
                };
            }
            return null;
        }
    }
  ```

## Step5
* Use fiddler to generate the oAuth token 
  ```
  POST /oauth/token HTTP/1.1
  Host: localhost:61557
  Cache-Control: no-cache
  Content-Type: application/x-www-form-urlencoded
  
  username=deepu&password=deepu&grant_type=password
  ```
  **OR**
  
* Use postman to generate the oAuth token

![Image of oAuth token test](https://github.com/deepumi/AspNetWebApiRoleAuthorization/blob/master/AspNetWebApiRoleAuthorization/Screens/webapi_token_generate.png)
 
## Step6

* Create API Authorize attribute class for authentication check
```c#
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (!IsAuthorized(actionContext)) //check if the request has a valid token using IsAuthorized method.
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not authorized");
            }
        }
    }
```
## Step7
* Get userid and roles from ClaimsIdentity
```c#
  public class BaseApiController : ApiController
  {
      /// <summary>
      /// Get user model from Claims Identity
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
```
## Finally create the API controller and get role and userid from base api controller
```c#
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
```

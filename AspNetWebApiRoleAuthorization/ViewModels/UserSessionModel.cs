namespace AspNetWebApiRoleAuthorization.ViewModels
{
    public class UserSessionModel
    {
        public System.Guid UserId { get; set; }

        public string[] Roles { get; set; }
    }
}
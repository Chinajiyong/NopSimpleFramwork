namespace Nop.Web.Framework.Security
{
    /// <summary>
    /// Jwt User
    /// </summary>
    public partial class JwtRequest
    {
         public string username { get; set; }

         public string password { get; set; }
    }

    /// <summary>
    /// Jwt User Model
    /// </summary>
    public partial class JwtResponse
    {
        public string username { get; set; }

        public string password { get; set; }

        public string token { get; set; }
    }
}

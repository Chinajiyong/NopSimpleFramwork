using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Domain.Users;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nop.Web.Framework.Security
{
    /// <summary>
    /// Jwt
    /// </summary>
    public class Jwt
    {
        protected static IConfiguration _config;

        public Jwt(IConfiguration config)
        {
            _config = config;
        }

        public static string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.UserRoles.Count > 0 ? String.Join(",", user.UserRoles) :  string.Empty), // 添加角色信息
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("very123SecretKey"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(Convert.ToDouble("30"));

            var token = new JwtSecurityToken("http://localhost:15536/",
              "api",
              claims,
              expires: expires,
              signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

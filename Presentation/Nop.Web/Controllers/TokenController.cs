using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Users;
using Nop.Services.Users;
using Nop.Web.Framework.Security;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : Controller
    {
        private readonly IUserRegistrationService _userRegistrationService;
        private readonly IUserService _userService;
        private readonly UserSettings _userSettings;
        public TokenController(IUserRegistrationService userRegistrationService,
             UserSettings userSettings,
             IUserService userService)
        {
            this._userRegistrationService = userRegistrationService;
            this._userSettings = userSettings;
            this._userService = userService;
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult CreateToken(JwtRequest login)
        {
            IActionResult response = Unauthorized();
             
            var loginResult = _userRegistrationService.ValidateUser(_userSettings.UsernamesEnabled ? login.username : login.username, login.password);

            if (loginResult == UserLoginResults.Successful)
            {
                var user = _userService.GetUserByUsername(login.username);

                var tokenString = Jwt.GenerateToken(user);
                response = Ok(new { token = tokenString });
            }

            return response;
        }
 
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Users;
using Nop.Services.Users;
using Nop.Web.Areas.Admin.Factories;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUserModelFactory _userModelFactory;

        public ValuesController(IUserService userService,
             IUserModelFactory userModelFactory)
        {
            this._userService = userService;
            this._userModelFactory = userModelFactory;
        }

        // GET: api/<controller>
        [Authorize]
        [HttpGet]
        public IActionResult Get()
        {
            var users = _userService.GetAllUsers().Where(x => x.IsRegistered() && x.Active && !x.Deleted).ToList();

            return Ok(users);
        }

        /// <summary>
        /// Get All Users
        /// </summary>
        /// <returns></returns>
        [HttpGet("List")]
        public IActionResult List()
        {
            var users = _userService.GetAllUsers().Where(x=>x.IsRegistered() && x.Active && !x.Deleted).ToList();
            //_mapper.Map();
            return Ok(users);
        }

        // GET api/<controller>/5
        /// <summary>
        /// Get a user by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public string Get(int id)
        {
            if (id == 5)
            {
                return "value=5";
            }
            else
                return "value";
        }

        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

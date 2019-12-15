using System;
using System.Collections.Generic;
using System.Linq;
using CDMApi.Features.Shared;
using Microsoft.AspNetCore.Mvc;

namespace CDMApi.Features.Roles
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly CDMService<RoleModel> _cdmService;

        public RolesController(CDMService<RoleModel> cdmService)
        {
            _cdmService = cdmService;
        }

        [HttpGet]
        public ActionResult<List<Guid>> GetIds()
        {
            var results = _cdmService.Query(r => r != null, r => r.Id, 0, 0);
            return Ok(results);
        }

        [HttpGet("Id")]
        public ActionResult<RoleModel> Get(string id)
        {
            var results = _cdmService.Query(r => r.Id.ToString() == id, r => r, 0, 0);
            if (!results.Any())
            {
                return NotFound();
            }

            return Ok(results.First());
        }

        [HttpGet("Names")]
        public ActionResult<RoleModel> GetNames()
        {
            var results = _cdmService.Query(r => r.name != null, r => r.name, 0, 0);
            return Ok(results);
        }
    }
}
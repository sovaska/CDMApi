using System;
using System.Collections.Generic;
using System.Linq;
using CDMApi.Features.Shared;
using Microsoft.AspNetCore.Mvc;

namespace CDMApi.Features.Systemusers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemusersController : ControllerBase
    {
        private readonly CDMService<SystemuserModel> _cdmService;

        public SystemusersController(CDMService<SystemuserModel> cdmService)
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
        public ActionResult<SystemuserModel> Get(string id)
        {
            var results = _cdmService.Query(r => r.ToString() == id, r => r, 0, 0);
            if (!results.Any())
            {
                return NotFound();
            }

            return Ok(results.First());
        }

        [HttpGet("Names")]
        public ActionResult<SystemuserModel> GetNames()
        {
            var results = _cdmService.Query(r => r.firstname != null, r => r.firstname, 0, 0);
            return Ok(results);
        }
    }
}
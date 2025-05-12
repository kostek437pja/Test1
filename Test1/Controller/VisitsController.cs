using Microsoft.AspNetCore.Mvc;
using Test1.Exceptions;
using Test1.Model;
using Test1.Service;

namespace Test1.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitsController : ControllerBase
    {
        
        private IDBService iDBService;

        public VisitsController(IDBService iDbService)
        {
            iDBService = iDbService;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetVisit(int id)
        {
            try
            {
                return Ok(await iDBService.GetVisit(id));
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }
        
        [HttpPost]
        public async Task<IActionResult> CreateVisit([FromBody] CreateVisitDTO visitDto)
        {
            try
            {
                await iDBService.CreateVisit(visitDto);
                return CreatedAtAction(nameof(CreateVisit), new { visitDto.VisitId });
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (InvalidArgumentException e)
            {
                return BadRequest(e.Message);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
            
        }
    }
}
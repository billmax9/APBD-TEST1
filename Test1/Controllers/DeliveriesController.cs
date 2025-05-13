using Microsoft.AspNetCore.Mvc;
using Test1.DTOs;
using Test1.Exceptions;
using Test1.Services;

namespace Test1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly IDBService _dbService;

    public DeliveriesController(IDBService dbService)
    {
        _dbService = dbService;
    }


    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetDeliveryById(int id)
    {
        try
        {
            var res = await _dbService.FindDeliveryByIdAsync(id);
            return Ok(res);
        }
        catch (NotFoundException e)
        {
            return NotFound(new { e.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { e.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddNewDelivery(DeliveryRequestDto dto)
    {
        try
        {
            await _dbService.AddNewDeliveryAsync(dto);
            return Ok();
        }
        catch (ConflictException e)
        {
            return Conflict(new { e.Message });
        }
        catch (DataValidationException e)
        {
            return BadRequest(new { e.Message });
        }
        catch (NotFoundException e)
        {
            return NotFound(new { e.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, new { e.Message });
        }
    }
}
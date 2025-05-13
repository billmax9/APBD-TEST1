using Microsoft.AspNetCore.Mvc;
using Test1.DTOs;
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
        var res = await _dbService.FindDeliveryByIdAsync(id);
        return Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> AddNewDelivery(DeliveryRequestDto dto)
    {
        await _dbService.AddNewDeliveryAsync(dto);
        return Created($"api/deliveries/{dto.DeliveryId}", new { dto.DeliveryId });
    }
}
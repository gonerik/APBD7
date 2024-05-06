using System.Data;
using System.Net;
using APBD7.Classes;
using APBD7.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD7.Controllers;

[ApiController]
[Route("api/[controller]")] // api/students
public class WarehouseProductController(IDbService db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> FulfillOrder(CreateWarehouseProductRequest request)
    {
        try
        {
            var result = await db.FullfilOrderAsync(request);
            return Ok(result);

        }
        catch (DataException e)
        {
            return BadRequest(e.Message);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }
}
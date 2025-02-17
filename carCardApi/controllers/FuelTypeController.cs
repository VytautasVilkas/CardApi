// FuelTypeController.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using carCard.Services;
using Microsoft.AspNetCore.Authorization;

namespace carCard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FuelTypeController : ControllerBase
    {
        private readonly IFuelTypeService _fuelTypeService;

        public FuelTypeController(IFuelTypeService fuelTypeService)
        {
            _fuelTypeService = fuelTypeService;
        }

        // GET api/fueltype
        
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetFuelTypes()
        {
            try
            {
                var fuelTypes = await _fuelTypeService.GetFuelTypesAsync();
                return Ok(fuelTypes);
            }
            catch (Exception ex){
                return StatusCode(500, new { message = "Nepavyko gauti degalų tipų.", error = ex.Message });
            }
        }
    }
}

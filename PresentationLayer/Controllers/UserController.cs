using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;

namespace PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("toggle-extra-quota")]
        public async Task<IActionResult> ToggleExtraQuota([FromBody] ToggleRequest request)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                         ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                         
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var success = await _userService.UpdateUseExtraQuotaAsync(userId, request.UseExtraQuota);
            return Ok(new { success });
        }
    }

    public class ToggleRequest
    {
        public bool UseExtraQuota { get; set; }
    }
}

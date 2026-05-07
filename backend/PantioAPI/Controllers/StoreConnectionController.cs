using Microsoft.AspNetCore.Mvc;
using PantioClassLibrary.DTO;
using PantioClassLibrary.Enums;
using PantioClassLibrary.Interfaces.Services;

namespace PantioAPI.Controllers
{
    [ApiController]
    [Route("api/users/{userId:guid}/store-connections")]
    public class StoreConnectionController(IStoreConnectionService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid userId, CancellationToken ct)
        {
            var connections = await service.GetByUserIdAsync(userId, ct);
            return Ok(connections);
        }

        [HttpPost("{chain}")]
        public async Task<IActionResult> Link(Guid userId, StoreChain chain, [FromBody] CompleteStoreConnectionLinkDto dto, CancellationToken ct)
        {
            var connection = await service.LinkAsync(userId, chain, dto, ct);
            if (connection is null)
                return BadRequest(new { message = "Store chain is not supported yet." });

            return CreatedAtAction(nameof(GetAll), new { userId }, connection);
        }

        [HttpPost("{connectionId:guid}/sync")]
        public async Task<IActionResult> Sync(Guid userId, Guid connectionId, CancellationToken ct)
        {
            var result = await service.SyncAsync(userId, connectionId, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpDelete("{connectionId:guid}")]
        public async Task<IActionResult> Disconnect(Guid userId, Guid connectionId, CancellationToken ct)
        {
            var disconnected = await service.DisconnectAsync(userId, connectionId, ct);
            return disconnected ? NoContent() : NotFound();
        }
    }
}

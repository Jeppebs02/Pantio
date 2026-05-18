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

        [HttpGet("{connectionId:guid}/pending-receipts")]
        public async Task<IActionResult> GetPendingReceipts(Guid userId, Guid connectionId, CancellationToken ct)
        {
            var receipts = await service.GetPendingReceiptsAsync(userId, connectionId, ct);
            return receipts is null ? NotFound() : Ok(receipts);
        }

        [HttpPost("{connectionId:guid}/import")]
        public async Task<IActionResult> ImportSelected(Guid userId, Guid connectionId, [FromBody] ImportSelectedReceiptsDto dto, CancellationToken ct)
        {
            try
            {
                var result = await service.ImportSelectedAsync(userId, connectionId, dto, ct);
                return result is null ? NotFound() : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { message = ex.Message });
            }
        }

        [HttpPost("{connectionId:guid}/sync")]
        public async Task<IActionResult> Sync(Guid userId, Guid connectionId, CancellationToken ct)
        {
            var result = await service.SyncAsync(userId, connectionId, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("{connectionId:guid}/sync-history")]
        public async Task<IActionResult> GetSyncHistory(Guid userId, Guid connectionId, CancellationToken ct)
        {
            var logs = await service.GetSyncHistoryAsync(userId, connectionId, ct);
            return Ok(logs);
        }

        [HttpPatch("{connectionId:guid}/auto-sync")]
        public async Task<IActionResult> UpdateAutoSync(
            Guid userId,
            Guid connectionId,
            [FromBody] UpdateStoreConnectionAutoSyncDto dto,
            CancellationToken ct)
        {
            var connection = await service.UpdateAutoSyncAsync(userId, connectionId, dto.AutoSyncEnabled, ct);
            return connection is null ? NotFound() : Ok(connection);
        }

        [HttpDelete("{connectionId:guid}")]
        public async Task<IActionResult> Disconnect(Guid userId, Guid connectionId, CancellationToken ct)
        {
            var disconnected = await service.DisconnectAsync(userId, connectionId, ct);
            return disconnected ? NoContent() : NotFound();
        }
    }
}

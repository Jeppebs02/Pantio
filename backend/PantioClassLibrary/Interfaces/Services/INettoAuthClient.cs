using System;
using System.Collections.Generic;
using System.Text;
using PantioClassLibrary.DTO;

namespace PantioClassLibrary.Interfaces.Services;

public interface INettoAuthClient
{
    Task<NettoTokenSet> ExchangeCodeAsync(string authorizationCode, string codeVerifier, string? redirectUri, CancellationToken ct = default);
    Task<NettoTokenSet> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<IReadOnlyCollection<NettoReceiptSummary>> GetReceiptSummariesAsync(string accessToken, string idToken, CancellationToken ct = default);
    Task<NettoReceiptDetail> GetReceiptDetailAsync(string accessToken, string idToken, string receiptType, string receiptId, CancellationToken ct = default);
}

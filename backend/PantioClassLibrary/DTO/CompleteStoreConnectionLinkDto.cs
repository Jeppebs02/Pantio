namespace PantioClassLibrary.DTO;

public sealed record CompleteStoreConnectionLinkDto(
    string AuthorizationCode,
    string CodeVerifier,
    string? RedirectUri
);

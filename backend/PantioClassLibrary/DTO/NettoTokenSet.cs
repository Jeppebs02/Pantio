using System;
using System.Collections.Generic;
using System.Text;

namespace PantioClassLibrary.DTO;

public sealed record NettoTokenSet(
    string AccessToken,
    string RefreshToken,
    string IdToken,
    int? ExpiresInSeconds
);

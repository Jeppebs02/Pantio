using System;
using System.Collections.Generic;
using System.Text;
using PantioClassLibrary.DTO;

namespace PantioClassLibrary.DTO;

public sealed record NettoReceiptDetail(
    IReadOnlyCollection<NettoReceiptLine> LineItems
);

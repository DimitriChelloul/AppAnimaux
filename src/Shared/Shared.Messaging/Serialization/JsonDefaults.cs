using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Messaging.Serialization;

using System.Text.Json;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}


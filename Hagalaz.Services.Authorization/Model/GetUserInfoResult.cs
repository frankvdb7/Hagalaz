using System.Collections.Generic;

namespace Hagalaz.Services.Authorization.Model
{
    public record GetUserInfoResult(IDictionary<string, object>? Claims);
}
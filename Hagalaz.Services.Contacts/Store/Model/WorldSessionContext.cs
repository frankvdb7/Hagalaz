using System;

namespace Hagalaz.Services.Contacts.Store.Model
{
    public record WorldSessionContext(
        int WorldId,
        string WorldName,
        string InstanceId = "",
        long Generation = 0,
        DateTimeOffset LeaseExpiresAt = default);
}

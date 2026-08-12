using System;
using System.Collections.Generic;
using System.Net;
using Hagalaz.Game.Configuration;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Configuration;

public sealed class WorldOptionsValidator : IValidateOptions<WorldOptions>
{
    public ValidateOptionsResult Validate(string? name, WorldOptions options)
    {
        var failures = new List<string>();
        if (options.Id <= 0)
        {
            failures.Add("World.Id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            failures.Add("World.Name is required.");
        }

        if (!IPAddress.TryParse(options.ListenHost, out _))
        {
            failures.Add("World.ListenHost must be a valid IP address.");
        }

        if (string.IsNullOrWhiteSpace(options.AdvertisedEndpoint.Host))
        {
            failures.Add("World.AdvertisedEndpoint.Host is required.");
        }

        if (options.AdvertisedEndpoint.Port is < 1 or > 65535)
        {
            failures.Add("World.AdvertisedEndpoint.Port must be between 1 and 65535.");
        }

        if (options.RegistrationLeaseDuration <= TimeSpan.Zero)
        {
            failures.Add("World.RegistrationLeaseDuration must be positive.");
        }

        if (options.RegistrationRenewalInterval <= TimeSpan.Zero ||
            options.RegistrationRenewalInterval >= options.RegistrationLeaseDuration)
        {
            failures.Add("World.RegistrationRenewalInterval must be positive and shorter than the lease duration.");
        }

        if (options.RegistrationRetryDelay <= TimeSpan.Zero)
        {
            failures.Add("World.RegistrationRetryDelay must be positive.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

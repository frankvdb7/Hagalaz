using System.Numerics;

namespace Hagalaz.Services.GameWorld.Configuration.Model;

public record RsaCacheConfig
{
    public const string Key = "CacheKeys";

    public BigInteger PrivateKey { get; set; } = BigInteger.MinusOne;
    public BigInteger ModulusKey { get; set; } = BigInteger.MinusOne;
    public BigInteger PublicKey { get; set; } = BigInteger.MinusOne;
}
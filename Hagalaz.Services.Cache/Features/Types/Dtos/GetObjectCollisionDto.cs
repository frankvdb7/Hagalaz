namespace Hagalaz.Services.Cache.Features.Types.Dtos;

/// <summary>IDs of the base object definitions to read.</summary>
public sealed record GetObjectCollisionDto(int[]? Ids);

/// <summary>The independent outcome of reading one requested ID.</summary>
public sealed record GetObjectCollisionResultDto(int Id, string Status, ObjectCollisionDto? Collision);

/// <summary>Collision fields from a placed object's base definition.</summary>
public sealed record ObjectCollisionDto(int SizeX, int SizeY, int ClipType, bool Solid, bool Gateway);

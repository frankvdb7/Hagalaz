using Microsoft.AspNetCore.Mvc;

namespace Hagalaz.Services.Cache.Features.Models.Dtos;

/// <summary>A texture's material definition, or the problem that prevented reading it.</summary>
public sealed record TextureMaterialResultDto(int Id, int Status, GetTextureMaterialDto? Material, ProblemDetails? Problem);

using Microsoft.AspNetCore.Mvc;

namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record ObjectRenderAppearanceKeyDto(int Id, int Shape, int Rotation);
public sealed record ObjectRenderAppearanceResultDto(int Id, int Shape, int Rotation, int Status, GetObjectRenderAppearanceDto? Appearance, ProblemDetails? Problem);

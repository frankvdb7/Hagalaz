using Microsoft.AspNetCore.Mvc;

namespace Hagalaz.Services.Cache.Features.Models.Dtos;

/// <summary>One decoded frame of an animation clip, keyed by its frame ID.</summary>
public sealed record AnimationClipFrameDto(int FrameId, GetAnimationFrameDto Frame);

/// <summary>An animation's sequence with every distinct frame it uses, or the problem that prevented reading it.</summary>
public sealed record AnimationClipResultDto(int Id, int Status, GetAnimationSequenceDto? Sequence, AnimationClipFrameDto[]? Frames, ProblemDetails? Problem);

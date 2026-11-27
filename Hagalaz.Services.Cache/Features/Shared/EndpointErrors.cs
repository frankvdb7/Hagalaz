using System;
using System.IO;
using System.Linq;
using FluentResults;
using Hagalaz.Cache.Logic.Rendering;
using Hagalaz.Cache.Maps;
using Hagalaz.Cache.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace Hagalaz.Services.Cache.Features.Shared;

internal static class EndpointErrors
{
    public static ProblemDetails Validation(string detail, int statusCode = StatusCodes.Status400BadRequest)
    {
        return new ProblemDetails
        {
            Title = "Request validation failed.",
            Detail = detail,
            Status = statusCode
        };
    }

    /// <summary>The response for a failed type mutation: 404 for a missing type entry, 422 for one that cannot be decoded.</summary>
    public static IResult TypeMutationFailure(ResultBase result, string type, int id)
    {
        if (result.HasError<TypeAbsentError>())
            return TypedResults.NotFound(Validation($"No {type} with ID {id} exists.", StatusCodes.Status404NotFound));
        if (result.HasError<TypeUnreadableError>())
            return TypedResults.Problem(title: "Unsupported cache data.", detail: $"The stored {type} {id} could not be decoded.", statusCode: StatusCodes.Status422UnprocessableEntity);
        throw new InvalidOperationException($"Unexpected type mutation failure: {string.Join("; ", result.Errors)}");
    }

    /// <summary>The response for a model that cannot be assembled: 404 when there is nothing to assemble, 400 for an unknown appearance, 422 for an unsupported one.</summary>
    public static IResult ModelAssemblyFailure(ResultBase result) => result.Errors.FirstOrDefault() switch
    {
        ModelSubjectAbsentError => TypedResults.NotFound(),
        UnknownAppearanceError => TypedResults.BadRequest(Validation("Unknown item model appearance.")),
        UnsupportedAppearanceError unsupported => TypedResults.Problem(title: "Unsupported object appearance", detail: unsupported.Message, statusCode: StatusCodes.Status422UnprocessableEntity),
        _ => throw new InvalidOperationException($"Unexpected model assembly failure: {string.Join("; ", result.Errors)}")
    };

    /// <summary>The response for a failed region map save: 404 for a missing terrain or landscape file, 409 for one written since it was loaded, 422 for one that cannot be decoded, 500 for a write that did not verify.</summary>
    public static IResult RegionMapFileFailure(ResultBase result, int id)
    {
        if (result.Errors.OfType<RegionMapFileConflictError>().FirstOrDefault() is { } conflict)
            return TypedResults.Conflict(Validation($"Region {id}'s {(conflict.Kind == RegionMapFile.Terrain ? "terrain" : "landscape")} was changed by another save after it was loaded. Nothing was written; reload the region and apply your edits again.", StatusCodes.Status409Conflict));
        if (result.Errors.OfType<RegionMapWriteUnverifiedError>().FirstOrDefault() is { } unverified)
            return TypedResults.Problem(title: "Save could not be verified", detail: $"Region {id}'s {(unverified.Kind == RegionMapFile.Terrain ? "terrain" : "landscape")} was written, but reopening it did not return the written data. Reload the region and check it before saving again.", statusCode: StatusCodes.Status500InternalServerError);
        if (result.Errors.OfType<RegionMapFileAbsentError>().FirstOrDefault() is { } absent)
            return TypedResults.NotFound(Validation($"Region {id} has no {(absent.Kind == RegionMapFile.Terrain ? "terrain" : "landscape")} archive.", StatusCodes.Status404NotFound));
        return result.Errors.OfType<RegionMapFileUnreadableError>().FirstOrDefault()?.Kind switch
        {
            RegionMapFile.Terrain => TypedResults.Problem(title: "Terrain cannot be changed", detail: $"Region {id}'s stored terrain could not be decoded.", statusCode: StatusCodes.Status422UnprocessableEntity),
            RegionMapFile.Landscape => TypedResults.Problem(title: "Landscape cannot be replaced", detail: $"Region {id}'s stored landscape is encrypted or corrupt, so saving would lose its placements.", statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => throw new InvalidOperationException($"Unexpected region map failure: {string.Join("; ", result.Errors)}")
        };
    }

    public static IResult MapException(Exception exception)
    {
        return exception switch
        {
            // Cache content the decoders cannot read: an unsupported format or malformed data, not a client or server fault.
            NotSupportedException or InvalidDataException or UnknownImageFormatException => TypedResults.Problem(
                title: "Unsupported cache data.",
                detail: exception.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity),
            FileNotFoundException => TypedResults.NotFound(new ProblemDetails
            {
                Title = "Cache file not found.",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            }),
            IOException => TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Cache operation failed.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            }),
            ArgumentException => TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid request.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            }),
            _ => TypedResults.Problem(
                title: "Unexpected cache error.",
                detail: exception.Message,
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}

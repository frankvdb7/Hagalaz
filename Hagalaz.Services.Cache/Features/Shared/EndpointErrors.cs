using System;
using System.IO;
using FluentResults;
using Hagalaz.Cache.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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

using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

    public static IResult MapException(Exception exception)
    {
        return exception switch
        {
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

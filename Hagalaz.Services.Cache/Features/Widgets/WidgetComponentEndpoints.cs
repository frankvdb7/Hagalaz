using System;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Widgets;

/// <summary>Widget archive members with preview bytes and decoded callbacks.</summary>
public sealed record WidgetComponentsResponse(WidgetComponentEntry[] Entries);

/// <summary>A widget archive slot and its callback metadata, if readable.</summary>
public sealed record WidgetComponentEntry(int SubFileId, string DataBase64, WidgetCallback[]? Callbacks);

/// <summary>A cached widget event, script target, arguments and trigger filter.</summary>
public sealed record WidgetCallback(string Event, int? ScriptId, WidgetCallbackValue[] Args, int[] Triggers);

/// <summary>A tagged value stored in a callback array.</summary>
public sealed record WidgetCallbackValue(string Type, object Value);

internal static class WidgetComponentEndpoints
{
    public static void MapWidgetComponentEndpoints(this IEndpointRouteBuilder group) =>
        group.MapGet("/widgets/{widgetId:int}/components", GetComponents)
            .WithSummary("Get widget preview bytes and decoded callbacks")
            .Produces<WidgetComponentsResponse>();

    private static IResult GetComponents(int widgetId, ICacheAPI cache)
    {
        using var archive = cache.ReadArchive(3, widgetId);
        var entries = (archive.Entries ?? []).Select((member, slot) =>
        {
            var bytes = member.ToArray();
            return new WidgetComponentEntry(slot, Convert.ToBase64String(bytes), WidgetCallbackDecoder.Decode(bytes));
        }).ToArray();
        return TypedResults.Ok(new WidgetComponentsResponse(entries));
    }
}

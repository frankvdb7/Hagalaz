using System;
using System.Text.Json;
using Hagalaz.Services.Characters.Model;
using Hagalaz.Services.Common.Model;

var json = "{\"sort\":{\"experience\":\"Desc\"},\"filter\":{\"page\":1,\"limit\":10,\"type\":\"Overall\"}}";
var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var request = JsonSerializer.Deserialize<GetAllCharacterStatisticsRequest>(json, options);

if (request?.Sort == null) {
    Console.WriteLine("Sort is null");
} else if (request.Sort.Experience == null) {
    Console.WriteLine("Sort.Experience is null");
} else {
    Console.WriteLine($"Sort.Experience is {request.Sort.Experience}");
}

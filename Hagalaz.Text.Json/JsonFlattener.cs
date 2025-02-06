using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hagalaz.Text.Json
{
    public class JsonFlattener
    {
        public static Dictionary<string, JsonNode> FlattenJson([StringSyntax(StringSyntaxAttribute.Json)] string json, JsonNodeOptions nodeOptions = default, JsonDocumentOptions documentOptions = default)
        {
            var keyValues = new Dictionary<string, JsonNode>(nodeOptions.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(json)) {
                return keyValues;
            }
            var rootNode = JsonNode.Parse(json, nodeOptions, documentOptions);
            if (rootNode == null)
            {
                return keyValues;
            }
            FlattenNode(rootNode, string.Empty, keyValues);
            return keyValues;
        }

        public static void FlattenNode(JsonNode node, string prefix, IDictionary<string, JsonNode> keyValues)
        {
            switch (node)
            {
                case JsonObject @object:
                    {
                        if (!string.IsNullOrEmpty(prefix))
                        {
                            keyValues[prefix] = @object;
                        }
                        foreach (var (key, value) in @object)
                        {
                            if (value == null)
                            {
                                continue;
                            }
                            var propertyName = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
                            FlattenNode(value, propertyName, keyValues);
                        }

                        break;
                    }
                case JsonArray array: keyValues[prefix] = array; break;
                default: keyValues[prefix] = node; break;
            }
        }

        public static string UnflattenJson(IDictionary<string, JsonNode> keyValues, JsonNodeOptions nodeOptions = default)
        {
            var rootNode = Unflatten(keyValues, nodeOptions);
            return rootNode.ToJsonString();
        }

        private static JsonNode Unflatten(IDictionary<string, JsonNode> keyValues, JsonNodeOptions nodeOptions = default) 
        {
            var rootNode = new JsonObject(nodeOptions);
            foreach (var (key, value) in keyValues)
            {
                var keyParts = key.Split(".");
                var currentNode = rootNode;
                for (var i = 0; i < keyParts.Length; i++)
                {
                    var keyPart = keyParts[i];
                    if (i == keyParts.Length - 1)
                    {
                        currentNode![keyPart] = JsonSerializer.SerializeToNode(value);
                    } 
                    else
                    {
                        if (!currentNode!.ContainsKey(keyPart) || currentNode[keyPart] is not JsonObject)
                        {
                            currentNode[keyPart] = new JsonObject(nodeOptions);
                        }

                        currentNode = currentNode[keyPart] as JsonObject;
                    }
                }
            }
            return rootNode;
        }
    }
}

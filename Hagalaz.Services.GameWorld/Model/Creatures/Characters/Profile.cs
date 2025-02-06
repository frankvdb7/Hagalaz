using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Text.Json;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    public class Profile : IProfile, IHydratable<HydratedProfileDto>, IDehydratable<HydratedProfileDto>
    {
        private readonly JsonDictionary _data;

        public Profile() : this(new(JsonSerializerOptions.Default)
        {
            PropertyNameCaseInsensitive = true
        }) { }

        public Profile(JsonSerializerOptions serializerOptions) => _data = new JsonDictionary(serializerOptions);

        public void SetValue<T>(string key, T value) where T : struct => _data.SetValue(key, value);

        public void SetValue(string key, string value) => _data.SetValue(key, value);

        public void SetObject<T>(string key, T value) where T : class => _data.SetObject(key, value);

        public void SetArray<T>(string key, T[] value) => _data.SetArray(key, value);

        public bool TryGetValue<T>(string key, out T value, T defaultValue = default) where T : struct => _data.TryGetValue(key, out value, defaultValue);

        public bool TryGetValue(string key, out string value, string defaultValue = "") => _data.TryGetValue(key, out value, defaultValue);

        public bool TryGetObject<T>(string key, [NotNullWhen(true)] out T? value, T? defaultValue = default) where T : class =>
            _data.TryGetObject(key, out value, defaultValue);

        public T GetValue<T>(string key, T defaultValue = default) where T : struct => _data.GetValue(key, defaultValue);

        public string GetValue(string key, string defaultValue = "") => _data.GetValue(key, defaultValue);

        [return: NotNullIfNotNull(nameof(defaultValue))]
        public T? GetObject<T>(string key, T? defaultValue = default) where T : class => _data.GetObject(key, defaultValue);

        public IEnumerable<T> GetArray<T>(string key, IEnumerable<T>? defaultValue = default) => _data.GetArray(key, defaultValue);

        public void Hydrate(HydratedProfileDto hydration) => _data.FromJson(hydration.JsonData);

        public HydratedProfileDto Dehydrate() =>
            new()
            {
                JsonData = _data.ToJson()
            };
    }
}
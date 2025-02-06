using System.Text.Json;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CharacterProfileTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Default)
        {
            PropertyNameCaseInsensitive = true
        };

        private const string _json = /*lang=json*/
                    """
                    {
                        "a":{
                            "b":1,
                            "c":{
                                "d":2
                            }
                        },
                        "e":[3,4],
                        "f":5,
                        "g":1,
                        "pascalcaseproperty": 15,
                        "h":"stringvalue"
                    }
                    """;
        private record JsonDto
        {
            public ADto A { get; init; }
            public int[] E { get; init; }
            public int F { get; init; }
            public DtoEnum G { get; init; }
            public int PascalCaseProperty { get; init; }
            public string H { get; init; } = string.Empty;
        }

        private record ADto
        {
            public int B { get; init; }
            public CDto C { get; init; } = default!;
        }

        private record CDto
        {
            public int D { get; init; }
        }

        private enum DtoEnum : int
        {
            None = 0,
            One = 1
        }

        [TestMethod]
        public void Profile_Hydration()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            Assert.AreEqual(1, profile.GetValue<int>("a.b"));
            Assert.AreEqual(2, profile.GetValue<int>("a.c.d"));
            CollectionAssert.AreEqual(new int[] { 3, 4 }, profile.GetArray<int>("e").ToArray());
            Assert.AreEqual(new ADto { B = 1, C = new CDto { D = 2 } }, profile.GetObject<ADto>("a"));
            Assert.AreEqual(DtoEnum.One, profile.GetValue<DtoEnum>("g"));
            Assert.AreEqual(15, profile.GetValue<int>("pascalcaseproperty"));
        }

        [TestMethod]
        public void Profile_Dehydration()
        {
            var profile = new Profile(_jsonOptions);

            profile.SetValue("a.b.c.d", 5);
            var dto = profile.Dehydrate();
            Assert.AreEqual(dto.JsonData, "{\"a\":{\"b\":{\"c\":{\"d\":5}}}}", true);
        }

        [TestMethod]
        public void Profile_Set_Value()
        {
            var profile = new Profile();
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            profile.SetValue("a.b", 5);

            Assert.AreEqual(5, profile.GetValue<int>("a.b"));
        }

        [TestMethod]
        public void Profile_Set_Value_String()
        {
            var profile = new Profile();
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            profile.SetValue("h", "newvalue");

            Assert.AreEqual("newvalue", profile.GetValue("h"));
        }

        [TestMethod]
        public void Profile_Set_Non_Existing_Value()
        {
            var profile = new Profile(_jsonOptions);

            profile.SetValue("a.b.c", 5);

            Assert.AreEqual(5, profile.GetValue<int>("a.b.c"));
        }

        [TestMethod]
        public void Profile_Get_Enum_Value()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            Assert.AreEqual(DtoEnum.One, profile.GetValue<DtoEnum>("g"));
        }

        [TestMethod]
        public void Profile_Get_Value_Default()
        {
            var profile = new Profile(_jsonOptions);

            Assert.AreEqual(10, profile.GetValue("a.b.c.d.e.f", 10));
        }

        [TestMethod]
        public void Profile_Get_Value_Default_String()
        {
            var profile = new Profile(_jsonOptions);

            Assert.AreEqual("defaultstring", profile.GetValue("a.b.c.d.e.f", "defaultstring"));
        }

        [TestMethod]
        public void Profile_Try_Get_Value()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            var result = profile.TryGetValue<int>("a.b", out var value);
            Assert.AreEqual(result, true);
            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void Profile_Try_Get_Value_String()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            var result = profile.TryGetValue("h", out var value);
            Assert.AreEqual(result, true);
            Assert.AreEqual("stringvalue", value);
        }

        [TestMethod]
        public void Profile_Set_Object()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            var dto = new ADto { B = 7, C = new CDto { D = 8 } };
            profile.SetObject("a", dto);
            Assert.AreEqual(dto, profile.GetObject<ADto>("a"));
            Assert.AreEqual(7, profile.GetValue<int>("a.b"));
            Assert.AreEqual(8, profile.GetValue<int>("a.c.d"));
        }

        [TestMethod]
        public void Profile_Get_Object()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            ; var result = profile.GetObject<ADto>("a");
            var expected = new ADto { B = 1, C = new CDto { D = 2 } };
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Profile_Get_Object_Null()
        {
            var profile = new Profile(_jsonOptions);
            Assert.AreEqual(null, profile.GetObject<ADto>("b.d.f.g"));
        }

        [TestMethod]
        public void Profile_Get_Object_Default()
        {
            var profile = new Profile(_jsonOptions);

            var dto = new ADto {B = 7, C = new CDto { D = 8 } };
            Assert.AreEqual(dto, profile.GetObject("b.d.f.g", dto));
        }

        [TestMethod]
        public void Profile_Try_Get_Object()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            var result = profile.TryGetObject<ADto>("a", out var value);
            Assert.AreEqual(true, result);
            Assert.AreEqual(value, new ADto { B = 1, C = new CDto { D = 2 } });
        }

        [TestMethod]
        public void Profile_Set_Array()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            var array = new int[] { 5, 6 };
            profile.SetArray("e", array);

            CollectionAssert.AreEqual(array, profile.GetArray<int>("e").ToArray());
        }


        [TestMethod]
        public void Profile_Get_Array_Default()
        {
            var profile = new Profile(_jsonOptions);
            profile.Hydrate(new HydratedProfileDto { JsonData = _json });

            var expected = new int[] { 5, 6 };
            CollectionAssert.AreEqual(expected, profile.GetArray("e.f.g.f", expected).ToArray());
        }
    }
}

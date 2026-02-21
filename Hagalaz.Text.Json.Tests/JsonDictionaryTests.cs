using System.Text.Json;

namespace Hagalaz.Text.Json.Tests
{
    [TestClass]
    public class JsonDictionaryTests
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
                "h":"stringvalue",
                "i":999
            }
            """;

        private record JsonDto
        {
            public ADto A { get; init; } = default!;
            public int[] E { get; init; } = default!;
            public int F { get; init; }
            public DtoEnum G { get; init; }
            public DtoEnum I { get; init; }
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
        public void Dictionary_FromJson()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            Assert.AreEqual(1, dictionary.GetValue<int>("a.b"));
            Assert.AreEqual(2, dictionary.GetValue<int>("a.c.d"));
            CollectionAssert.AreEqual(new int[]
                {
                    3, 4
                },
                dictionary.GetArray<int>("e").ToArray());
            Assert.AreEqual(new ADto
                {
                    B = 1,
                    C = new CDto
                    {
                        D = 2
                    }
                },
                dictionary.GetObject<ADto>("a"));
            Assert.AreEqual(DtoEnum.One, dictionary.GetValue<DtoEnum>("g"));
            Assert.AreEqual(15, dictionary.GetValue<int>("pascalcaseproperty"));
        }

        [TestMethod]
        public void Dictionary_ToJson()
        {
            var dictionary = new JsonDictionary(_jsonOptions);

            dictionary.SetValue("a.b.c.d", 5);
            var json = dictionary.ToJson();
            Assert.AreEqual("{\"a\":{\"b\":{\"c\":{\"d\":5}}}}", json, true);
        }

        [TestMethod]
        public void Dictionary_Set_Value()
        {
            var dictionary = new JsonDictionary();
            dictionary.FromJson(_json);

            dictionary.SetValue("a.b", 5);

            Assert.AreEqual(5, dictionary.GetValue<int>("a.b"));
        }

        [TestMethod]
        public void Dictionary_Set_Value_String()
        {
            var dictionary = new JsonDictionary();
            dictionary.FromJson(_json);

            dictionary.SetValue("h", "newvalue");

            Assert.AreEqual("newvalue", dictionary.GetValue("h"));
        }

        [TestMethod]
        public void Dictionary_Set_Non_Existing_Value()
        {
            var profile = new JsonDictionary(_jsonOptions);

            profile.SetValue("a.b.c", 5);

            Assert.AreEqual(5, profile.GetValue<int>("a.b.c"));
        }

        [TestMethod]
        public void Dictionary_Get_Enum_Value()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            Assert.AreEqual(DtoEnum.One, dictionary.GetValue<DtoEnum>("g"));
        }

        [TestMethod]
        public void Dictionary_Get_Enum_Value_Non_Existing()
        {
            var dictionary = new JsonDictionary(_jsonOptions);

            Assert.AreEqual(DtoEnum.None, dictionary.GetValue<DtoEnum>("a.b.c.d.e.f"));
        }

        [TestMethod]
        public void Dictionary_Get_Invalid_Enum_Value()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            Assert.AreEqual(DtoEnum.None, dictionary.GetValue<DtoEnum>("i"));
        }

        [TestMethod]
        public void Dictionary_Get_Value_Default()
        {
            var dictionary = new JsonDictionary(_jsonOptions);

            Assert.AreEqual(10, dictionary.GetValue("a.b.c.d.e.f", 10));
        }

        [TestMethod]
        public void Dictionary_Get_Value_Non_Existing()
        {
            var dictionary = new JsonDictionary(_jsonOptions);

            Assert.AreEqual(0, dictionary.GetValue<int>("a.b.c.d.e.f"));
        }

        [TestMethod]
        public void Dictionary_Get_Value_Default_String()
        {
            var dictionary = new JsonDictionary(_jsonOptions);

            Assert.AreEqual("defaultstring", dictionary.GetValue("a.b.c.d.e.f", "defaultstring"));
        }

        [TestMethod]
        public void Dictionary_Get_Value_String_Non_Existing()
        {
            var dictionary = new JsonDictionary(_jsonOptions);

            Assert.AreEqual(string.Empty, dictionary.GetValue("a.b.c.d.e.f"));
        }

        [TestMethod]
        public void Dictionary_Try_Get_Value()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var result = dictionary.TryGetValue<int>("a.b", out var value);
            Assert.IsTrue(result);
            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void Dictionary_Try_Get_Enum_Value()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var result = dictionary.TryGetValue<DtoEnum>("g", out var value);
            Assert.IsTrue(result);
            Assert.AreEqual(DtoEnum.One, value);
        }

        [TestMethod]
        public void Dictionary_Try_Get_Enum_Value_Non_Existing()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var result = dictionary.TryGetValue<DtoEnum>("a.b.c.d.e.f.g", out var value);
            Assert.IsFalse(result);
            Assert.AreEqual(DtoEnum.None, value);
        }

        [TestMethod]
        public void Dictionary_Try_Get_Invalid_Enum_Value()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var result = dictionary.TryGetValue<DtoEnum>("i", out var value);
            Assert.IsFalse(result);
            Assert.AreEqual(DtoEnum.None, value);
        }

        [TestMethod]
        public void Dictionary_Try_Get_Value_String()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var result = dictionary.TryGetValue("h", out var value);
            Assert.IsTrue(result);
            Assert.AreEqual("stringvalue", value);
        }

        [TestMethod]
        public void Dictionary_Set_Object()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var dto = new ADto
            {
                B = 7,
                C = new CDto
                {
                    D = 8
                }
            };
            dictionary.SetObject("a", dto);
            Assert.AreEqual(dto, dictionary.GetObject<ADto>("a"));
            Assert.AreEqual(7, dictionary.GetValue<int>("a.b"));
            Assert.AreEqual(8, dictionary.GetValue<int>("a.c.d"));
        }

        [TestMethod]
        public void Dictionary_Get_Object()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            ;
            var result = dictionary.GetObject<ADto>("a");
            var expected = new ADto
            {
                B = 1,
                C = new CDto
                {
                    D = 2
                }
            };
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Dictionary_Get_Object_Null()
        {
            var profile = new JsonDictionary(_jsonOptions);
            Assert.IsNull(profile.GetObject<ADto>("b.d.f.g"));
        }

        [TestMethod]
        public void Dictionary_Get_Object_Default()
        {
            var dictionary = new JsonDictionary(_jsonOptions);

            var dto = new ADto
            {
                B = 7,
                C = new CDto
                {
                    D = 8
                }
            };
            Assert.AreEqual(dto, dictionary.GetObject("b.d.f.g", dto));
        }

        [TestMethod]
        public void Dictionary_Try_Get_Object()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var result = dictionary.TryGetObject<ADto>("a", out var value);
            Assert.IsTrue(result);
            Assert.AreEqual(value,
                new ADto
                {
                    B = 1,
                    C = new CDto
                    {
                        D = 2
                    }
                });
        }

        [TestMethod]
        public void Dictionary_Try_Get_Object_NonExisting()
        {
            var dictionary = new JsonDictionary(_jsonOptions);

            var result = dictionary.TryGetObject<ADto>("non.existing.key", out var value);
            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        [TestMethod]
        public void Dictionary_Set_Array()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var array = new int[]
            {
                5, 6
            };
            dictionary.SetArray("e", array);

            CollectionAssert.AreEqual(array, dictionary.GetArray<int>("e").ToArray());
        }


        [TestMethod]
        public void Dictionary_Get_Array_Default()
        {
            var dictionary = new JsonDictionary(_jsonOptions);
            dictionary.FromJson(_json);

            var expected = new int[]
            {
                5, 6
            };
            CollectionAssert.AreEqual(expected, dictionary.GetArray("e.f.g.f", expected).ToArray());
        }
    }
}
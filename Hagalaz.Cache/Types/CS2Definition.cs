using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    public class Cs2Definition : ICs2Definition
    {
        public int Id { get; }
        public string Name { get; internal set; }
        public int IntLocalsCount { get; internal set; }
        public int StringLocalsCount { get; internal set; }
        public int LongLocalsCount { get; internal set; }
        public int IntArgsCount { get; internal set; }
        public int StringArgsCount { get; internal set; }
        public int LongArgsCount { get; internal set; }
        public IReadOnlyDictionary<int, int>[] Switches { get; internal set; }
        public IReadOnlyList<int> Opcodes { get; internal set; }
        public IReadOnlyList<int> IntPool { get; internal set; }
        public IReadOnlyList<string> StringPool { get; internal set; }
        public IReadOnlyList<long> LongPool { get; internal set; }

        public Cs2Definition(int id)
        {
            Id = id;
            Name = string.Empty;
            Switches = [];
            Opcodes = [];
            IntPool = [];
            StringPool = [];
            LongPool = [];
        }
    }
}

using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Types
{
    public interface ICs2Definition : IType
    {
        string Name { get; }
        int IntLocalsCount { get; }
        int StringLocalsCount { get; }
        int LongLocalsCount { get; }
        int IntArgsCount { get; }
        int StringArgsCount { get; }
        int LongArgsCount { get; }
        IReadOnlyDictionary<int, int>[] Switches { get; }
        IReadOnlyList<int> Opcodes { get; }
        IReadOnlyList<int> IntPool { get; }
        IReadOnlyList<string> StringPool { get; }
        IReadOnlyList<long> LongPool { get; }
    }
}

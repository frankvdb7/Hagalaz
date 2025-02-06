using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public interface IContactList<T> : IEnumerable<T> where T : Contact
    {
        void Add(T contact);
        void Set(IEnumerable<T> contacts);
        void Remove(uint masterId);
        void Update(uint masterId, T contact);
        bool Contains(uint masterId);
        T? Get(uint masterId);
    }
}
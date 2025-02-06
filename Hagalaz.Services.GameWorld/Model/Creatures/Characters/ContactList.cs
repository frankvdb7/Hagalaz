using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    public class ContactList<T> : IContactList<T> where T : Contact
    {
        private List<T> _contacts = [];
        public void Add(T contact) => _contacts.Add(contact);

        public void Remove(uint masterId)
        {
            var contact = _contacts.SingleOrDefault(c => c.MasterId == masterId);
            if (contact != null)
            {
                _contacts.Remove(contact);
            }
        }

        public void Update(uint masterId, T contact)
        {
            var contactIndex = _contacts.IndexOf(f => f.MasterId == masterId);
            _contacts.Insert(contactIndex, contact);
        }

        public bool Contains(uint masterId) => _contacts.Any(c => c.MasterId == masterId);
        public T? Get(uint masterId) => _contacts.SingleOrDefault(c => c.MasterId == masterId);
        public void Set(IEnumerable<T> contacts) => _contacts = [..contacts];
        public IEnumerator<T> GetEnumerator() => _contacts.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _contacts.GetEnumerator();
    }
}
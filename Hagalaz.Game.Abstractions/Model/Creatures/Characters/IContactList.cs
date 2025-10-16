using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a list of contacts, such as a friends or ignore list.
    /// </summary>
    /// <typeparam name="T">The type of contact, which must inherit from <see cref="Contact"/>.</typeparam>
    public interface IContactList<T> : IEnumerable<T> where T : Contact
    {
        /// <summary>
        /// Adds a new contact to the list.
        /// </summary>
        /// <param name="contact">The contact to add.</param>
        void Add(T contact);
        /// <summary>
        /// Clears the existing list and replaces it with a new set of contacts.
        /// </summary>
        /// <param name="contacts">The new collection of contacts to set.</param>
        void Set(IEnumerable<T> contacts);
        /// <summary>
        /// Removes a contact from the list by their master account ID.
        /// </summary>
        /// <param name="masterId">The master ID of the contact to remove.</param>
        void Remove(uint masterId);
        /// <summary>
        /// Updates an existing contact's information.
        /// </summary>
        /// <param name="masterId">The master ID of the contact to update.</param>
        /// <param name="contact">The new contact information.</param>
        void Update(uint masterId, T contact);
        /// <summary>
        /// Checks if a contact with the specified master ID exists in the list.
        /// </summary>
        /// <param name="masterId">The master ID to check for.</param>
        /// <returns><c>true</c> if the contact exists; otherwise, <c>false</c>.</returns>
        bool Contains(uint masterId);
        /// <summary>
        /// Retrieves a contact from the list by their master account ID.
        /// </summary>
        /// <param name="masterId">The master ID of the contact to retrieve.</param>
        /// <returns>The contact if found; otherwise, <c>null</c>.</returns>
        T? Get(uint masterId);
    }
}
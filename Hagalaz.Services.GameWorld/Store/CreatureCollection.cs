using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Store
{
    public class CreatureCollection<TCreature> : ICreatureCollection<TCreature> where TCreature : class, ICreature
    {
        private const int _startIndex = 1;

        private readonly TCreature?[] _creatures;
        private int _size;
        private int _version;
        private int _capacity;
        private readonly IndexSet _indexSet;

        public TCreature? this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_creatures.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _creatures[index];
            }
        }

        public int Capacity => _capacity;

        public int Count => _size;

        public CreatureCollection(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            _capacity = capacity;
            _creatures = new TCreature[_startIndex + capacity];
            _indexSet = new IndexSet(_startIndex, capacity);
        }

        public bool Add(TCreature creature)
        {
            if ((uint)_size >= (uint)_creatures.Length)
            {
                return false;
            }

            var index = _indexSet.Get();
            if (index == -1)
            {
                return false;
            }

            creature.Index = index;
            _creatures[index] = creature;
            _size++;
            _version++;
            return true;
        }

        public bool Remove(TCreature creature)
        {
            var index = creature.Index;
            if (index < _startIndex || (uint)index >= (uint)_creatures.Length)
            {
                return false;
            }
            RemoveAt(index);
            return true;
        }

        private void RemoveAt(int index)
        {
            if (index < _startIndex || (uint)index >= (uint)_creatures.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _indexSet.Return(index);
            _creatures[index] = null;
            _size--;
            _version++;
        }

        public IEnumerator<TCreature> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        private class IndexSet
        {
            private readonly HashSet<int> _indices = [];
            private readonly HashSet<int> _usedIndices = [];

            public IReadOnlyCollection<int> UsedIndices => _usedIndices;

            internal IndexSet(int startIndex, int capacity)
            {
                _indices = [..Enumerable.Range(startIndex, capacity)];
                _usedIndices = [];
            }

            public int Get()
            {
                if (_indices.Count == 0)
                {
                    return -1;
                }
                var index = _indices.First();
                _indices.Remove(index);
                _usedIndices.Add(index);
                return index;
            }

            public void Return(int index)
            {
                if (_indices.Contains(index))
                {
                    throw new InvalidOperationException("Tried to return available index");
                }
                if (!_usedIndices.Contains(index))
                {
                    throw new InvalidOperationException("Tried to return invalid used index");
                }
                _usedIndices.Remove(index);
                _indices.Add(index);
            }
        }

        private struct Enumerator : IEnumerator<TCreature>, IEnumerator
        {
            private readonly CreatureCollection<TCreature> _container;
            private readonly int _version;
            private TCreature? _current;
            private readonly IEnumerator<int> _usedIndexEnumerator;

            public TCreature Current => _current!;

            internal Enumerator(CreatureCollection<TCreature> container)
            {
                _container = container;
                _version = container._version;
                _usedIndexEnumerator = container._indexSet.UsedIndices.GetEnumerator();
                _current = default;
            }

            public bool MoveNext()
            {
                var local = _container;
                if (_version == local._version && _usedIndexEnumerator.MoveNext())
                {                    
                    _current = _container._creatures[_usedIndexEnumerator.Current];
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _container._version)
                {
                    throw new InvalidOperationException();
                }

                _current = default;
                return false;
            }

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                if (_version != _container._version)
                {
                    throw new InvalidOperationException();
                }

                _usedIndexEnumerator.Reset();
                _current = default;
            }

            public void Dispose() => _usedIndexEnumerator.Dispose();
        }
    }
}
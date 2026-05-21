using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    public interface IMapRegion
    {
        int Id { get; }
        int[] XteaKeys { get; }
        ILocation BaseLocation { get; }
        IVector3 Size { get; }
        bool IsDynamic { get; }
        bool IsLoaded { get; }
        bool IsDestroyed { get; }
        bool CanDestroy();
        bool CanSuspend();
        void Load();
        void Resume();
        void Suspend();
        Task DestroyAsync();
        IEnumerable<ICharacter> FindAllCharacters();
        IEnumerable<INpc> FindAllNpcs();
        void ForEachCharacter<TState>(Action<ICharacter, TState> action, TState state);
        void ForEachNpc<TState>(Action<INpc, TState> action, TState state);
        IEnumerable<IGroundItem> FindAllGroundItems();
        IEnumerable<IGameObject> FindAllGameObjects();
        void MakeStandard();
        void MakeDynamic();
        void Add(INpc npc);
        void Add(ICharacter character);
        void Add(IGroundItem item);
        void Add(IGameObject gameObj);
        void Remove(ICharacter character);
        void Remove(INpc npc);
        void Remove(IGameObject gameObj);
        void Remove(IGroundItem item);
        void FlagCollision(int localX, int localY, int z, CollisionFlag flag);
        void UnFlagCollision(int localX, int localY, int z, CollisionFlag flag);
        void UnFlagCollision(IGameObject gameObject);
        void FlagCollision(IGameObject gameObject);
        Task MajorUpdateTick();
        Task MajorClientPrepareUpdateTick();
        Task MajorClientUpdateTick();
        Task MajorClientUpdateResetTick();
        void SendFullPartUpdates(ICharacter character);
        IMapRegionPart GetRegionPartData(int partX, int partY, int z);
        void WriteBlock(int partX, int partY, int z, int drawPartX, int drawPartY, int drawPartZ);
        void QueueUpdate(IRegionPartUpdate update);
        IGameObject? FindStandardGameObject(int localX, int localY, int z);
        IEnumerable<IGameObject> FindGameObjects(int localX, int localY, int z);
        CollisionFlag GetCollision(int localX, int localY, int z);
    }
}

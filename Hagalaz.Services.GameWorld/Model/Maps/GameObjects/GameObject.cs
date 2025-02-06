using System;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Model.Maps.GameObjects
{
    /// <summary>
    /// Class which represents game object.
    /// </summary>
    public class GameObject : IGameObject
    {
        /// <summary>
        /// Contains Id of this object.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Contains name of this object.
        /// </summary>
        public string Name => Definition.Name;

        /// <summary>
        /// Contains location of this object.
        /// </summary>
        public ILocation Location { get; }

        /// <summary>
        /// Contains Region of this object.
        /// </summary>
        public IMapRegion Region
        {
            get
            {
                var regionManager = ServiceLocator.Current.GetInstance<IMapRegionService>();
                return regionManager.GetOrCreateMapRegion(Location.RegionId, Location.Dimension, false);
            }
        }

        /// <summary>
        /// Contains definition of this object.
        /// </summary>
        public IGameObjectDefinition Definition { get; }

        /// <summary>
        /// Contains object script.
        /// </summary>
        public IGameObjectScript Script { get; }

        /// <summary>
        /// Contains boolean if this object is static.
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Contains boolean if this object is disabled.
        /// </summary>
        public bool IsDisabled { get; private set; }

        /// <summary>
        /// Contains object rotation.
        /// </summary>
        public int Rotation { get; set; }

        /// <summary>
        /// Contains object shape type.
        /// </summary>
        public ShapeType ShapeType { get; }

        /// <summary>
        /// Gets the average size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public int Size => (SizeX + SizeY) / 2;

        /// <summary>
        /// Gets the size x.
        /// </summary>
        /// <value>
        /// The size x.
        /// </value>
        public int SizeX
        {
            get
            {
                if ((Rotation & 2) == 0)
                {
                    return Definition.SizeX;
                }

                return Definition.SizeY;
            }
        }

        /// <summary>
        /// Gets the size y.
        /// </summary>
        /// <value>
        /// The size y.
        /// </value>
        public int SizeY
        {
            get
            {
                if ((Rotation & 2) == 0)
                {
                    return Definition.SizeY;
                }

                return Definition.SizeX;
            }
        }

        public bool IsDestroyed { get; private set; }

        /// <summary>
        /// Construct's new game object.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <param name="location">The location.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="shapeType">Type of the spawn.</param>
        /// <param name="isStatic">if set to <c>true</c> [static object].</param>
        /// <param name="gameObjectDefinition"></param>
        /// <param name="script">The script.</param>
        public GameObject(
            int id, ILocation location, int rotation, ShapeType shapeType, bool isStatic, IGameObjectDefinition gameObjectDefinition, IGameObjectScript script)
        {
            Id = id;
            Definition = gameObjectDefinition;
            Location = location;
            IsStatic = isStatic;
            IsDisabled = false;
            Rotation = rotation;
            ShapeType = shapeType;
            Script = script;
            Script.Initialize(this);
        }

        /// <summary>
        /// Happens when object is spawned/enabled.
        /// </summary>
        public void OnSpawn() => Script.OnSpawn();

        /// <summary>
        /// Get's called when this object is disabled.
        /// </summary>
        public void Disable()
        {
            if (!IsStatic)
            {
                throw new Exception("Non-Static game objects can't be disabled.");
            }

            Script.OnDisable();
            IsDisabled = true;
        }

        public void Enable()
        {
            if (!IsStatic)
            {
                throw new Exception("Non-Static game objects can't be enabled.");
            }

            Script.OnEnable();
            IsDisabled = false;
        }

        /// <summary>
        /// Destroys this entity.
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                throw new InvalidOperationException($"{this} is already destroyed!");
            }

            IsDestroyed = true;
            Script.OnDestroy();
        }

        /// <summary>
        /// @IEntity
        /// Notifies Entity that it has been linked to new region.
        /// </summary>
        public void OnRegionChange() => throw new Exception("GameObjects can't change regions!");

        /// <summary>
        /// Get's if entity can be destroyed.
        /// </summary>
        /// <returns></returns>
        public bool CanDestroy() => Script.CanDestroy();

        /// <summary>
        /// Get's if entity can be suspended.
        /// </summary>
        /// <returns></returns>
        public bool CanSuspend() => Script.CanSuspend();

        public override string ToString() => $"obj[name={Definition.Name},id={Definition.Id},shape={ShapeType},rotation={Rotation},loc=({Location})]";
    }
}
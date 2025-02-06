using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Messages.Protocol;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class Configurations
    /// </summary>
    public class Configurations : IConfigurations
    {
        /// <summary>
        /// Owner of this class.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Construct's new configurations class for given
        /// character.
        /// </summary>
        /// <param name="owner">Owner of this class.</param>
        public Configurations(ICharacter owner) => _owner = owner;

        /// <summary>
        /// Send's standart configuration to character's client.
        /// </summary>
        /// <param name="configurationID">Id of the configuration to send.</param>
        /// <param name="value">Value of the configuration.</param>
        public void SendStandardConfiguration(int configurationID, int value) =>
            _owner.Session.SendMessage(new SetConfigMessage
            {
                Id = configurationID, Value = value
            });

        /// <summary>
        /// Sends the client var bit.
        /// 'ConfigByFile'
        /// </summary>
        /// <param name="fileID">The file Id.</param>
        /// <param name="value">The value.</param>
        public void SendBitConfiguration(int fileID, int value) =>
            _owner.Session.SendMessage(new SetVarpBitMessage
            {
                Id = fileID, Value = value
            });

        /// <summary>
        /// Send's global cs2int configuration to character's client.
        /// </summary>
        /// <param name="configurationID">Id of the configuration to send.</param>
        /// <param name="value">Value of the configuration.</param>
        public void SendGlobalCs2Int(int configurationID, int value) =>
            _owner.Session.SendMessage(new SetCS2IntMessage
            {
                Id = configurationID, Value = value
            });

        /// <summary>
        /// Send's global cs2string configuration to character's client.
        /// </summary>
        /// <param name="configurationID">Id of the configuration to send.</param>
        /// <param name="value">Value of the configuration.</param>
        public void SendGlobalCs2String(int configurationID, string value) =>
            _owner.Session.SendMessage(new SetCS2StringMessage
            {
                Id = configurationID, Value = value
            });

        /// <summary>
        /// Send's CS2Script call to character's client.
        /// </summary>
        /// <param name="scriptID">Id of the script to send.</param>
        /// <param name="callParameters">Parameters of the script to call , can only contain ints and strings.</param>
        public void SendCs2Script(int scriptID, object[] callParameters) =>
            _owner.Session.SendMessage(new RunCS2ScriptMessage
            {
                Id = scriptID, Parameters = callParameters
            });

        /// <summary>
        /// Sends items to given client container.
        /// </summary>
        /// <param name="containerId">Id of the client container to send items to.</param>
        /// <param name="split">Whether items should be split.</param>
        /// <param name="container">Container which items should be sent.</param>
        /// <param name="slots">The slots.</param>
        public void SendItems(int containerId, bool split, IContainer<IItem?> container, HashSet<int>? slots = null)
        {
            if (slots != null && container.Capacity != slots.Count)
            {
                _owner.Session.SendMessage(new SetItemContainerMessage
                {
                    Id = containerId,
                    Split = split,
                    Items = slots.ToDictionary(slot => slot,
                        slot => container[slot] != null
                            ? new ItemDto
                            {
                                Id = container[slot]!.Id, Count = container[slot]!.Count
                            } as IItemBase
                            : null)
                });
            }
            else
            {
                _owner.Session.SendMessage(new DrawItemContainerMessage
                {
                    Id = containerId, Split = split, Items = container.ToList(), Capacity = container.Capacity
                });
            }
        }


        /// <summary>
        /// Send's minimap configuration.
        /// </summary>
        /// <param name="type">Type of the minimap.</param>
        public void SendMinimapConfiguration(MinimapType type) =>
            _owner.Session.SendMessage(new SetMiniMapTypeMessage
            {
                MinimapType = type,
            });

        /// <summary>
        /// Send's walk here option which is displayed on tiles text and cursor spriteID.
        /// </summary>
        /// <param name="optionText">Text which should be showed instead of walk here.</param>
        /// <param name="cursorSpriteID">Id of the sprite which should be showed on cursor.</param>
        public void SendWalkHereConfiguration(string optionText, short cursorSpriteID)
        {
            //_owner.Session.SendPacketAsync(new SetWalkhereOptionPacketComposer(optionText, cursorSpriteID));
        }

        /// <summary>
        /// Send's minimap flag at specific map location.
        /// </summary>
        /// <param name="mapX">X in the map.</param>
        /// <param name="mapY">Y in the map.</param>
        public void SendSetMinimapFlag(byte mapX, byte mapY)
        {
            //_owner.Session.SendPacketAsync(new MinimapFlagPacketComposer(mapX, mapY));
        }

        /// <summary>
        /// Hide's minimap flag.
        /// </summary>
        public void SendHideMinimapFlag()
        {
            //_owner.Session.SendPacketAsync(new MinimapFlagPacketComposer(255, 255));
        }

        /// <summary>
        /// Set's client camera looking position.
        /// </summary>
        /// <param name="localX">X in the map where camera will be looking at.</param>
        /// <param name="localY">Y in the map where camera will be looking at.</param>
        /// <param name="z">Height of the camera.</param>
        /// <param name="speedX">Speed to the x.</param>
        /// <param name="speedY">Speed to the y.</param>
        public void SendSetCameraLookAtlocation(int localX, int localY, int z, int speedX, int speedY) =>
            _owner.Session.SendMessage(new SetCameraLookAtLocationMessage
            {
                LocalX = localX,
                LocalY = localY,
                Z = z,
                SpeedX = speedX,
                SpeedY = speedY,
            });

        /// <summary>
        /// Set's client camera location.
        /// </summary>
        /// <param name="localX">X in the map where camera will be looking from.</param>
        /// <param name="localY">Y in the map where camera will be looking from.</param>
        /// <param name="z">Z of the camera.</param>
        /// <param name="speedX">Speed to the x.</param>
        /// <param name="speedY">Speed to the y.</param>
        public void SendSetCameraLocation(int localX, int localY, int z, int speedX, int speedY) =>
            _owner.Session.SendMessage(new SetCameraLocationMessage
            {
                LocalX = localX,
                LocalY = localY,
                Z = z,
                SpeedX = speedX,
                SpeedY = speedY
            });

        /// <summary>
        /// Release's client camera.
        /// </summary>
        public void SendReleaseCamera()
        {
            //_owner.Session.SendPacketAsync(new ReleaseCameraPacketComposer());
        }

        /// <summary>
        /// Sends the shake camera.
        /// </summary>
        /// <param name="up">Up.</param>
        /// <param name="down">Down.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="index">The index.</param>
        public void SendSetCameraShake(int up, int down, int left, int right, int index) =>
            _owner.Session.SendMessage(new SetCameraShakeMessage
            {
                Index = index,
                UpDelta = up,
                DownDelta = down,
                LeftDelta = left,
                RightDelta = right
            });

        /// <summary>
        /// Resets the camera.
        /// </summary>
        public void SendResetCameraShake() =>
            _owner.Session.SendMessage(new SetCameraShakeMessage
            {
                Reset = true
            });

        /// <summary>
        /// Sends the integer input.
        /// </summary>
        /// <param name="message">The message.</param>
        public void SendIntegerInput(string message) =>
            SendCs2Script(108,
            [
                message
            ]);

        /// <summary>
        /// Sends the string input.
        /// </summary>
        /// <param name="message">The text.</param>
        public void SendStringInput(string message) =>
            SendCs2Script(109,
            [
                message
            ]);

        /// <summary>
        /// Sends the text input.
        /// </summary>
        /// <param name="message">The message.</param>
        public void SendTextInput(string message) =>
            SendCs2Script(110,
            [
                message
            ]);
    }
}
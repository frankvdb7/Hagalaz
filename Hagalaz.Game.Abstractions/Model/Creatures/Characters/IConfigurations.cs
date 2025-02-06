using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConfigurations
    {
        /// <summary>
        /// Send's standart configuration to character's client.
        /// </summary>
        /// <param name="configurationID">Id of the configuration to send.</param>
        /// <param name="value">Value of the configuration.</param>
        void SendStandardConfiguration(int configurationID, int value);

        /// <summary>
        /// Send's CS2Script call to character's client.
        /// </summary>
        /// <param name="scriptID">Id of the script to send.</param>
        /// <param name="callParameters">Parameters of the script to call , can only contain ints and strings.</param>
        void SendCs2Script(int scriptID, object[] callParameters);

        /// <summary>
        /// Send's global cs2int configuration to character's client.
        /// </summary>
        /// <param name="configurationID">Id of the configuration to send.</param>
        /// <param name="value">Value of the configuration.</param>
        void SendGlobalCs2Int(int configurationID, int value);

        /// <summary>
        /// Sends the client var bit.
        /// 'ConfigByFile'
        /// </summary>
        /// <param name="fileID">The file Id.</param>
        /// <param name="value">The value.</param>
        void SendBitConfiguration(int fileID, int value);

        /// <summary>
        /// Send's global cs2string configuration to character's client.
        /// </summary>
        /// <param name="configurationID">Id of the configuration to send.</param>
        /// <param name="value">Value of the configuration.</param>
        void SendGlobalCs2String(int configurationID, string value);

        /// <summary>
        /// Send's items to given client container.
        /// </summary>
        /// <param name="containerId">Id of the client container to send items to.</param>
        /// <param name="split">Wheter items should be splited.</param>
        /// <param name="container">Container which items should be sended.</param>
        /// <param name="slots">The slots.</param>
        void SendItems(int containerId, bool split, IContainer<IItem?> container, HashSet<int>? slots = null);

        /// <summary>
        /// Send's minimap configuration.
        /// </summary>
        /// <param name="type">Type of the minimap.</param>
        void SendMinimapConfiguration(MinimapType type);
        /// <summary>
        /// Sends the integer input.
        /// </summary>
        /// <param name="message">The message.</param>
        void SendIntegerInput(string message);
        /// <summary>
        /// Sends the string input.
        /// </summary>
        /// <param name="message">The text.</param>
        void SendStringInput(string message);
        /// <summary>
        /// Sends the text input.
        /// </summary>
        /// <param name="message">The message.</param>
        void SendTextInput(string message);

        /// <summary>
        /// Sends the shake camera.
        /// </summary>
        /// <param name="up">Up.</param>
        /// <param name="down">Down.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="index">The index.</param>
        void SendSetCameraShake(int up, int down, int left, int right, int index);

        /// <summary>
        /// Set's client camera looking position.
        /// </summary>
        /// <param name="localX">X in the map where camera will be looking at.</param>
        /// <param name="localY">Y in the map where camera will be looking at.</param>
        /// <param name="z">Z of the camera.</param>
        /// <param name="speedX">Speed to the x.</param>
        /// <param name="speedY">Speed to the y.</param>
        void SendSetCameraLookAtlocation(int localX, int localY, int z, int speedX, int speedY);

        /// <summary>
        /// Set's client camera location.
        /// </summary>
        /// <param name="localX">X in the map where camera will be looking from.</param>
        /// <param name="localY">Y in the map where camera will be looking from.</param>
        /// <param name="z">Z of the camera.</param>
        /// <param name="speedX">Speed to the x.</param>
        /// <param name="speedY">Speed to the my.</param>
        void SendSetCameraLocation(int localX, int localY, int z, int speedX, int speedY);
        /// <summary>
        /// Release's client camera.
        /// </summary>
        void SendReleaseCamera();
        /// <summary>
        /// Resets the camera.
        /// </summary>
        void SendResetCameraShake();
    }
}

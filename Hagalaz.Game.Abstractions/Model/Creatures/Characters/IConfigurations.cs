using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for sending various client-side configurations and updates to a character.
    /// </summary>
    public interface IConfigurations
    {
        /// <summary>
        /// Sends a standard integer-based configuration value to the client.
        /// </summary>
        /// <param name="configurationID">The ID of the configuration to send.</param>
        /// <param name="value">The value to set.</param>
        void SendStandardConfiguration(int configurationID, int value);
        /// <summary>
        /// Sends a request to execute a CS2 (ClientScript 2) script on the client.
        /// </summary>
        /// <param name="scriptID">The ID of the script to execute.</param>
        /// <param name="callParameters">An array of parameters to pass to the script (can only contain ints and strings).</param>
        void SendCs2Script(int scriptID, object[] callParameters);
        /// <summary>
        /// Sends a global integer configuration value that is shared across the client.
        /// </summary>
        /// <param name="configurationID">The ID of the global configuration.</param>
        /// <param name="value">The value to set.</param>
        void SendGlobalCs2Int(int configurationID, int value);
        /// <summary>
        /// Sends a bit-packed configuration value to the client.
        /// </summary>
        /// <param name="fileID">The ID of the configuration file.</param>
        /// <param name="value">The value to set.</param>
        void SendBitConfiguration(int fileID, int value);
        /// <summary>
        /// Sends a global string configuration value that is shared across the client.
        /// </summary>
        /// <param name="configurationID">The ID of the global configuration.</param>
        /// <param name="value">The value to set.</param>
        void SendGlobalCs2String(int configurationID, string value);
        /// <summary>
        /// Sends the contents of an item container to the client.
        /// </summary>
        /// <param name="containerId">The ID of the client-side container to update.</param>
        /// <param name="split">A value indicating whether the items should be sent in a split update.</param>
        /// <param name="container">The server-side container whose items should be sent.</param>
        /// <param name="slots">An optional set of specific slots to update; if null, the entire container is sent.</param>
        void SendItems(int containerId, bool split, IContainer<IItem?> container, HashSet<int>? slots = null);
        /// <summary>
        /// Sends a configuration update for the minimap.
        /// </summary>
        /// <param name="type">The type of minimap configuration to apply.</param>
        void SendMinimapConfiguration(MinimapType type);
        /// <summary>
        /// Prompts the user with a dialog box to enter an integer.
        /// </summary>
        /// <param name="message">The message to display in the dialog box.</param>
        void SendIntegerInput(string message);
        /// <summary>
        /// Prompts the user with a dialog box to enter a string.
        /// </summary>
        /// <param name="message">The message to display in the dialog box.</param>
        void SendStringInput(string message);
        /// <summary>
        /// Prompts the user with a dialog box to enter text.
        /// </summary>
        /// <param name="message">The message to display in the dialog box.</param>
        void SendTextInput(string message);
        /// <summary>
        /// Applies a shaking effect to the game camera.
        /// </summary>
        /// <param name="up">The upward shake intensity.</param>
        /// <param name="down">The downward shake intensity.</param>
        /// <param name="left">The leftward shake intensity.</param>
        /// <param name="right">The rightward shake intensity.</param>
        /// <param name="index">The index of the shake effect.</param>
        void SendSetCameraShake(int up, int down, int left, int right, int index);
        /// <summary>
        /// Moves the client camera to look at a specific position.
        /// </summary>
        /// <param name="localX">The local X-coordinate to look at.</param>
        /// <param name="localY">The local Y-coordinate to look at.</param>
        /// <param name="z">The height of the camera.</param>
        /// <param name="speedX">The horizontal movement speed.</param>
        /// <param name="speedY">The vertical movement speed.</param>
        void SendSetCameraLookAtlocation(int localX, int localY, int z, int speedX, int speedY);
        /// <summary>
        /// Moves the client camera to a new location.
        /// </summary>
        /// <param name="localX">The local X-coordinate to move to.</param>
        /// <param name="localY">The local Y-coordinate to move to.</param>
        /// <param name="z">The height of the camera.</param>
        /// <param name="speedX">The horizontal movement speed.</param>
        /// <param name="speedY">The vertical movement speed.</param>
        void SendSetCameraLocation(int localX, int localY, int z, int speedX, int speedY);
        /// <summary>
        /// Releases the client camera from any script-controlled position, returning control to the player.
        /// </summary>
        void SendReleaseCamera();
        /// <summary>
        /// Resets any active camera shake effect.
        /// </summary>
        void SendResetCameraShake();
    }
}

using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.CharacterName
{
    /// <summary>
    ///     Contains name interface script.
    /// </summary>
    public class NameInterface : WidgetScript
    {
        /// <summary>
        ///     Contains name set callback.
        /// </summary>
        private EventHappened? _nameSetCallback;

        /// <summary>
        ///     Contains last name entered.
        /// </summary>
        private string? _lastDisplayName;

        /// <summary>
        ///     Contains standart string.
        /// </summary>
        private const string _standardString = "Please enter a new character name in the box below.";

        public NameInterface(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            Owner.Configurations.SendCs2Script(3943, []); // enable the input
            InterfaceInstance.DrawString(19, _standardString);
            InterfaceInstance.AttachClickHandler(31, (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                AccountNameConfirm();
                return true;
            });
        }

        /// <summary>
        ///     Enable's acc name confirm.
        /// </summary>
        /// >
        public void AccountNameConfirm()
        {
            Owner.Widgets.StringInputHandler = NameInputHandler;
            Owner.Configurations.SendCs2Script(3945, [0, 1, 1]); // send the packet.
        }

        /// <summary>
        ///     Method for listening for name input.
        /// </summary>
        /// <param name="name"></param>
        public void NameInputHandler(string name)
        {
            Owner.Widgets.StringInputHandler = null;
            _lastDisplayName = name;
            if (name.Length <= 0)
            {
                OnBadNameEntered(name, null);
                return;
            }

            SendSetName(name);
        }

        /// <summary>
        ///     Send's set name packet.
        /// </summary>
        /// <param name="name"></param>
        private void SendSetName(string name)
        {
            _nameSetCallback = Owner.RegisterEventHandler<NameSetFinishedEvent>(NameSetFinished);
            //var adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
            //adapter.SendPacketAsync(new SetCharacterDetailsRequestPacketComposer(Owner.Session.Id, new CharacterDetailsDto() { DisplayName = name })).Wait();
        }

        /// <summary>
        ///     Get's called when name set is finished and we can get response.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool NameSetFinished(Event e)
        {
            if (_nameSetCallback == null)
            {
                return false;
            }

            if (_lastDisplayName == null)
            {
                return false;
            }

            if (!Owner.UnregisterEventHandler<NameSetFinishedEvent>(_nameSetCallback))
            {
                return false;
            }

            _nameSetCallback = null;
            var nfe = (NameSetFinishedEvent)e;
            if (nfe.Success)
            {
                End(_lastDisplayName);
            }
            else
            {
                OnBadNameEntered(_lastDisplayName, nfe.ReturnMessage);
            }

            return false;
        }

        /// <summary>
        ///     Executed when given name is not available.
        /// </summary>
        /// <param name="displayName">The name.</param>
        /// <param name="returnMessage">The return message.</param>
        private void OnBadNameEntered(string displayName, string? returnMessage)
        {
            if (returnMessage == null)
            {
                InterfaceInstance.DrawString(19, $"'{displayName}' is not available.");
            }
            else
            {
                InterfaceInstance.DrawString(19, returnMessage);
            }

            Owner.Configurations.SendCs2Script(3943, []); // enable input
        }

        /// <summary>
        ///     Ends the script.
        /// </summary>
        /// <param name="displayName">The name.</param>
        private void End(string displayName)
        {
            Owner.SendChatMessage($"You successfully changed your name to: {displayName}");
            Owner.Configurations.SendCs2Script(3943, []); // enable input
            Owner.Widgets.CloseWidget(InterfaceInstance);
        }
    }
}
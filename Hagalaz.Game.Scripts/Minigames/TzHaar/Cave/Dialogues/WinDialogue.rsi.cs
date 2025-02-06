﻿using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.Dialogues
{
    /// <summary>
    /// </summary>
    public class WinDialogue : DialogueScript
    {
        /// <summary>
        ///     The definition
        /// </summary>
        private INpcDefinition _definition;

        public WinDialogue(ICharacterContextAccessor contextAccessor, INpcService definitionRepository) : base(contextAccessor)
        {
            if (definitionRepository == null) throw new ArgumentNullException(nameof(definitionRepository));
            _definition = definitionRepository.FindNpcDefinitionById(TzHaarConstants.TzhaarMejJal);
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(_definition, DialogueAnimations.CalmTalk, "You even defeated TzTok-Jad. I am most impressed!", "Please accept this gift as a reward.");
                return true;
            });

            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }
    }
}
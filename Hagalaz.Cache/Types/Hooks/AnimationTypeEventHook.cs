﻿using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Hooks
{
    public class AnimationTypeEventHook : ITypeEventHook<IAnimationType>
    {
        public void AfterDecode(ITypeDecoder<IAnimationType> decoder, IAnimationType[] types)
        {
            foreach (var type in types)
            {
                type.AfterDecode();
            }
        }
    }
}

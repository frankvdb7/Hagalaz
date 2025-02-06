using AutoMapper;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol.Map;
using Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Profiles
{
    public class RegionProfile : Profile
    {
        public RegionProfile()
        {
            CreateMap<AddGameObjectUpdate, RaidoMessage>()
                .ConstructUsing(src => new AddGameObjectMessage
                {
                    GameObjectId = src.GameObject.Id,
                    Shape = src.GameObject.ShapeType,
                    Rotation = src.GameObject.Rotation,
                    PartLocalX = src.Location.X % 8,
                    PartLocalY = src.Location.Y % 8
                });
            CreateMap<AddGroundItemUpdate, RaidoMessage>()
                .ConstructUsing(src => new AddGroundItemMessage
                {
                    ItemId = src.Item.ItemOnGround.Id, Count = src.Item.ItemOnGround.Count, PartLocalX = src.Location.X % 8, PartLocalY = src.Location.Y % 8
                });
            CreateMap<DrawGraphicUpdate, RaidoMessage>()
                .ConstructUsing(src => new DrawGraphicMessage
                {
                    GraphicId = src.Graphic.Id,
                    Delay = src.Graphic.Delay,
                    Height = src.Graphic.Height,
                    Rotation = src.Graphic.Rotation,
                    PartLocalX = src.Location.X % 8,
                    PartLocalY = src.Location.Y % 8
                });
            CreateMap<DrawProjectileUpdate, RaidoMessage>()
                .ConstructUsing(src => new DrawProjectileMessage
                {
                    GraphicId = src.Projectile.GraphicId,
                    AdjustFromFlyingHeight = src.Projectile.AdjustToFlyingHeight,
                    AdjustToFlyingHeight = src.Projectile.AdjustFromFlyingHeight,
                    FromBodyPartId = src.Projectile.FromBodyPartId,
                    Angle = src.Projectile.Angle,
                    Delay = src.Projectile.Delay,
                    Duration = src.Projectile.Duration,
                    FromHeight = src.Projectile.FromHeight,
                    ToHeight = src.Projectile.ToHeight,
                    Slope = src.Projectile.Slope,
                    FromIsCharacter = src.Projectile.FromCreature is ICharacter,
                    FromIndex = src.Projectile.FromCreature != null ? src.Projectile.FromCreature.Index : null,
                    FromLocation = src.Projectile.FromLocation,
                    ToLocation = src.Projectile.ToLocation,
                    ToIsCharacter = src.Projectile.ToCreature is ICharacter,
                    ToIndex = src.Projectile.ToCreature != null ? src.Projectile.ToCreature.Index : null
                });
            CreateMap<DrawTileStringUpdate, RaidoMessage>()
                .ConstructUsing(src => new DrawTileStringMessage
                {
                    Value = src.Value,
                    Duration = src.Duration,
                    Height = src.Height,
                    Color = src.Color,
                    PartLocalX = src.Location.X % 8,
                    PartLocalY = src.Location.Y % 8
                });
            CreateMap<RemoveGameObjectUpdate, RaidoMessage>().ConstructUsing(src => new RemoveGameObjectMessage
            {
                PartLocalX = src.Location.X % 8,
                PartLocalY = src.Location.Y % 8,
                Rotation = src.GameObject.Rotation,
                Shape = src.GameObject.ShapeType
            });
            CreateMap<RemoveGroundItemUpdate, RaidoMessage>().ConstructUsing(src => new RemoveGroundItemMessage
            {
                ItemId = src.Item.ItemOnGround.Id,
                PartLocalX = src.Location.X % 8,
                PartLocalY = src.Location.Y % 8
            });
            CreateMap<SetGameObjectAnimationUpdate, RaidoMessage>().ConstructUsing(src => new SetGameObjectAnimationMessage
            {
                AnimationId = src.Animation.Id,
                Shape = src.GameObject.ShapeType,
                Rotation = src.GameObject.Rotation,
                PartLocalX = src.Location.X % 8,
                PartLocalY = src.Location.Y % 8
            });
            CreateMap<SetGroundItemCountUpdate, RaidoMessage>().ConstructUsing(src => new SetGroundItemCountMessage
            {
                ItemId = src.Item.ItemOnGround.Id,
                PartLocalX = src.Location.X % 8,
                PartLocalY = src.Location.Y % 8,
                NewCount = src.Item.ItemOnGround.Count,
                OldCount = src.OldCount
            });
            CreateMap<SetTileSoundUpdate, RaidoMessage>().ConstructUsing(src => new SetTileSoundMessage
            {
                SoundId = src.Sound.Id,
                Volume = src.Sound.Volume,
                Delay = src.Sound.Delay,
                PlaybackSpeed = src.Sound.PlaybackSpeed,
                Distance = src.Sound.Distance,
                RepeatCount = src.Sound.RepeatCount,
                PartLocalX = src.Location.X % 8,
                PartLocalY = src.Location.Y % 8
            });
        }
    }
}
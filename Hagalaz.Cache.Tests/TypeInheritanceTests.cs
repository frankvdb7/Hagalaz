using Hagalaz.Cache.Abstractions.Types;
using NSubstitute;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class TypeInheritanceTests
    {
        [Fact]
        public void IItemType_HasAccessToId()
        {
            var item = Substitute.For<IItemType>();
            item.Id.Returns(1);
            Assert.Equal(1, item.Id);
        }

        [Fact]
        public void INpcType_HasAccessToId()
        {
            var npc = Substitute.For<INpcType>();
            npc.Id.Returns(2);
            Assert.Equal(2, npc.Id);
        }

        [Fact]
        public void IObjectType_HasAccessToId()
        {
            var obj = Substitute.For<IObjectType>();
            obj.Id.Returns(3);
            Assert.Equal(3, obj.Id);
        }

        [Fact]
        public void IAnimationType_HasAccessToId()
        {
            var anim = Substitute.For<IAnimationType>();
            anim.Id.Returns(4);
            Assert.Equal(4, anim.Id);
        }
    }
}

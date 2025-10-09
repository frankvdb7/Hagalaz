// Copyright (c) Geta Digital. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using NSubstitute;

namespace Hagalaz.Game.Abstractions.Tests;

[TestClass]
public class GameCommandArgsTests
{
    [TestMethod]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var character = Substitute.For<ICharacter>();
        var arguments = new[] { "arg1", "arg2" };

        // Act
        var commandArgs = new GameCommandArgs(character, arguments);

        // Assert
        Assert.AreSame(character, commandArgs.Character);
        Assert.AreSame(arguments, commandArgs.Arguments);
        Assert.IsFalse(commandArgs.Handled);
    }

    [TestMethod]
    public void Constructor_WithEmptyArguments_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var character = Substitute.For<ICharacter>();
        var arguments = Array.Empty<string>();

        // Act
        var commandArgs = new GameCommandArgs(character, arguments);

        // Assert
        Assert.AreSame(character, commandArgs.Character);
        Assert.AreSame(arguments, commandArgs.Arguments);
    }

    [TestMethod]
    public void Handled_Property_CanBeSet()
    {
        // Arrange
        var character = Substitute.For<ICharacter>();
        var arguments = new[] { "arg1" };
        var commandArgs = new GameCommandArgs(character, arguments);

        // Act
        commandArgs.Handled = true;

        // Assert
        Assert.IsTrue(commandArgs.Handled);
    }
}
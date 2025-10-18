# Hagalaz.Game.Abstractions

This project is a foundational component of the Hagalaz game architecture. It defines the essential contracts, data models, and interfaces that govern the entire game world. By providing a rich set of abstractions, it ensures that different parts of the game (e.g., game logic, services, clients) can communicate and interact in a consistent and decoupled manner.

## Overview

The primary purpose of `Hagalaz.Game.Abstractions` is to serve as the single source of truth for the game's core domain model. It is intentionally free of concrete implementations, focusing solely on defining the "what" rather than the "how." All other game-related projects within the Hagalaz solution should reference this project to build upon a common understanding of game entities and their behaviors.

## Key Features & Directories

This project is organized into several key directories, each representing a distinct area of the game's design:

-   **`/Model`**: Contains the core data structures and enums that represent all entities in the game world. This includes everything from `Creature`, `IItem`, and `GameObject` to fundamental types like `Location` and `Direction`.
-   **`/Builders`**: Defines interfaces for the fluent builder pattern used to construct complex game objects like `Animation`, `Graphic`, and `Widget`. This ensures a consistent and readable way to create these objects across the system.
-   **`/Services`**: Lays out the contracts for high-level services that manage major game features, such as `ICharacterService` and `IGameObjectService`.
-   **`/Providers`**: Contains interfaces for providers, which are responsible for supplying scripts and other data associated with game entities (e.g., `IItemScriptProvider`, `INpcScriptProvider`).
-   **`/Scripts`**: Defines contracts for the scripting system, which allows for dynamic and extensible game logic.
-   **`/Logic`**: Holds interfaces related to core game logic systems, such as loot generation (`ILootGenerator`), pathfinding (`IPathFinderProvider`), and skill-based actions.
-   **`/Features`**: Defines contracts for specific, self-contained game features like Clans (`IClan`), Shops (`IShop`), and States (`IState`).
-   **`/Collections`**: Provides interfaces for specialized collections used in the game, most notably the `IItemContainer`.
-   **`/Mediator`**: Contains interfaces for the in-process messaging system (`IGameMediator`), which facilitates communication between different components within a single service.
-   **`/Store`**: Defines contracts for data stores (`ICharacterStore`, `INpcStore`) that manage the lifecycle of active game entities.
-   **`/Tasks`**: Includes interfaces and classes for the game's task scheduling system, which handles delayed and recurring actions within the main game loop.

## Core Concepts

A developer working with this project should be familiar with the following architectural patterns:

### 1. Abstraction-First Design

The entire project is built around interfaces (contracts). This decouples the definition of a game entity or system from its concrete implementation. For example, game logic services will depend on `ICharacter` rather than a specific `Player` or `Monster` class. This makes the system more modular, testable, and easier to refactor.

### 2. Scripting Engine

Much of the game's dynamic behavior is handled by a scripting system. Instead of hard-coding the logic for every item, NPC, or game object, the system relies on attaching scripts to these entities. The providers (e.g., `IItemScriptProvider`) are responsible for retrieving the correct script for a given entity, which is then executed by the game engine.

### 3. Hydration and Dehydration

To manage the state of game objects, particularly for persistence (saving to a database) or network transfer, the concepts of "hydration" and "dehydration" are used.
-   **Dehydration (`IDehydratable`)**: The process of converting a live, in-memory game object into a simple, serializable Data Transfer Object (DTO).
-   **Hydration (`IHydratable`)**: The process of taking a DTO and restoring it into a fully-functional, live game object.

### 4. Mediator Pattern for In-Process Communication

The `IGameMediator` interface provides a powerful mechanism for different components within a service to communicate without having direct references to each other. It supports:
-   **Request/Response**: Asynchronously send a request and get a response.
-   **Publish/Subscribe**: Publish an event to multiple subscribers.
-   **Send**: Send a command to a single, known handler.

## Setup and Usage

This project is a class library (`.csproj`) and is not meant to be run on its own. To use it, simply add a project reference to `Hagalaz.Game.Abstractions` from any other project in the solution that needs to interact with the core game domain.

**Example (in a service project's `.csproj` file):**

```xml
<ItemGroup>
  <ProjectReference Include="..\Hagalaz.Game.Abstractions\Hagalaz.Game.Abstractions.csproj" />
</ItemGroup>
```

Once referenced, you can use the defined interfaces in your services, logic, and data layers through dependency injection.

```csharp
// Example of a service using an abstraction from this project
public class MyGameService
{
    private readonly ICharacterService _characterService;

    public MyGameService(ICharacterService characterService)
    {
        _characterService = characterService;
    }

    public void MovePlayer(ICharacter character, Location newLocation)
    {
        // Use the service to perform an action
        _characterService.MoveCharacter(character, newLocation);
    }
}
```
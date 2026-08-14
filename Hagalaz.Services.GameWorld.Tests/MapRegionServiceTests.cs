using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class MapRegionServiceTests
    {
        [TestMethod]
        public async Task EnsureRegionLoadScheduled_SkipsLoadedAndDuplicateRegions()
        {
            var queue = new RecordingBackgroundTaskQueue();
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            using var provider = new ServiceCollection()
                .AddSingleton(loader)
                .BuildServiceProvider();
            var service = CreateService(queue, provider);
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);

            service.EnsureRegionLoadScheduled(region);
            service.EnsureRegionLoadScheduled(region);

            Assert.HasCount(1, queue.WorkItems);
            await queue.WorkItems[0](CancellationToken.None);

            service.EnsureRegionLoadScheduled(region);
            Assert.HasCount(2, queue.WorkItems);

            region.IsLoaded.Returns(true);
            service.EnsureRegionLoadScheduled(region);
            Assert.HasCount(2, queue.WorkItems);
        }

        [TestMethod]
        public async Task EnsureRegionLoadScheduled_DoesNotDuplicateWhileQueueAdmissionWaits()
        {
            var queue = new BlockingBackgroundTaskQueue();
            using var provider = new ServiceCollection().BuildServiceProvider();
            var service = CreateService(queue, provider);
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);

            var firstRequest = Task.Run(() => service.EnsureRegionLoadScheduled(region));
            await queue.AdmissionStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            service.EnsureRegionLoadScheduled(region);

            Assert.HasCount(1, queue.WorkItems);
            queue.Admission.TrySetResult();
            await firstRequest;
        }

        private static MapRegionService CreateService(IBackgroundTaskQueue queue, IServiceProvider serviceProvider) =>
            new(
                queue,
                serviceProvider,
                Substitute.For<ILocationBuilder>(),
                Substitute.For<IGameObjectBuilder>(),
                Substitute.For<IGroundItemBuilder>(),
                Substitute.For<ILogger<MapRegionService>>(),
                Substitute.For<IMapper>());

        private sealed class RecordingBackgroundTaskQueue : IBackgroundTaskQueue
        {
            public List<Func<CancellationToken, ValueTask>> WorkItems { get; } = [];

            public ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
            {
                WorkItems.Add(workItem);
                return ValueTask.CompletedTask;
            }

            public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken) =>
                throw new NotSupportedException();
        }

        private sealed class BlockingBackgroundTaskQueue : IBackgroundTaskQueue
        {
            public List<Func<CancellationToken, ValueTask>> WorkItems { get; } = [];
            public TaskCompletionSource AdmissionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource Admission { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
            {
                WorkItems.Add(workItem);
                AdmissionStarted.TrySetResult();
                return new ValueTask(Admission.Task);
            }

            public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken) =>
                throw new NotSupportedException();
        }
    }
}

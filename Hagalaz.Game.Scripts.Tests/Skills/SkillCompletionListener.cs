using System.Diagnostics.Metrics;

namespace Hagalaz.Game.Scripts.Tests.Skills;

internal static class SkillCompletionListener
{
    public static MeterListener Start(MeasurementCallback<long> record)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, activeListener) =>
        {
            if (instrument.Meter.Name == "Hagalaz.Services.GameWorld"
                && instrument.Name == "hagalaz.gameworld.skill.action.completed")
            {
                activeListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback(record);
        listener.Start();
        return listener;
    }
}

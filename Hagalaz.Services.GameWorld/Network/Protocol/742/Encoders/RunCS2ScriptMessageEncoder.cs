using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class RunCS2ScriptMessageEncoder : IRaidoMessageEncoder<RunCS2ScriptMessage>
    {
        public void EncodeMessage(RunCS2ScriptMessage message, IRaidoMessageBinaryWriter output)
        {
            output.SetOpcode(29).SetSize(RaidoMessageSize.VariableShort);
            var parameters = message.Parameters;
            foreach (var param in parameters)
            {
                if (param is string)
                    output.WriteByte((byte)'s');
                else
                    output.WriteByte((byte)'a');
            }
            output.WriteByte(0); // end string

            for (int i = parameters.Length - 1; i >= 0; i--)
            {
                if (parameters[i] is string @string)
                {
                    output.WriteString(@string);
                }
                else
                {
                    output.WriteInt32BigEndian((int)parameters[i]);
                }
            }
            output.WriteInt32BigEndian(message.Id);
        }
    }
}

using System;
using System.Net;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Encoders
{
    public class LobbySignInResponseEncoder : IRaidoMessageEncoder<LobbySignInResponse>
    {
        public void EncodeMessage(LobbySignInResponse message, IRaidoMessageBinaryWriter output)
        {
            output
                .SetOpcode(ClientSignInResponse.Success.GetOpcode())
                .SetSize(RaidoMessageSize.VariableByte);

            output.WriteByte((byte)message.ClientPermissions);
            output.WriteByte(0); // does something between 5 >= and <= 9 (client logging level?)

            output.WriteByte(0); // has something to do with chat muted???

            output.WriteInt24BigEndian(0); // 343932928, has something to do with female / male???
            output.WriteByte(1); // whether the player is a female == 0 / male == 1

            output.WriteByte(0); // something to do with chat muted???

            output.WriteByte(0); // executes some kind of 'zap' on the client frame

            // current time from unix 1970-1-1
            output.WriteInt64BigEndian(DateTimeOffset.Now.ToUnixTimeMilliseconds());
            // membership time left from now until future date (7 days by default)
            output.WriteInt40BigEndian(TimeSpan.FromDays(7).Milliseconds);

            byte flag = 0; // membership flag
            flag |= 0x1; // isMember
            flag |= 0x2; // isSubscription ( non time limited )
            output.WriteByte(flag);

            output.WriteInt32BigEndian(0); // something with a cache file and client scripts

            output.WriteByte(0); // some bool that has something to do with next int
            output.WriteInt32BigEndian(0); // recovery questions set date
            output.WriteInt16BigEndian(1); // recovery questions, 0 - not set, otherwise goes bitencoded date.

            output.WriteInt16BigEndian((short)message.UnreadMessagesCount); // unread messages

            if (!message.LastLogin.HasValue)
            {
                output.WriteInt16BigEndian(0); // never logged in before
            }
            else
            {
                var now = DateTimeOffset.Now;
                var jagDate = new DateTimeOffset(new DateTime(2002, 2, 27));
                output.WriteInt16BigEndian((short)((now - jagDate).TotalDays - (now - message.LastLogin.Value).TotalDays)); // last login date
            }

            output.WriteInt32BigEndian(!TryParseIpv4(message.LastIpAddress, out var lastIpInt) ? 0 : lastIpInt);

            // email configuration
            // 0 - no email
            // 1 - pending parental confirmation
            // 2 - pending confirmation.
            // 3 - registered.
            // 4 - no longer registered.
            output.WriteByte(3);

            output.WriteInt16BigEndian(0); // credit card expiration time
            output.WriteInt16BigEndian(0); // credit card loyalty expiration time

            output.WriteByte(0); // unknown boolean

            output.WriteString(message.DisplayName, true);

            output.WriteByte(0); // bool used in a client script

            output.WriteInt32BigEndian(0); // 1 is used for a direct login, may be used for other values

            output.WriteByte(0); // bool used in client script

            output.WriteInt16BigEndian((short)message.WorldId);

            output.WriteString(message.WorldAddress, true);
        }

        private static bool TryParseIpv4(string? ip, out int ipInt)
        {
            if (!IPAddress.TryParse(ip, out var address))
            {
                ipInt = 0;
                return false;
            }
            var ipv4 = address.MapToIPv4().ToString();
            var ipv4Split = ipv4.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (!sbyte.TryParse(ipv4Split[0], out var first))
            {
                ipInt = 0;
                return false;
            }
            if (!sbyte.TryParse(ipv4Split[1], out var second))
            {
                ipInt = 0;
                return false;
            }
            if (!sbyte.TryParse(ipv4Split[2], out var third))
            {
                ipInt = 0;
                return false;
            }
            if (!sbyte.TryParse(ipv4Split[3], out var fourth))
            {
                ipInt = 0;
                return false;
            }
            ipInt = (first << 24) + (second << 16) + (third << 8) + fourth;
            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClasses
{
    public enum MessageType : byte
    {
        PublicMessage, PrivateMessage, ConnectionConfirmed, NewUser, UserLeave, ServerOverloaded, NameConflict, ServerClosed,
        ConnectionClosed, ClientDisconnected, ClientBanned, ClientActivated,  BanUser, ActivateUser, BannedByUser, ActivatedByUser
    };

    /*  Класс, реализующий исключения, которые
     *  могут быть сгенерированны при работе программы  */
    public class ConnectionException : Exception
    {
        public ConnectionException() : base() { }
        public ConnectionException(string str) : base(str) { }
        public ConnectionException(
            string str, Exception inner)
            : base(str, inner) { }
        public ConnectionException(
            System.Runtime.Serialization.SerializationInfo si,
            System.Runtime.Serialization.StreamingContext sc)
            : base(si, sc) { }
    }
}

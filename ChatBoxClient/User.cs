using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using CommonClasses;

namespace ChatBoxClient
{
    /*  Класс, реализующий взаимодействие с сервером  */
    class CUser
    {
        public BinaryWriter writer { get; set; }
        public bool isUserConnectedToServer { get; set; }
        public string userName { get; set; }
        public sbyte ID { get; set; }
        public bool isUserBanned { get; set; }

        const string MESSG_WELCOME = "Добро пожаловать, {0}!";
        const string MESSG_CLIENT_CONNECTED = "Пользователь <{0}> подключился к серверу.";
        const string MESSG_CLIENT_LEFT = "Пользователь <{0}> отключён от сервера.";
        const string MESSG_USER_LEAVE_SERVER = "Вы покинули сервер.";
        const string MESSG_SERVER_OVERLOADED = "Не удалось подключиться к серверу. Сервер перегружен.";
        const string MESSG_NAME_CONFLICT = "Не удалось подключиться к серверу. Пользователь с таким именем уже существует.";
        const string MESSG_CONNECTION_CLOSED = "Вы отключены от сервера!";
        const string MESSG_BANNED_BY_USER = "Пользователь <{0}> добавил Вас в чёрный список!";
        const string MESSG_ACTIVATED_BY_USER = "Пользователь <{0}> удалил Вас из чёрного списка!";
        const string TEMPLATE_PERSONAL_MESSAGE = "<Вы>: {0}";
        const string TEMPLATE_USER_MESSAGE = "<{0}>: {1}";
        const string MESSG_IMPOSSIBLE_TO_SEND_MESSAGE = "Невозможно отправить сообщение по причине блокировки.";
        const string MESSG_USER_BANNED = "Функция отправки сообщений была заблокирована.";
        const string MESSG_USER_ATIVATED = "Функция отправки сообщений разблокирована.";
        const string MESSG_SERVER_CLOSED = "Не удалось подключиться к серверу. Сервер закрыт для входящих подключений.";

        CColorsLoader loadedColors;
        CUsersManager usersManager;
        Socket listenSocket;
        IPEndPoint serverPoint;
        CView UI;
        NetworkStream NetStream;
        BinaryReader reader;
        Thread thrdListenServer;

        /*  Конструктор класса  */
        public CUser(string name, string ip, int port, CView currUI, CUsersManager manager)
        {
            loadedColors = CColorsLoader.GetInstance();
            serverPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            UI = currUI;
            usersManager = manager;
            userName = name;
            isUserConnectedToServer = false;
            isUserBanned = false;
        }

        /*  Подпрограмма установления соединения с сервером  */
        public void ConnectToServer()
        {
            writer = null;
            reader = null;
            NetStream = null;
            try
            {
                /*  Пытеамся подключиться к серверу  */
                listenSocket.Connect(serverPoint);
                NetStream = new NetworkStream(listenSocket);

                writer = new BinaryWriter(NetStream);
                reader = new BinaryReader(NetStream);

                writer.Write(userName);
                if (IsConnectionConfirmed())
                {
                   /*  Создаём новый поток для прослушивания 
                    *  входящих сообщений от сервера  */
                    thrdListenServer = new Thread(ReceiveMessages);
                    thrdListenServer.IsBackground = true;
                    thrdListenServer.Start();
                }
                else
                {
                    CloseConnection();
                }
                UI.UpdateConnectionInfo();
            }
            catch (Exception ex)
            {
                usersManager.ShowMessageInMainWritingPad(ex.Message, loadedColors.ClrErrorInd);
            }
        }

        private bool IsConnectionConfirmed()
        {
            while (!NetStream.DataAvailable) ;
            MessageType serverAnswer = (MessageType)reader.ReadByte();

            if (serverAnswer == MessageType.ConnectionConfirmed)
            {
                AnalyseMessageType(serverAnswer);
                return true;
            }

            isUserConnectedToServer = false;
            try
            {
                AnalyseMessageType(serverAnswer);  
            }
            catch (ConnectionException ex)
            {
                usersManager.ShowMessageInMainWritingPad(ex.Message, loadedColors.ClrErrorInd);
            }
            return false;
        }

        /*  Подпрограмма отключения от сервера  */
        public void DisconnectFromServer()
        {
            if (isUserConnectedToServer)
            {
                isUserConnectedToServer = false;
                bool isDisconnectedByServer = false;
                try
                {
                    //Отправка серверу сигнала о том, что клиент покинул сервер
                    writer.Write((byte)MessageType.ClientDisconnected);
                }
                catch (Exception)
                {
                    isDisconnectedByServer = true;
                }

                CloseConnection();

                if (isDisconnectedByServer)
                {
                    usersManager.ShowMessageInMainWritingPad(MESSG_CONNECTION_CLOSED, loadedColors.ClrErrorInd);
                }
                else
                {
                    usersManager.ShowMessageInMainWritingPad(MESSG_USER_LEAVE_SERVER, ID);
                }
            }
        }

        /*  Подпрограмма закрытия сетевых соединений  */
        private void CloseConnection()
        {
            if (reader != null)
                reader.Close();
            if (writer != null)
                writer.Close();
            if (NetStream != null)
                NetStream.Close();
            if ((listenSocket != null) && (listenSocket.Connected))
            {
                try
                {
                    listenSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception) { }
                listenSocket.Close();
            }
            usersManager.RemoveAllUsers();
        }

        /*  Подпрограмма генерирования исключения,
         *  соответствующего номеру ошибки  */
        private void AnalyseMessageType(MessageType curType)
        {
            string errorMessage = "";

            switch (curType)
            {
                case MessageType.PublicMessage:
                    ReceivePublicMessage();
                    break;
                case MessageType.PrivateMessage:
                    ReceivePrivateMessage();
                    break;
                case MessageType.BannedByUser:
                    BannedByAnotherUser();
                    break;
                case MessageType.ActivatedByUser:
                    ActivatedByAnotherUser();
                    break;
                case MessageType.ConnectionConfirmed:
                    GetInitialDataFromServer();
                    isUserConnectedToServer = true;
                    break;
                case MessageType.NewUser:
                    NewUserConnectedToServer();
                    break;
                case MessageType.UserLeave:
                    SomeUserLeftServer();
                    break;
                /*  Сервер перегружен  */
                case MessageType.ServerOverloaded:
                    errorMessage = MESSG_SERVER_OVERLOADED;
                    isUserConnectedToServer = false;
                    throw new ConnectionException(errorMessage);
                    break;
                /*  Клиент с таким именем есть на сервере  */
                case MessageType.NameConflict:
                    errorMessage = MESSG_NAME_CONFLICT;
                    isUserConnectedToServer = false;
                    throw new ConnectionException(errorMessage);
                    break;
                /*  Соединение прервано  */
                case MessageType.ConnectionClosed:
                    errorMessage = MESSG_CONNECTION_CLOSED;
                    isUserConnectedToServer = false;
                    throw new ConnectionException(errorMessage);
                    break;
                /*  Сервер закрыт для подключений  */
                case MessageType.ServerClosed:
                    errorMessage = MESSG_SERVER_CLOSED;
                    isUserConnectedToServer = false;
                    throw new ConnectionException(errorMessage);
                    break;
                /*  Блокировка клиента  */
                case MessageType.ClientBanned:
                    isUserBanned = true;
                    usersManager.ShowMessageInMainWritingPad(String.Format(MESSG_USER_BANNED), loadedColors.ClrErrorInd);
                    UI.UpdateConnectionInfo();
                    break;
                /*  Снятие блокировки с клиента  */
                case MessageType.ClientActivated:
                    isUserBanned = false;
                    usersManager.ShowMessageInMainWritingPad(String.Format(MESSG_USER_ATIVATED), loadedColors.ClrDefaultInd);
                    UI.UpdateConnectionInfo();
                    break;
            }
        }

        private void GetInitialDataFromServer()
        {
            ID = reader.ReadSByte();

            int colorsAmo = reader.ReadInt32();
            Color[] curPalette = new Color[colorsAmo];
            byte r, g, b;
            for (int i = 0; i < colorsAmo; i++)
            {
                r = reader.ReadByte();
                g = reader.ReadByte();
                b = reader.ReadByte();
                curPalette[i] = Color.FromRgb(r, g, b);
            }
            loadedColors.SetPalette(curPalette);

            string message = String.Format(MESSG_WELCOME, userName);
            usersManager.ShowMessageInMainWritingPad(message, ID);
            UI.AddFirstElemToList();

            byte usersAmo = reader.ReadByte();
            string newUserName;
            sbyte newUserID;
            for (int i = 0; i < usersAmo; i++)
            {
                newUserID = reader.ReadSByte();
                newUserName = reader.ReadString();
                usersManager.AddUser(newUserName, newUserID);
            }

            usersManager.UpdateDialogSelection();
        }

        /*  Подпрограмма получения от сервера сообщений  */
        private void ReceiveMessages()
        {
            MessageType messgType;

            try
            {
                /*  В бесконечном цикле принимаем 
                 *  входящие сообщения  */
                while (true)
                {
                    /*  Проверяем, есть ли непрочитанные данные  */
                    if (NetStream.DataAvailable)
                    {
                        messgType = (MessageType)reader.ReadByte();
                        AnalyseMessageType(messgType);
                    }
                }
            }
            catch (ConnectionException connExc)
            {
                /*  Вывод сообщения об ошибке  */
                usersManager.ShowMessageInMainWritingPad(connExc.Message, loadedColors.ClrErrorInd);
            }
            catch (Exception ex)
            {
                //usersManager.ShowMessageInMainWritingPad(ex.Message, loadedColors.ClrDefaultInd);//!!!!!!!!!!!!
            }
            finally
            {
                /*  В конце закрываем сетевые соединения  */
                CloseConnection();
                UI.UpdateConnectionInfo();
            }
        }

        /*  Подпрограмма отправки сообщения на сервер  */
        public void SendPublicMessage(string message)
        {
            try
            {
                if (!isUserBanned)
                {
                    usersManager.ShowMessageInMainWritingPad(String.Format(TEMPLATE_PERSONAL_MESSAGE, message), ID);
                    try
                    {
                        writer.Write((byte)MessageType.PublicMessage);
                        writer.Write(message);
                    }
                    catch (Exception)
                    {
                        DisconnectFromServer();
                    }
                }
                else
                {
                    usersManager.ShowMessageInMainWritingPad(String.Format(MESSG_IMPOSSIBLE_TO_SEND_MESSAGE), loadedColors.ClrErrorInd);
                }
            }
            catch (Exception ex)
            {
                usersManager.ShowMessageInMainWritingPad(ex.Message, 0);
            }
        }

        public void SendPrivateMessage(string message)
        {
            try
            {
                sbyte receiverID = usersManager.GetIDOfCurrentUser();
                if (!usersManager.IsBannedByUser(receiverID))
                {
                    usersManager.ShowMessageInCurrentWritingPad(String.Format(TEMPLATE_PERSONAL_MESSAGE, message), ID);
                    try
                    {
                        writer.Write((byte)MessageType.PrivateMessage);
                        writer.Write(receiverID);
                        writer.Write(message);
                    }
                    catch (Exception)
                    {
                        DisconnectFromServer();
                    }
                }
                else
                {
                    usersManager.ShowMessageInCurrentWritingPad(String.Format(MESSG_IMPOSSIBLE_TO_SEND_MESSAGE), loadedColors.ClrErrorInd);
                }
            }
            catch (Exception ex)
            {
                usersManager.ShowMessageInCurrentWritingPad(ex.Message, 0);
            }
        }

        public void BanUser(sbyte ID)
        {
            try
            {
                writer.Write((byte)MessageType.BanUser);
                writer.Write(ID);
            }
            catch (Exception)
            {
                DisconnectFromServer();
            }
        }

        public void ActivateUser(sbyte ID)
        {
            try
            {
                writer.Write((byte)MessageType.ActivateUser);
                writer.Write(ID);
            }
            catch (Exception)
            {
                DisconnectFromServer();
            }
        }

        public bool IsUserBannedForCurrentDialog()
        {
            if (usersManager.IsPublicMessage())
            {
                return isUserBanned;
            }
            else
            {
                return usersManager.IsUserBannedForCurrentDialog();
            }
        }
        
        private void ReadMessageFromUser(out sbyte id, out string message)
        {
            id = reader.ReadSByte();
            message = reader.ReadString();
        }

        private void ReceivePrivateMessage()
        {
            sbyte senderID;
            string message;
            ReadMessageFromUser(out senderID, out message);
            usersManager.ShowMessageInSpecifiedWritingPad(String.Format(TEMPLATE_USER_MESSAGE, usersManager.GetUsernameByID(senderID), message), senderID);
        }

        private void ReceivePublicMessage()
        {
            sbyte senderID;
            string message;
            ReadMessageFromUser(out senderID, out message);
            usersManager.ShowMessageInMainWritingPad(String.Format(TEMPLATE_USER_MESSAGE, usersManager.GetUsernameByID(senderID), message), senderID);
        }

        private void BannedByAnotherUser()
        {
            sbyte senderID = reader.ReadSByte();
            string senderName = usersManager.GetUsernameByID(senderID);
            usersManager.AddSelfToBlackList(senderID);
            usersManager.ShowMessageInSpecifiedWritingPad(String.Format(MESSG_BANNED_BY_USER, senderName), senderID);
            UI.UpdateConnectionInfo();
        }

        private void ActivatedByAnotherUser()
        {
            sbyte senderID = reader.ReadSByte();
            string senderName = usersManager.GetUsernameByID(senderID);
            usersManager.RemoveSelfToBlackList(senderID);
            usersManager.ShowMessageInSpecifiedWritingPad(String.Format(MESSG_ACTIVATED_BY_USER, senderName), senderID);
            UI.UpdateConnectionInfo();
        }

        private void NewUserConnectedToServer()
        {
            string newUserName = reader.ReadString();
            sbyte newUserID = reader.ReadSByte();

            usersManager.AddUser(newUserName, newUserID);
            string message = String.Format(MESSG_CLIENT_CONNECTED, newUserName);
            usersManager.ShowMessageInMainWritingPad(message, newUserID);
        }

        private void SomeUserLeftServer()
        {
            sbyte userID = reader.ReadSByte();
            string message = String.Format(MESSG_CLIENT_LEFT, usersManager.GetUsernameByID(userID));

            usersManager.RemoveUser(userID);
            usersManager.ShowMessageInMainWritingPad(message, userID);
        }
    }
}

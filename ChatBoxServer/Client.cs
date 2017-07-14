using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using CommonClasses;

namespace ChatBoxServer
{
    /* Класс, реализующий работу сервера с подключённым клиентом  */
    class CClient
    {
        public sbyte ID { get; set; }
        public string ClientName { get; set; }
        public bool IsClientBanned { get; set; }
        public BinaryWriter Writer { get; set; }
        public BinaryReader Reader { get; set; }

        const bool CLIENT_MESSAGE = false;
        const bool SERVER_MESSAGE = true;

        Socket clientSocket;
        CServer server;
        NetworkStream NetStream;
        CView UI;
        Thread thrListenClient;

        /*  Конструктор класса  */
        public CClient(Socket socket, CServer serv, sbyte id, CView currUI)
        {
            clientSocket = socket;
            server = serv;
            UI = currUI;
            ID = id;
            IsClientBanned = false;
        }

        public void Start()
        {
            thrListenClient = new Thread(WorkWithClient);
            thrListenClient.IsBackground = true;
            thrListenClient.Start();
        }

        public void GetClientName()
        {
            NetStream = new NetworkStream(clientSocket);
            Writer = new BinaryWriter(NetStream);
            Reader = new BinaryReader(NetStream);

            ClientName = Reader.ReadString();
        }

        public void ConfirmConnection()
        {
            Writer.Write((byte)MessageType.ConnectionConfirmed);
            Writer.Write(ID);
            server.SendPalette(this);
            server.SendClientsList(this);

            server.NotifyAllClientsAboutNewUser(ClientName, ID);
        }

        public void RejectConnection(MessageType errorType)
        {
            Writer.Write((byte)errorType);
            CloseConnection();
        }

        /*  Подпрограмма закрытия сетевых соединений  */
        private void CloseConnection()
        {
            if (Writer != null)
                Writer.Close();
            if (Reader != null)
                Reader.Close();
            if (NetStream != null)
                NetStream.Close();
            if ((clientSocket != null) && (clientSocket.Connected))
            {
                try
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception) { }
                clientSocket.Close();
            }
        }

        /*  Подпрограмма генерирования исключения в 
         *  в соответствии с типом ошибки  */
        private void AnalyseMessageType(MessageType curType)
        {
            switch (curType)
            {
                case MessageType.PublicMessage:
                    SendPublicMessage();
                    break;
                case MessageType.PrivateMessage:
                    SendPrivateMessage();
                    break;
                case MessageType.BanUser:
                    BanUser();
                    break;
                case MessageType.ActivateUser:
                    ActivateUser();
                    break;
                case MessageType.ClientDisconnected:
                    throw new ConnectionException();
                    break;
            } 
        }

        /*  Подпрограмма обработки сгенерированного исключения  */
        private void WorkWithExc(ConnectionException ex)
        {
            UI.RemoveClientFromList(ID);
            server.NotifyAllClientsAboutUserLeft(ClientName, ID);
            server.RemoveConnection(ID);
        }

        /*  Подпрограмма работы с подключённым клиентом  */
        private void WorkWithClient()
        {
            MessageType messgType;
            try
            {
            /*  В бесконечном цикле пытаемся получить сообщение от клиента.
             *  Выход из цикла осуществляется при возникновении исключения  */
            while (true)
                {
                    /*  Проверяем, есть ли непрочитанные данные  */
                    if (NetStream.DataAvailable)
                    {
                       /*  Считываем код сообщения, который определяет, 
                        *  это текстовое сообщение либо ошибка  */
                        messgType = (MessageType)Reader.ReadByte();
                        AnalyseMessageType(messgType);
                    }
                }
            }
            catch (ConnectionException clExc)
            {
                /*  Обработка возникшего исключения  */
                WorkWithExc(clExc);    
            }
            catch (Exception)
            {

            }
            finally
            {
                CloseConnection();
                UI.UpdateServerInfo();
            }
        }

        /*  Подпрограмма отключения клиента от сервера  */
        public void Stop()
        {
            try
            {
                //Отправка пользователю сигнала об отключении
                Writer.Write((byte)MessageType.ConnectionClosed);
            }
            catch (Exception) { }
            CloseConnection();
        }

        private void SendPrivateMessage()
        {
            sbyte receiverID = Reader.ReadSByte();
            string message = Reader.ReadString();

            server.SendPrivateMessage(message, ID, receiverID);
        }

        private void SendPublicMessage()
        {
            string message = Reader.ReadString();

            server.SendPublicMessage(message, ID);
        }

        private void BanUser()
        {
            sbyte userID = Reader.ReadSByte();
            server.BanUserByAnotherUser(ID, userID);
        }

        private void ActivateUser()
        {
            sbyte userID = Reader.ReadSByte();
            server.ActivateUserByAnotherUser(ID, userID);
        }
    }
}

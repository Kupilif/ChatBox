using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Media;
using CommonClasses;

namespace ChatBoxServer
{
    /*  Класс, реализующий работу сервера  */
    class CServer
    {
        public int AmoOfConnectedClients { get; private set; }
        public bool IsServerWork { get; private set; }
        public bool IsServerClosed { get; private set; }

        const bool SERVER_MESSAGE = true;
        const bool NEED_MESSAGE = true;
        const bool NOT_NEED_MESSAGE = false;

        const string MESSG_WAITING_FOR_CLIENTS = "Сервер запущен. Ожидание подключений...";
        const string MESSG_SERVER_STOPPED = "Сервер остановлен.";
        const string MESSG_CLIENT_CONNECTED_TO_SERVER = "Пользователь <{0}> подключился к серверу.";
        const string MESSG_CLIENT_DISCONNECTED_FROM_SERVER = "Пользователь <{0}> отключён от сервера.";

        int maxAmoOfConnectedClients;

        CIDManager idManager;
        IPEndPoint listenPoint;
        Socket listenSocket;
        Thread thrListeningForNewClients;
        CView UI;
        List<CClient> clients;

        /*  Конструктор класса  */
        public CServer(string ip, int port, int clientsAmo, CView currUI)
        {
            idManager = CIDManager.GetInstance();
            clients = new List<CClient>();
            listenPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            UI = currUI;
            maxAmoOfConnectedClients = clientsAmo;
            AmoOfConnectedClients = 0;
            IsServerWork = false;
            IsServerClosed = false;
            
        }

        /* Подпрограмма для запуска сервера */
        public void Start()
        {
            thrListeningForNewClients = new Thread(ListenIncomingConnections);
            thrListeningForNewClients.IsBackground = true;
            thrListeningForNewClients.Start();
        }

        /*  Подпрограмма прослушивания входящих подключений  */
        private void ListenIncomingConnections()
        {
            try
            {
                listenSocket.Bind(listenPoint);
                listenSocket.Listen(10);
                UI.ShowText(MESSG_WAITING_FOR_CLIENTS, idManager.ID_SERVER_DEFAULT);
                IsServerWork = true;
                UI.UpdateServerInfo();
            }
            catch (Exception ex)
            {
                UI.ShowText(ex.Message, idManager.ID_SERVER_ERROR);
            }

            if (IsServerWork)
            {
                try
                {
                    while (true)
                    {
                        Socket handler = listenSocket.Accept();
                        try
                        {
                            TryToConnectNewClient(handler);
                        }
                        catch (Exception) { }
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    RemoveAllConnections(NEED_MESSAGE);
                    UI.ShowText(MESSG_SERVER_STOPPED, idManager.ID_SERVER_DEFAULT);
                    IsServerWork = false;
                }
            }
        }

        /* Подпрограмма для обработки запроса на подключение */
        private void TryToConnectNewClient(Socket newClientSocket)
        {
            sbyte newClientID = idManager.GetID();
            CClient newClient = new CClient(newClientSocket, this, newClientID, UI);
            newClient.GetClientName();
            MessageType errorType;
            if (IsPossibleToConnectClient(newClient, out errorType))
            {
                newClient.ConfirmConnection();
                newClient.Start();
                UI.UpdateServerInfo();
            }
            else
            {
                newClient.RejectConnection(errorType);
                idManager.FreeID(newClientID);
            }
        }

        /* Подпрограмма проверки нового клиента */
        private bool IsPossibleToConnectClient(CClient newClient, out MessageType errorType)
        {
            string messageForOthers;
            int numOfNewClient;

            if (IsServerClosed)
            {
                errorType = MessageType.ServerClosed;
                return false;
            }

            if (AmoOfConnectedClients >= maxAmoOfConnectedClients)
            {
                errorType = MessageType.ServerOverloaded;
                return false;
            }

            if (IsNameConflict(newClient.ClientName))
            {
                errorType = MessageType.NameConflict;
                return false;
            }

            AmoOfConnectedClients++;
            numOfNewClient = AmoOfConnectedClients - 1;
            clients.Add(newClient);

            messageForOthers = String.Format(MESSG_CLIENT_CONNECTED_TO_SERVER, clients[numOfNewClient].ClientName);
            UI.ShowText(messageForOthers, clients[numOfNewClient].ID);
            UI.AddClientToList(clients[numOfNewClient].ClientName, clients[numOfNewClient].ID);

            errorType = MessageType.ConnectionConfirmed;
            return true;
        }

        /*  Подпрограмм проверки, есть ли пользователь
         *  с заданным именем на сервере  */
        private bool IsNameConflict(string name)
        {
            bool res;
            int i;
            res = false;

            i = 0;
            while ((i < AmoOfConnectedClients) && (!res))
            {
                if (string.Compare(name, clients[i].ClientName, true) == 0)
                    res = true;
                i++;
            }

            return res;
        }

        /* Подпрограмма отправки новому пользователю
         * списка всех клиентов сервера */
        public void SendClientsList(CClient receiver)
        {
            receiver.Writer.Write((byte)(clients.Count - 1));
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].ID != receiver.ID)
                {
                    receiver.Writer.Write(clients[i].ID);
                    receiver.Writer.Write(clients[i].ClientName);
                }
            }
        }

        /* Подпрограмма отправки новому пользователю 
         * таблицы цветов */
        public void SendPalette(CClient receiver)
        {
            Color[] curPalette = CColorsLoader.GetInstance().GetCurrentPalette();
            receiver.Writer.Write(curPalette.Length);
            for (int i = 0; i < curPalette.Length; i++)
            {
                receiver.Writer.Write(curPalette[i].R);
                receiver.Writer.Write(curPalette[i].G);
                receiver.Writer.Write(curPalette[i].B);
            }
        }

        /*  Подпрограмма отключения заданного клиента  */
        public void DisconnectClient(sbyte clientID)
        {
            int discClientInd;
            string discClientName;

            discClientInd = GetClientIndByID(clientID);
            if (discClientInd < 0)
            {
                return;
            }

            discClientName = clients[discClientInd].ClientName;

            clients[discClientInd].Stop();
            RemoveConnection(clients[discClientInd].ID);
            UI.UpdateServerInfo();

            NotifyAllClientsAboutUserLeft(discClientName, clientID);
        }

        /*  Подпрограмма отключения всех клиентов  */
        public void RemoveAllConnections(bool isNeedMessage)
        {
            for (int i = 0; i < AmoOfConnectedClients; i++)
            {
                if (clients[i] != null)
                {
                    clients[i].Stop();
                    idManager.FreeID(clients[i].ID);
                    if (isNeedMessage)
                        UI.ShowText(String.Format(MESSG_CLIENT_DISCONNECTED_FROM_SERVER, clients[i].ClientName), clients[i].ID);
                }
            }
            clients.Clear();
            AmoOfConnectedClients = 0;
            UI.RemoveAllClientsFromList();
            UI.UpdateServerInfo();
        }

        /*  Подпрограмма проверки, заблокирован
         *  ли сервером клиент с заданным ID  */
        public bool IsClientBanned(sbyte ID)
        {
            int clientInd = GetClientIndByID(ID);
            if (clientInd < 0)
            {
                return false;
            }

            return clients[clientInd].IsClientBanned;
        }

        /*  Подпрограмма блокировки клиента с ID  */
        public void BanClient(sbyte clientID)
        {
            int clientInd = GetClientIndByID(clientID);
            if (clientInd < 0)
            {
                return;
            }

            clients[clientInd].IsClientBanned = true;
            try
            {
                clients[clientInd].Writer.Write((byte)MessageType.ClientBanned);
            }
            catch (Exception)
            {
                UI.RemoveClientFromList(clients[clientInd].ID);
                DisconnectClient(clients[clientInd].ID);
            }
        }

        /*  Подпрограмма снятия блокировки с клиента с заданным ID  */
        public void ActivateClient(sbyte clientID)
        {
            int clientInd = GetClientIndByID(clientID);
            if (clientInd < 0)
            {
                return;
            }

            clients[clientInd].IsClientBanned = false;
            try
            {
                clients[clientInd].Writer.Write((byte)MessageType.ClientActivated);
            }
            catch (Exception)
            {
                UI.RemoveClientFromList(clients[clientInd].ID);
                DisconnectClient(clients[clientInd].ID);
            }
        }

        /*  Подпрограмма отключения клиента с заданным  ID  */
        public void RemoveConnection(sbyte ID)
        {
            int clientInd = GetClientIndByID(ID);
            if (clientInd < 0)
            {
                return; 
            }

            AmoOfConnectedClients--;
            idManager.FreeID(clients[clientInd].ID);
            clients.RemoveAt(clientInd);
        }

        /* Подпрограмма получения индекса клиента по его ID */
        public int GetClientIndByID(int ID)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].ID == ID)
                {
                    return i;
                }
            }
            return -1;
        }

        /*  Подпрограмма остановки сервера  */
        public void Stop()
        {
            if (IsServerWork && listenSocket.IsBound)
            {
                try
                {
                    listenSocket.Close();
                }
                catch (Exception ex)
                {
                    UI.ShowText(ex.Message, idManager.ID_SERVER_ERROR);
                }
            }
        }

        /*  Подпрограмма отправки группового сообщения  */
        public void SendPublicMessage(string message, sbyte senderID)
        {
            for (int i = 0; i < AmoOfConnectedClients; i++)
            {
                if (clients[i].ID != senderID)
                {
                    try
                    {
                        clients[i].Writer.Write((byte)MessageType.PublicMessage);
                        clients[i].Writer.Write(senderID);
                        clients[i].Writer.Write(message);
                    }
                    catch (Exception)
                    {
                        UI.RemoveClientFromList(clients[i].ID);
                        DisconnectClient(clients[i].ID);
                        i--;
                    }
                }
            }
        }

        /* Подпрограмма отправки личного сообщения */
        public void SendPrivateMessage(string message, sbyte senderID, sbyte receiverID)
        {
            int receiverInd = GetClientIndByID(receiverID);
            if (receiverInd < 0)
            {
                return;
            }

            try
            {
                clients[receiverInd].Writer.Write((byte)MessageType.PrivateMessage);
                clients[receiverInd].Writer.Write(senderID);
                clients[receiverInd].Writer.Write(message);
            }
            catch (Exception)
            {
                UI.RemoveClientFromList(clients[receiverInd].ID);
                DisconnectClient(clients[receiverInd].ID);
            }
        }

        /* Подпрограмма блокировки одного пользователя другим */
        public void BanUserByAnotherUser(sbyte senderID, sbyte receiverID)
        {
            int receiverInd = GetClientIndByID(receiverID);
            if (receiverInd < 0)
            {
                return;
            }

            try
            {
                clients[receiverInd].Writer.Write((byte)MessageType.BannedByUser);
                clients[receiverInd].Writer.Write(senderID);
            }
            catch (Exception)
            {
                UI.RemoveClientFromList(clients[receiverInd].ID);
                DisconnectClient(clients[receiverInd].ID);
            }
        }

        /* Подпрограмма разблокировки одного пользователя другим */
        public void ActivateUserByAnotherUser(sbyte senderID, sbyte receiverID)
        {
            int receiverInd = GetClientIndByID(receiverID);
            if (receiverInd < 0)
            {
                return;
            }

            try
            {
                clients[receiverInd].Writer.Write((byte)MessageType.ActivatedByUser);
                clients[receiverInd].Writer.Write(senderID);
            }
            catch (Exception)
            {
                UI.RemoveClientFromList(clients[receiverInd].ID);
                DisconnectClient(clients[receiverInd].ID);
            }
        }

        /* Подпрограмма уведомления всех клиентов о 
         * подключении нового пользователя */
        public void NotifyAllClientsAboutNewUser(string username, sbyte userID)
        {
            for (int i = 0; i < AmoOfConnectedClients; i++)
            {
                if (clients[i].ID != userID)
                {
                    try
                    {
                        clients[i].Writer.Write((byte)MessageType.NewUser);
                        clients[i].Writer.Write(username);
                        clients[i].Writer.Write(userID);
                    }
                    catch (Exception)
                    {
                        UI.RemoveClientFromList(clients[i].ID);
                        DisconnectClient(clients[i].ID);
                        i--;
                    }
                }
            }
        }

        /* Подпрограмма уведомления всех клиентов об отключении
         * одного из пользователей */
        public void NotifyAllClientsAboutUserLeft(string username, sbyte userID)
        {
            for (int i = 0; i < AmoOfConnectedClients; i++)
            {
                if (clients[i].ID != userID)
                {
                    try
                    {
                        clients[i].Writer.Write((byte)MessageType.UserLeave);
                        clients[i].Writer.Write(userID);
                    }
                    catch (Exception)
                    {
                        UI.RemoveClientFromList(clients[i].ID);
                        DisconnectClient(clients[i].ID);
                        i--;
                    }
                }
            }

            string message = String.Format(MESSG_CLIENT_DISCONNECTED_FROM_SERVER, username);
            UI.ShowText(message, userID);
        }

        /* Подпрограмма закрытия сервера для новых клиентов */
        public void CloseForIncomingConnections()
        {
            IsServerClosed = true;
        }

        /* Подпрограмма открытия сервера для новых клиентов */
        public void OpenForIncomingConnections()
        {
            IsServerClosed = false;
        }
    }
}

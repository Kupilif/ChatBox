using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBoxServer
{
    class CView
    {
        private MainWindow userInterface;

        delegate void delShowText(string message, sbyte colorInd);
        delegate void delAddClientToList(string username, sbyte ID);
        delegate void delRemoveClientFromList(sbyte ID);
        delegate void delUpdateServerInfo();
        delegate void delRemoveAllClientsFromList();

        public CView(MainWindow form)
        {
            userInterface = form;
        }

        public void ShowText(string message, sbyte colorInd)
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delShowText method = new delShowText(ShowText);
                userInterface.Dispatcher.Invoke(method, new object[] { message, colorInd});
            }
            else
            {
                userInterface.WriteTextToRTB(message, colorInd);
            }
        }

        public void AddClientToList(string username, sbyte ID)
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delAddClientToList method = new delAddClientToList(AddClientToList);
                userInterface.Dispatcher.Invoke(method, new object[] { username, ID });
            }
            else
            {
                userInterface.AddClientToList(username, ID);
            }
        }

        public void RemoveClientFromList(sbyte ID)
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delRemoveClientFromList method = new delRemoveClientFromList(RemoveClientFromList);
                userInterface.Dispatcher.Invoke(method, new object[] { ID });
            }
            else
            {
                userInterface.RemoveClientFromList(ID);
            }
        }

        public void RemoveAllClientsFromList()
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delRemoveAllClientsFromList method = new delRemoveAllClientsFromList(RemoveAllClientsFromList);
                userInterface.Dispatcher.Invoke(method);
            }
            else
            {
                userInterface.RemoveAllClientsFromList();
            }
        }

        public void UpdateServerInfo()
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delUpdateServerInfo method = new delUpdateServerInfo(UpdateServerInfo);
                userInterface.Dispatcher.Invoke(method);
            }
            else
            {
                userInterface.UpdateServerInfo();
            }
        }
    }
}

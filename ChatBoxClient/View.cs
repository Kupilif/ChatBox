using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ChatBoxClient
{
    class CView
    {
        private MainWindow userInterface;

        delegate void delShowText(string message, sbyte id, RichTextBox writingPad);
        delegate void delUpdateConnectionInfo();
        delegate RichTextBox delGetWritingPad();
        delegate void delRemoveWritingPad(RichTextBox rtb);
        delegate void delAddElemToList(string name, sbyte ID);
        delegate void delAddFirstElemToList();
        delegate void delSelectElemInList(int ind);
        delegate void delRemoveElemFromList(int ind);
        delegate void delRemoveAllElemsFromList();
        delegate void delSetMessagesCounter(int ind, int value);
        delegate void delSetVisibiltyOfRTB(RichTextBox rtb, bool value);
        delegate void delDisposeRTB(RichTextBox rtb);

        public CView(MainWindow form)
        {
            userInterface = form;
        }

        public void ShowText(string message, sbyte id, RichTextBox writingPad)
        {
            
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delShowText method = new delShowText(ShowText);
                userInterface.Dispatcher.Invoke(method, new object[] { message, id, writingPad });
            }
            else
            {
                userInterface.ShowMessage(message, id, writingPad);
            }
        }

        public void UpdateConnectionInfo()
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delUpdateConnectionInfo method = new delUpdateConnectionInfo(UpdateConnectionInfo);
                userInterface.Dispatcher.Invoke(method);
            }
            else
            {
                userInterface.UpdateConnectionInfo();
            }
        }

        public void AddElemToList(string name, sbyte ID)
        {

            if (!userInterface.Dispatcher.CheckAccess())
            {
                delAddElemToList method = new delAddElemToList(AddElemToList);
                userInterface.Dispatcher.Invoke(method, new object[] { name, ID });
            }
            else
            {
                userInterface.AddItemToListBox(name, ID);
            }
        }

        public void AddFirstElemToList()
        {

            if (!userInterface.Dispatcher.CheckAccess())
            {
                delAddFirstElemToList method = new delAddFirstElemToList(AddFirstElemToList);
                userInterface.Dispatcher.Invoke(method);
            }
            else
            {
                userInterface.AddFirstItemToListBox();
            }
        }

        public void RemoveElemFromList(int ind)
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delRemoveElemFromList method = new delRemoveElemFromList(RemoveElemFromList);
                userInterface.Dispatcher.Invoke(method, new object[] { ind });
            }
            else
            {
                userInterface.DeleteItemFromListBox(ind);
            }
        }

        public void RemoveAllElemsFromList()
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delRemoveAllElemsFromList method = new delRemoveAllElemsFromList(RemoveAllElemsFromList);
                userInterface.Dispatcher.Invoke(method);
            }
            else
            {
                userInterface.DeleteAllItemsFromListBox();
            }
        }

        public RichTextBox GetWritingPad()
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delGetWritingPad method = new delGetWritingPad(GetWritingPad);
                return (RichTextBox) userInterface.Dispatcher.Invoke(method);
            }
            else
            {
                return userInterface.GetRTB();
            }
        }

        public void RemoveWritingPad(RichTextBox rtb)
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delRemoveWritingPad method = new delRemoveWritingPad(RemoveWritingPad);
                userInterface.Dispatcher.Invoke(method, new object[] { rtb});
            }
            else
            {
                userInterface.RemoveRTB(rtb);
            }
        }

        public void SetMessagesCounter(int ind, int value)
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delSetMessagesCounter method = new delSetMessagesCounter(SetMessagesCounter);
                userInterface.Dispatcher.Invoke(method, new object[] { ind, value });
            }
            else
            {
                userInterface.SetMessagesCounter(ind, value);
            }
        }

        public void SetVisibilityOfRTB(RichTextBox rtb, bool isVisible)
        {
            if (!userInterface.Dispatcher.CheckAccess())
            {
                delSetVisibiltyOfRTB method = new delSetVisibiltyOfRTB(SetVisibilityOfRTB);
                userInterface.Dispatcher.Invoke(method, new object[] { rtb, isVisible });
            }
            else
            {
                if (isVisible)
                {
                    rtb.Visibility = Visibility.Visible;
                }
                else
                {
                    rtb.Visibility = Visibility.Hidden;
                }
                userInterface.UpdateConnectionInfo();
            }
        }

        public void MakeSmile(string name, BitmapImage smile)
        {
            userInterface.MakeSmileButton(name, smile);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ChatBoxClient
{
    class CUsersManager
    {
        private List<string> usernames;
        private List<sbyte> usersID;
        private List<int> newMessagesAmo;
        private List<bool> isUserBannedByMe;
        private List<bool> isSelfBannedByUser;
        private List<RichTextBox> writingPads;

        private const int PUBLIC_DIALOG_INDEX = -1;

        private RichTextBox mainWritingPad;
        private RichTextBox curWritingPad;
        private CView UI;
        private int curDialogInd;
        private int publicDialogMessagesAmo;

        public CUsersManager(CView currUI, RichTextBox writingPad)
        {
            UI = currUI;
            mainWritingPad = writingPad;
            curWritingPad = mainWritingPad;
            curDialogInd = PUBLIC_DIALOG_INDEX;
            publicDialogMessagesAmo = 0;

            usernames = new List<string>();
            usersID = new List<sbyte>();
            isSelfBannedByUser = new List<bool>();
            isUserBannedByMe = new List<bool>();
            newMessagesAmo = new List<int>();
            writingPads = new List<RichTextBox>();
        }

        public void AddUser(string name, sbyte ID)
        {
            usernames.Add(name);
            usersID.Add(ID);
            newMessagesAmo.Add(0);
            isSelfBannedByUser.Add(false);
            isUserBannedByMe.Add(false);
            writingPads.Add(UI.GetWritingPad());
            UI.AddElemToList(name, ID);
        }

        public void RemoveUser(sbyte ID)
        {
            int ind = GetUserIndByID(ID);
            usernames.RemoveAt(ind);
            usersID.RemoveAt(ind);
            isSelfBannedByUser.RemoveAt(ind);
            isUserBannedByMe.RemoveAt(ind);
            newMessagesAmo.RemoveAt(ind);

            if (curDialogInd == ind)
            {

                curDialogInd = PUBLIC_DIALOG_INDEX;
                curWritingPad = mainWritingPad;
                UI.SetVisibilityOfRTB(curWritingPad, true);
                
            }
            if (curDialogInd > ind)
            {
                curDialogInd--;
            }

            UI.RemoveWritingPad(writingPads[ind]);
            writingPads.RemoveAt(ind);

            UI.RemoveElemFromList(ind);
            UpdateDialogSelection();
        }

        public void RemoveAllUsers()
        {
            UI.SetVisibilityOfRTB(curWritingPad, false);
            curWritingPad = mainWritingPad;
            UI.SetVisibilityOfRTB(curWritingPad, true);
            curDialogInd = PUBLIC_DIALOG_INDEX;
            publicDialogMessagesAmo = 0;
            usernames.Clear();
            usersID.Clear();
            isUserBannedByMe.Clear();
            isSelfBannedByUser.Clear();
            newMessagesAmo.Clear();
            for (int i = 0; i > writingPads.Count; i++)
            {
                UI.RemoveWritingPad(writingPads[i]);
            }
            writingPads.Clear();
            UI.RemoveAllElemsFromList();
        }

        public void ShowWritingPad(int ind)
        {
            UI.SetVisibilityOfRTB(curWritingPad, false);
            if (ind < 0)
            {
                curDialogInd = PUBLIC_DIALOG_INDEX;
                curWritingPad = mainWritingPad;
            }
            else
            {
                curDialogInd = ind;
                curWritingPad = writingPads[ind];
            }
            UI.SetVisibilityOfRTB(curWritingPad, true);
            UpdateDialogSelection();
        }

        public void UpdateDialogSelection()
        {
            if (curDialogInd != PUBLIC_DIALOG_INDEX)
            {
                newMessagesAmo[curDialogInd] = 0;
            }
            else
            {
                publicDialogMessagesAmo = 0;
            }
            UI.SetMessagesCounter(curDialogInd, 0);
        }

        public int GetUserIndByID(sbyte ID)
        {
            for (int i = 0; i < usersID.Count; i++)
            {
                if (usersID[i] == ID)
                {
                    return i;
                }
            }
            return 0;
        }

        public string GetUsernameByID(sbyte ID)
        {
            for (int i = 0; i < usernames.Count; i++)
            {
                if (usersID[i] == ID)
                {
                    return usernames[i];
                }
            }
            return String.Empty;
        }

        public sbyte GetIDByInd(int ind)
        {
            if ((ind >= 0) && (ind < usersID.Count))
            {
                return usersID[ind];
            }
            else
            {
                return -1;
            }
        }

        public sbyte GetIDOfCurrentUser()
        {
            return usersID[curDialogInd];
        }

        public bool IsUserBannedByMe(sbyte ID)
        {
            int ind = GetUserIndByID(ID);
            return isUserBannedByMe[ind];
        }

        public bool IsPublicMessage()
        {
            if (curDialogInd == PUBLIC_DIALOG_INDEX)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ShowMessageInMainWritingPad(string message, sbyte id)
        {
            UI.ShowText(message, id, mainWritingPad);
            if (curDialogInd != PUBLIC_DIALOG_INDEX)
            {
                publicDialogMessagesAmo++;
                UI.SetMessagesCounter(PUBLIC_DIALOG_INDEX, publicDialogMessagesAmo);
            }
        }

        public void ShowMessageInCurrentWritingPad(string message, sbyte id)
        {
            UI.ShowText(message, id, curWritingPad);
        }

        public void ShowMessageInSpecifiedWritingPad(string message, sbyte id)
        {
            int writingPadInd = GetUserIndByID(id);
            UI.ShowText(message, id, writingPads[writingPadInd]);
            int ind = GetUserIndByID(id);
            if (ind != curDialogInd)
            {
                newMessagesAmo[ind]++;
                UI.SetMessagesCounter(ind, newMessagesAmo[ind]);
            }
        }

        public void AddSelectedUserToPersonalBlackList(sbyte ID)
        {
            int ind = GetUserIndByID(ID);
            isUserBannedByMe[ind] = true;
        }

        public void RemoveSelectedUserFromPersonalBlackList(sbyte ID)
        {
            int ind = GetUserIndByID(ID);
            isUserBannedByMe[ind] = false;
        }

        public void AddSelfToBlackList(sbyte senderID)
        {
            int senderInd = GetUserIndByID(senderID);
            isSelfBannedByUser[senderInd] = true;
        }

        public void RemoveSelfToBlackList(sbyte senderID)
        {
            int senderInd = GetUserIndByID(senderID);
            isSelfBannedByUser[senderInd] = false;
        }

        public bool IsBannedByUser(sbyte userID)
        {
            int ind = GetUserIndByID(userID);
            return isSelfBannedByUser[ind];
        }

        public bool IsUserBannedForCurrentDialog()
        {
            return isSelfBannedByUser[curDialogInd];
        }
    }
}

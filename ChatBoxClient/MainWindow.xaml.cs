using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using CommonClasses;

namespace ChatBoxClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MAX_USERNAME = 20;
        private const int PAGE_MAINMENU = 0;
        private const int PAGE_DIALOGS = 1;
        private const double BTN_USER_STATUS_SIZE_PX = 20;

        private const string TEMPLATE_NEW_MESSAGE = "+{0}";
        private const string TEMPLATE_IP = "IP сервера: {0}";
        private const string TEMPLATE_PORT = "Порт сервера: {0}";
        private const string TEMPLATE_CONNECTION_STATUS_ACTIVE = "Подключён, активен";
        private const string TEMPLATE_CONNECTION_STATUS_BANNED = "Подключён, заблокирован";
        private const string TEMPLATE_CONNECTION_STATUS_DISCONNECTED = "Отключён";
        private const string TEMPLATE_NO_USERNAME = "-----";
        private const string TEMPLATE_BTN_BAN = "btnBanUser_{0}";

        bool isIPEntered = false;
        bool isPortEntered = false;
        bool isUsernameEntered = false;
        string serverIP;
        string enteredUsername;
        int serverPort;
        int curPage = PAGE_MAINMENU;

        CUser user;
        CView currentUI;
        CSmiles smiles;
        CColorsLoader colorsManager;
        CUsersManager usersManager;

        public MainWindow()
        {
            InitializeComponent();

            colorsManager = CColorsLoader.GetInstance();
            currentUI = new CView(this);
            usersManager = new CUsersManager(currentUI, rtbPublicDialog);
            smiles = new CSmiles(currentUI);
            smiles.LoadSmiles();

            btnSmiles.Content = colorsManager.GetSmileDraw(btnSmiles.Width);
            SetAvaliabiltyOfMenuItems();
        }

        public void MakeSmileButton(string name, BitmapImage smile)
        {
            Image img = new Image();
            img.Height = smile.Height;
            img.Width = smile.Width;
            img.Source = smile;

            Button btn = new Button();
            btn.Name = name;
            btn.Background = new SolidColorBrush(Colors.White);
            btn.Content = img;
            btn.Click += new RoutedEventHandler(btnSmile_Click);
            wpSmiles.Children.Add(btn);
        }

        public void UpdateConnectionInfo()
        {
            labIP.Content = String.Format(TEMPLATE_IP, serverIP);
            labPort.Content = String.Format(TEMPLATE_PORT, serverPort);
            labIP.Foreground = new SolidColorBrush(colorsManager.ClrIPandPort);
            labPort.Foreground = new SolidColorBrush(colorsManager.ClrIPandPort);
            labUsernameText.Foreground = new SolidColorBrush(colorsManager.ClrLabelText);
            labConnectionStatusText.Foreground = new SolidColorBrush(colorsManager.ClrLabelText);

            if (IsUserConnectedToServer())
            {
                labUsername.Content = user.userName;
                labUsername.Foreground = new SolidColorBrush(colorsManager.GetColorForID(user.ID));

                tbSendMessage.IsEnabled = true;
                btnSendMessage.IsEnabled = true;

                if (!user.IsUserBannedForCurrentDialog())
                {
                    labConnectionStatus.Content = TEMPLATE_CONNECTION_STATUS_ACTIVE;
                    labConnectionStatus.Foreground = new SolidColorBrush(colorsManager.ClrClientStatusActive);
                }
                else
                {
                    labConnectionStatus.Content = TEMPLATE_CONNECTION_STATUS_BANNED;
                    labConnectionStatus.Foreground = new SolidColorBrush(colorsManager.ClrClientStatusBanned);
                }
            }
            else
            {
                tbSendMessage.IsEnabled = false;
                btnSendMessage.IsEnabled = false;

                labUsername.Content = TEMPLATE_NO_USERNAME;
                labUsername.Foreground = new SolidColorBrush(colorsManager.ClrLabelText);

                labConnectionStatus.Content = TEMPLATE_CONNECTION_STATUS_DISCONNECTED;
                labConnectionStatus.Foreground = new SolidColorBrush(colorsManager.ClrClientStatusDisconnected);
            }

            SetAvaliabiltyOfMenuItems();
        }

        public void AddItemToListBox(string name, sbyte ID)
        {
            ListBoxItem lbi = new ListBoxItem();
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            Button btn = new Button();
            btn.Width = BTN_USER_STATUS_SIZE_PX;
            btn.Height = BTN_USER_STATUS_SIZE_PX;
            btn.Background = new SolidColorBrush(Colors.White);
            btn.Content = colorsManager.GetPicForActiveUser(BTN_USER_STATUS_SIZE_PX);
            btn.Name = String.Format(TEMPLATE_BTN_BAN, ID);
            btn.Click += new RoutedEventHandler(btnChangeUserStatus_Click);
            sp.Children.Add(btn);

            TextBlock tb = new TextBlock();
            tb.Width = 30;
            tb.FontWeight = FontWeights.Bold;
            sp.Children.Add(tb);

            tb = new TextBlock();
            tb.Text = name;
            tb.Foreground = new SolidColorBrush(colorsManager.GetColorForID(ID));
            tb.FontWeight = FontWeights.Bold;
            sp.Children.Add(tb);

            lbi.Content = sp;
            lbUsers.Items.Add(lbi);
        }

        public void AddFirstItemToListBox()
        {
            ListBoxItem lbi = new ListBoxItem();
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            Button btn = new Button();
            btn.Width = BTN_USER_STATUS_SIZE_PX;
            btn.Height = BTN_USER_STATUS_SIZE_PX;
            btn.Background = new SolidColorBrush(Colors.White);
            btn.Content = colorsManager.GetPicForActiveUser(BTN_USER_STATUS_SIZE_PX);
            btn.Click += new RoutedEventHandler(msiDisconnectFromServer_Click);
            sp.Children.Add(btn);

            TextBlock tb = new TextBlock();
            tb.Width = 30;
            tb.FontWeight = FontWeights.Bold;
            sp.Children.Add(tb);

            tb = new TextBlock();
            tb.Text = "Общий чат";
            tb.Foreground = new SolidColorBrush(colorsManager.ClrDefault);
            tb.FontWeight = FontWeights.Bold;
            sp.Children.Add(tb);

            lbi.Content = sp;
            lbUsers.Items.Add(lbi);
        }

        public void DeleteItemFromListBox(int ind)
        {
            lbUsers.Items.RemoveAt(ind + 1);
        }

        public void DeleteAllItemsFromListBox()
        {
            lbUsers.Items.Clear();
        }

        public RichTextBox GetRTB()
        {
            RichTextBox rtb = new RichTextBox();
            rtb.Document = new FlowDocument();
            rtb.Document.Blocks.Add(new Paragraph());
            rtb.FontFamily = rtbPublicDialog.FontFamily;
            rtb.FontSize = rtbPublicDialog.FontSize;
            rtb.FontStretch = rtbPublicDialog.FontStretch;
            rtb.FontStyle = rtbPublicDialog.FontStyle;
            rtb.FontWeight = rtbPublicDialog.FontWeight;
            rtb.Background = rtbPublicDialog.Background;
            rtb.Margin = rtbPublicDialog.Margin;
            rtb.VerticalScrollBarVisibility = rtbPublicDialog.VerticalScrollBarVisibility;
            rtb.IsReadOnly = true;
            rtb.Visibility = Visibility.Hidden;
            panelDialogs.Children.Add(rtb);
            return rtb;
        }

        public void RemoveRTB(RichTextBox rtb)
        {
            if (panelDialogs.Children.Contains(rtb))
            {
                int ind = panelDialogs.Children.IndexOf(rtb);
                panelDialogs.Children.RemoveAt(ind);
            }
        }

        public void SetMessagesCounter(int ind, int value)
        {
            string strVal;
            if (value == 0)
            {
                strVal = String.Empty;
            }
            else
            {
                strVal = String.Format(TEMPLATE_NEW_MESSAGE, value);
            }

            (((lbUsers.Items[ind + 1] as ListBoxItem).Content as StackPanel).Children[1] as TextBlock).Text = strVal;
        }

        public void ShowMessage(string message, sbyte id, RichTextBox rtb)
        {
            string[] partsOfMessage;
            BitmapImage[] foundSmiles;
            int curSmileNum = 0;
            partsOfMessage = smiles.ParseMessage(message, out foundSmiles);

            for (int i = 0; i < partsOfMessage.Length; i++)
            {
                if (partsOfMessage[i].Equals(String.Empty))
                {
                    InsertImageToRTB(foundSmiles[curSmileNum], rtb);
                    curSmileNum++;
                }
                else
                {
                    WriteTextToRTB(partsOfMessage[i], id, rtb);
                }
            }
            WriteTextToRTB("\n", id, rtb);
            try
            {
                rtb.ScrollToEnd();
            }
            catch (Exception) { }
        }

        private void WriteTextToRTB(string message, sbyte id, RichTextBox rtb)
        {
            rtb.CaretPosition.Paragraph.Inlines.Add(new Run(message));
            rtb.CaretPosition.Paragraph.Inlines.LastInline.Foreground = 
                new SolidColorBrush(colorsManager.GetColorForID(id));
            try
            {
                rtb.ScrollToEnd();
            }
            catch (Exception) { }
        }

        private void InsertImageToRTB(BitmapImage smile, RichTextBox rtb)
        {
            Image img = new Image();
            img.Width = smile.Width;
            img.Height = smile.Height;
            img.Source = smile;
            InlineUIContainer imgc = new InlineUIContainer(img);
            rtb.CaretPosition.Paragraph.Inlines.Add(imgc);
        }

        /* Отрисовка различных картинок */



        /* -------------------------------------------- */

        private bool IsUserConnectedToServer()
        {
            return (user != null) && (user.isUserConnectedToServer);
        }

        private void ConnectToServer()
        {
            rtbPublicDialog.Document.Blocks.Clear();
            rtbPublicDialog.Document.Blocks.Add(new Paragraph());
            user = new CUser(enteredUsername, serverIP, serverPort, currentUI, usersManager);
            user.ConnectToServer();
        }

        private void DisconnectFromServer()
        {
            if (IsUserConnectedToServer())
                user.DisconnectFromServer();
        }

        private void SendMessage()
        {
            if ((IsUserConnectedToServer()) && (tbSendMessage.Text.Length != 0))
            {
                if (usersManager.IsPublicMessage())
                {
                    user.SendPublicMessage(tbSendMessage.Text);
                }
                else
                {
                    user.SendPrivateMessage(tbSendMessage.Text);
                }
                tbSendMessage.Clear();
            }
        }

        /* Проверка корректности введённых данных */

        private bool CheckIP()
        {
            IPAddress ip;
            if (IPAddress.TryParse(tbServerIP.Text, out ip))
            {
                serverIP = tbServerIP.Text;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckPort()
        {
            int port;
            if (!Int32.TryParse(tbServerPort.Text, out port))
            {
                return false;
            }
            if ((port < 0) || (port > 65535))
            {
                return false;
            }

            serverPort = port;
            return true;
        }

        private bool CheckUsername()
        {
            if ((tbName.Text.Length > MAX_USERNAME) || (tbName.Text.Length == 0))
            {
                return false;
            }
            else
            {
                enteredUsername = tbName.Text;
                return true;
            }
        }

        private void UpdateAvailabilityOfConnection()
        {
            btnConnectToServer.IsEnabled = (isIPEntered && isPortEntered && isUsernameEntered);
        }

        private void SetAvaliabiltyOfMenuItems()
        {
            if (IsUserConnectedToServer())
            {
                msiDisconnectFromServer.IsEnabled = true;
                msiMainMenu.IsEnabled = false;
                msiExit.IsEnabled = false;
            }
            else
            {
                msiDisconnectFromServer.IsEnabled = false;
                if (curPage != PAGE_MAINMENU)
                    msiMainMenu.IsEnabled = true;
                else
                    msiMainMenu.IsEnabled = false;
                msiExit.IsEnabled = true;
            }
        }

        /* ------------------------------------------------ */

        /* Обработчики событий */

        private void btnChangeUserStatus_Click(object sender, RoutedEventArgs e)
        {
            string[] info = (sender as Button).Name.Split('_');
            sbyte ID = SByte.Parse(info[1]);
            if (usersManager.IsUserBannedByMe(ID))
            {
                usersManager.RemoveSelectedUserFromPersonalBlackList(ID);
                user.ActivateUser(ID);
                (sender as Button).Content = colorsManager.GetPicForActiveUser(BTN_USER_STATUS_SIZE_PX); ;
            }
            else
            {
                usersManager.AddSelectedUserToPersonalBlackList(ID);
                user.BanUser(ID);
                (sender as Button).Content = colorsManager.GetPicForBannedUser(BTN_USER_STATUS_SIZE_PX); ;
            }
        }

        private void btnSmile_Click(object sender, RoutedEventArgs e)
        {
            if (tbSendMessage.IsEnabled)
            {
                tbSendMessage.AppendText((sender as Button).Name);
            }
        }

        private void btnSmiles_Click(object sender, RoutedEventArgs e)
        {
            pSmiles.IsOpen = !pSmiles.IsOpen;
        }

        private void tbServerIP_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CheckIP())
            {
                isIPEntered = true;
                tbServerIP.Background = new SolidColorBrush(colorsManager.clrOK);
            }
            else
            {
                isIPEntered = false;
                tbServerIP.Background = new SolidColorBrush(colorsManager.clrWRONG);
            }
            UpdateAvailabilityOfConnection();
        }

        private void tbServerPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CheckPort())
            {
                isPortEntered = true;
                tbServerPort.Background = new SolidColorBrush(colorsManager.clrOK);
            }
            else
            {
                isPortEntered = false;
                tbServerPort.Background = new SolidColorBrush(colorsManager.clrWRONG);
            }
            UpdateAvailabilityOfConnection();
        }

        private void tbName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CheckUsername())
            {
                isUsernameEntered = true;
                tbName.Background = new SolidColorBrush(colorsManager.clrOK);
            }
            else
            {
                isUsernameEntered = false;
                tbName.Background = new SolidColorBrush(colorsManager.clrWRONG);
            }
            UpdateAvailabilityOfConnection();
        }

        private void btnConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            ConnectToServer();

            pageMainMenu.Visibility = Visibility.Hidden;
            pageClientDialog.Visibility = Visibility.Visible;
            curPage = PAGE_DIALOGS;
            UpdateConnectionInfo();
            //SetAvaliabiltyOfMenuItems();
        }

        private void msiDisconnectFromServer_Click(object sender, RoutedEventArgs e)
        {
            DisconnectFromServer();
            //SetAvaliabiltyOfMenuItems();
        }

        private void msiMainMenu_Click(object sender, RoutedEventArgs e)
        {
            pageMainMenu.Visibility = Visibility.Visible;
            pageClientDialog.Visibility = Visibility.Hidden;
            curPage = PAGE_MAINMENU;
            SetAvaliabiltyOfMenuItems();
        }

        private void msiExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsUserConnectedToServer())
            {
                e.Cancel = true;
                DisconnectFromServer();
                pageMainMenu.Visibility = Visibility.Visible;
                pageClientDialog.Visibility = Visibility.Hidden;
                curPage = PAGE_MAINMENU;
                SetAvaliabiltyOfMenuItems();
            }
        }

        private void lbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbUsers.SelectedItems.Count == 1)
            {
                usersManager.ShowWritingPad(lbUsers.SelectedIndex - 1);
                UpdateConnectionInfo();
            }
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void pSmiles_MouseLeave(object sender, MouseEventArgs e)
        {
            pSmiles.IsOpen = false;
        }

        private void tbSendMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }
    }
}

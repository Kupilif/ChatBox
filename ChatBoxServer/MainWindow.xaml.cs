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

namespace ChatBoxServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double BTN_USER_STATUS_SIZE_PX = 20;
        private const int COLOR_BUTTON_SIZE_PX = 20;
        private const int PAGE_MAINMENU = 0;
        private const int PAGE_SERVER = 1;

        private const bool NEED_MESSAGE = true;
        private const bool NOT_NEED_MESSAGE = false;
        private const string TEMPLATE_CLIENTS_MAX_AMOUNT = "Максимальное число клиентов: {0}";
        private const string SERVER_STATUS_OPEN = "Открыт для входящих подключений";
        private const string SERVER_STATUS_CLOSED = "Закрыт для входящих подключений";
        private const string SERVER_STATUS_STOPPED = "Остановлен";
        private const string TEMPLATE_IP = "IP: {0}";
        private const string TEMPLATE_PORT = "Порт: {0}";
        private const string TEMPLATE_CLIENTS_AMOUNT = "Подключено пользователей: {0}/{1}";
        private const string NO_POSSIBLE_USERS = "-";
        private const string TEMPLATE_BTN_BAN_USER = "btnBanUser_{0}";
        private const string TEMPLATE_BTN_DISCONNECT_USER = "btnDisconnectUser_{0}";

        private int maxAmoOfClients = 2;
        private int serverPort;
        private string serverIP;
        private int curPage = PAGE_MAINMENU;
        private bool isIPEntered = false;
        private bool isPortEntered = false;

        CColorsLoader colorsManager;
        System.Windows.Forms.ColorDialog colorDialog;
        CView currentUI;
        CServer chatBoxServer;

        public MainWindow()
        {
            InitializeComponent();

            colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.FullOpen = true;
            colorsManager = CColorsLoader.GetInstance();
            currentUI = new CView(this);
            MakeColorsPalette();
            SetAvailabiltyOfMenuItems();
        }

        /* Работа с палитрой цветов */

        private void MakeColorsPalette()
        {
            Button btn;
            for (int i = 0; i < colorsManager.DefaultColors.Length; i++)
            {
                btn = new Button();
                btn.Width = COLOR_BUTTON_SIZE_PX;
                btn.Height = COLOR_BUTTON_SIZE_PX;
                btn.Background = new SolidColorBrush(colorsManager.DefaultColors[i]);
                btn.Click += new RoutedEventHandler(btnColor_Click);
                wpPalette.Children.Add(btn);
            }
        }

        private bool GetColorFromUser(out Color color)
        {
            if (colorDialog.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                color = Color.FromRgb(colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                return true;
            }
            color = new Color();
            return false;
        }

        private Color[] GetNewPalette()
        {
            List<Color> newPalette = new List<Color>();
            for (int i = 0; i < wpPalette.Children.Count; i++)
            {
                newPalette.Add(((wpPalette.Children[i] as Button).Background as SolidColorBrush).Color);
            }
            return newPalette.ToArray();
        }

        /* ----------------------------------- */

        /* Взаимодествие с формой */

        private void ClearRTB()
        {
            rtbServerMessages.Document.Blocks.Clear();
            rtbServerMessages.Document.Blocks.Add(new Paragraph());
        }

        public void WriteTextToRTB(string message, sbyte id)
        {
            Run line = new Run(message);
            line.Foreground = new SolidColorBrush(colorsManager.GetColorForID(id));
            line.Typography.StandardSwashes = 2; 

            rtbServerMessages.CaretPosition.Paragraph.Inlines.Add(line);
                
            rtbServerMessages.CaretPosition.Paragraph.Inlines.Add(new Run("\n"));
            try
            {
                rtbServerMessages.ScrollToEnd();
            }
            catch (Exception) { }
        }

        public void RemoveClientFromList(sbyte ID)
        {
            int clientInd = chatBoxServer.GetClientIndByID(ID);
            if (clientInd < 0)
            {
                return;
            }

            lbConnectedUsers.Items.RemoveAt(clientInd);
        }

        public void RemoveAllClientsFromList()
        {
            lbConnectedUsers.Items.Clear();
        }

        public void AddClientToList(string name, sbyte ID)
        {
            ListBoxItem lbi = new ListBoxItem();
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            Button btn = new Button();
            btn.Width = BTN_USER_STATUS_SIZE_PX;
            btn.Height = BTN_USER_STATUS_SIZE_PX;
            btn.Background = new SolidColorBrush(Colors.White);
            btn.Content = colorsManager.GetPicForActiveUser(BTN_USER_STATUS_SIZE_PX);
            btn.Name = String.Format(TEMPLATE_BTN_BAN_USER, ID);
            btn.Click += new RoutedEventHandler(btnBanUser_Click);
            sp.Children.Add(btn);

            btn = new Button();
            btn.Width = BTN_USER_STATUS_SIZE_PX;
            btn.Height = BTN_USER_STATUS_SIZE_PX;
            btn.Background = new SolidColorBrush(Colors.White);
            btn.Content = colorsManager.GetPicForDisconnectBtn(BTN_USER_STATUS_SIZE_PX);
            btn.Name = String.Format(TEMPLATE_BTN_DISCONNECT_USER, ID);
            btn.Click += new RoutedEventHandler(btnDisconnectUser_Click);
            sp.Children.Add(btn);

            TextBlock tb = new TextBlock();
            tb.Text = name;
            tb.Foreground = new SolidColorBrush(colorsManager.GetColorForID(ID));
            tb.FontWeight = FontWeights.Bold;
            sp.Children.Add(tb);

            lbi.Content = sp;
            lbConnectedUsers.Items.Add(lbi);
        }

        /* -------------------------------------------------- */

        /* Проверка введённых данных */

        private bool CheckIP()
        {
            IPAddress ip;
            if (IPAddress.TryParse(tbIP.Text, out ip))
            {
                serverIP = tbIP.Text;
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
            if (!Int32.TryParse(tbPort.Text, out port))
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

        private void UpdateAvailabilityOfServerStart()
        {
            btnStartServer.IsEnabled = (isIPEntered && isPortEntered);
        }

        /* --------------------------------------------------- */

        /* Работа с состоянием серера */

        private bool IsServerWork()
        {
            return ((chatBoxServer != null) && (chatBoxServer.IsServerWork));
        }

        private void StartServer()
        {
            if (!IsServerWork())
            {
                ClearRTB();
                try
                {
                    chatBoxServer = new CServer(serverIP, serverPort, maxAmoOfClients, currentUI);
                    chatBoxServer.Start();
                }
                catch (Exception ex)
                {
                    WriteTextToRTB(ex.Message, colorsManager.ClrErrorInd);
                }
            }
        }

        private void StopServer()
        {
            if (chatBoxServer != null)
            {
                chatBoxServer.Stop();
                chatBoxServer = null;
            }
        }

        /* -------------------------------------------------- */

        /* Настройка GUI в зависимости от состояния сервера */

        public void UpdateServerInfo()
        {
            labIP.Content = String.Format(TEMPLATE_IP, serverIP);
            labPort.Content = String.Format(TEMPLATE_PORT, serverPort.ToString());

            labIP.Foreground = new SolidColorBrush(colorsManager.ClrIPandPort);
            labPort.Foreground = new SolidColorBrush(colorsManager.ClrIPandPort);

            labServerStatusText.Foreground = new SolidColorBrush(colorsManager.ClrLabelText);

            if (IsServerWork())
            {
                if (chatBoxServer.IsServerClosed)
                {
                    labServerStatus.Content = SERVER_STATUS_CLOSED;
                    labServerStatus.Foreground = new SolidColorBrush(colorsManager.ClrServerStatusClosed);
                }
                else
                {
                    labServerStatus.Content = SERVER_STATUS_OPEN;
                    labServerStatus.Foreground = new SolidColorBrush(colorsManager.ClrServerStatusOpen);
                }

                labUsersAmo.Content = String.Format(TEMPLATE_CLIENTS_AMOUNT, chatBoxServer.AmoOfConnectedClients.ToString(), maxAmoOfClients.ToString());
                labUsersAmo.Foreground = new SolidColorBrush(colorsManager.ClrLabelText);
            }
            else
            {
                labServerStatus.Content = SERVER_STATUS_STOPPED;
                labServerStatus.Foreground = new SolidColorBrush(colorsManager.ClrServerStatusStopped);

                labUsersAmo.Content = String.Format(TEMPLATE_CLIENTS_AMOUNT, NO_POSSIBLE_USERS, NO_POSSIBLE_USERS);
                labUsersAmo.Foreground = new SolidColorBrush(colorsManager.ClrLabelText);
            }
            SetAvailabiltyOfMenuItems();
        }

        private void SetAvailabiltyOfMenuItems()
        {
            if (IsServerWork())
            {
                msiMainMenu.IsEnabled = false;
                msiExit.IsEnabled = true;
                if (chatBoxServer.IsServerClosed)
                {
                    msiCloseServer.IsEnabled = false;
                    msiOpenServer.IsEnabled = true;
                }
                else
                {
                    msiCloseServer.IsEnabled = true;
                    msiOpenServer.IsEnabled = false;
                }
                msiStopServer.IsEnabled = true;
                msiDisconnectAllUsers.IsEnabled = true;
            }
            else
            {
                if (curPage != PAGE_MAINMENU)
                    msiMainMenu.IsEnabled = true;
                else
                    msiMainMenu.IsEnabled = false;
                msiExit.IsEnabled = true;
                msiCloseServer.IsEnabled = false;
                msiOpenServer.IsEnabled = false;
                msiStopServer.IsEnabled = false;
                msiDisconnectAllUsers.IsEnabled = false;
            }
        }

        /* --------------------------------------------------- */

        /* Обработчики событий */

        private void btnColor_Click(object sender, RoutedEventArgs e)
        {
            Color newColor;
            if (GetColorFromUser(out newColor))
            {
                (sender as Button).Background = new SolidColorBrush(newColor);
            }
        }

        private void sMaxUsersAmo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            maxAmoOfClients = (int)Math.Round((sender as Slider).Value);
            labMaxUsersAmo.Content = String.Format(TEMPLATE_CLIENTS_MAX_AMOUNT, maxAmoOfClients);
        }

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            colorsManager.SetPalette(GetNewPalette());

            pageMainMenu.Visibility = Visibility.Hidden;
            pageServerWork.Visibility = Visibility.Visible;
            curPage = PAGE_SERVER;

            StartServer();
            //UpdateServerInfo();
        }

        private void tbIP_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CheckIP())
            {
                isIPEntered = true;
                tbIP.Background = new SolidColorBrush(colorsManager.clrOK);
            }
            else
            {
                isIPEntered = false;
                tbIP.Background = new SolidColorBrush(colorsManager.clrWRONG);
            }
            UpdateAvailabilityOfServerStart();
        }

        private void tbPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CheckPort())
            {
                isPortEntered = true;
                tbPort.Background = new SolidColorBrush(colorsManager.clrOK);
            }
            else
            {
                isPortEntered = false;
                tbPort.Background = new SolidColorBrush(colorsManager.clrWRONG);
            }
            UpdateAvailabilityOfServerStart();
        }

        private void msiMainMenu_Click(object sender, RoutedEventArgs e)
        {
            pageMainMenu.Visibility = Visibility.Visible;
            pageServerWork.Visibility = Visibility.Hidden;
            curPage = PAGE_MAINMENU;
            SetAvailabiltyOfMenuItems();
        }

        private void msiExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsServerWork())
            {
                e.Cancel = true;
                StopServer();
                pageMainMenu.Visibility = Visibility.Visible;
                pageServerWork.Visibility = Visibility.Hidden;
                curPage = PAGE_MAINMENU;
                SetAvailabiltyOfMenuItems();
            }
        }

        private void msiStopServer_Click(object sender, RoutedEventArgs e)
        {
            StopServer();
            UpdateServerInfo();
        }

        private void msiOpenServer_Click(object sender, RoutedEventArgs e)
        {
            chatBoxServer.OpenForIncomingConnections();
            UpdateServerInfo();
        }

        private void msiCloseServer_Click(object sender, RoutedEventArgs e)
        {
            chatBoxServer.CloseForIncomingConnections();
            UpdateServerInfo();
        }

        private void msiDisconnectAllUsers_Click(object sender, RoutedEventArgs e)
        {
            chatBoxServer.RemoveAllConnections(NEED_MESSAGE);
            UpdateServerInfo();
        }

        private void btnBanUser_Click(object sender, RoutedEventArgs e)
        {
            string[] info = (sender as Button).Name.Split('_');
            sbyte ID = sbyte.Parse(info[1]);

            if (chatBoxServer.IsClientBanned(ID))
            {
                chatBoxServer.ActivateClient(ID);
                (sender as Button).Content = colorsManager.GetPicForActiveUser(BTN_USER_STATUS_SIZE_PX);
            }
            else
            {
                chatBoxServer.BanClient(ID);
                (sender as Button).Content = colorsManager.GetPicForBannedUser(BTN_USER_STATUS_SIZE_PX);
            }
        }

        private void btnDisconnectUser_Click(object sender, RoutedEventArgs e)
        {
            string[] info = (sender as Button).Name.Split('_');
            sbyte ID = sbyte.Parse(info[1]);

            RemoveClientFromList(ID);
            chatBoxServer.DisconnectClient(ID);   
        }
    }
}

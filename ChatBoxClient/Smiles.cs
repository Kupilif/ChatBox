using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ChatBoxClient
{
    class CSmiles
    {
        const int MAX_SMILE_NUM = 256;
        const int SMILE_CODE_LENGTH = 3;
        const char SMILES_PREFIX = '_';
        const string SMILES_DIR = "smiles";
        const string SMILES_FORMAT = "*.gif";

        CView UI;
        Dictionary<string, BitmapImage> smiles;

        public CSmiles(CView curUI)
        {
            UI = curUI;
            smiles = new Dictionary<string, BitmapImage>();
        }

        public void LoadSmiles()
        {
            if (Directory.Exists(SMILES_DIR))
            {
                string[] pictures = Directory.GetFiles(SMILES_DIR, SMILES_FORMAT);
                int i = 0;
                int curSmileNum = 0;
                BitmapImage curSmile;
                string curSmileKey;
                string curSmileCode;

                while ((i < pictures.Length) && (curSmileNum < MAX_SMILE_NUM))
                {
                    try
                    {
                        curSmile = new BitmapImage();
                        curSmile.BeginInit();
                        curSmile.UriSource = new Uri(pictures[i], UriKind.Relative);
                        curSmile.CacheOption = BitmapCacheOption.OnLoad;
                        curSmile.EndInit();

                        curSmileKey = String.Format("{0:X2}", curSmileNum);
                        curSmileCode = String.Format("{0}{1}", SMILES_PREFIX, curSmileKey);

                        smiles.Add(curSmileKey, curSmile);
                        UI.MakeSmile(curSmileCode, curSmile);
                        curSmileNum++;
                    }
                    catch (Exception ex)
                    {

                    }
                    i++;
                }
            }
        }

        public string[] ParseMessage(string message, out BitmapImage[] foundSmiles)
        {
            List<string> partsOfMessage = new List<string>();
            List<BitmapImage> pictures = new List<BitmapImage>();

            string curPartOfMessage = String.Empty;
            string curSmileCode = String.Empty;
            BitmapImage curSmilePic;
            while (message.Length != 0)
            {
                int smileInd = message.IndexOf(SMILES_PREFIX);
                if (smileInd == -1)
                {
                    curPartOfMessage = message.Substring(0, message.Length);
                    partsOfMessage.Add(curPartOfMessage);
                    message = String.Empty;
                }
                else
                {
                    if (smileInd + SMILE_CODE_LENGTH <= message.Length)
                    {
                        if (smileInd != 0)
                        {
                            curPartOfMessage = message.Substring(0, smileInd);
                            partsOfMessage.Add(curPartOfMessage);
                        }
                        curSmileCode = message.Substring(smileInd, SMILE_CODE_LENGTH);

                        if (smiles.TryGetValue(curSmileCode.Substring(1, curSmileCode.Length - 1), out curSmilePic))
                        {
                            pictures.Add(curSmilePic);
                            partsOfMessage.Add(String.Empty);
                            message = message.Remove(0, smileInd + SMILE_CODE_LENGTH);
                        }
                        else
                        {
                            partsOfMessage.Add(SMILES_PREFIX.ToString());
                            message = message.Remove(0, smileInd + 1);
                        }
                    }
                    else
                    {
                        curPartOfMessage = message.Substring(0, message.Length);
                        partsOfMessage.Add(curPartOfMessage);
                        message = String.Empty;
                    }
                }
            }

            foundSmiles = pictures.ToArray();
            return partsOfMessage.ToArray();
        }
    }
}

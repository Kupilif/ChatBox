using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBoxServer
{
    class CIDManager
    {
        private static CIDManager instance;

        public static CIDManager GetInstance()
        {
            if (instance == null)
            {
                instance = new CIDManager();
            }

            return instance;
        }

        public sbyte ID_SERVER_ERROR { get; private set; }
        public sbyte ID_SERVER_DEFAULT { get; private set; }

        const int IDENTIFIERS_AMOUNT = 30;

        Random randomNumber;
        bool[] identifiers;
        int usedIDamo;

        private CIDManager()
        {
            ID_SERVER_ERROR = -1;
            ID_SERVER_DEFAULT = -2;
            identifiers = new bool[IDENTIFIERS_AMOUNT];
            usedIDamo = 0;
            for (int i = 0; i < IDENTIFIERS_AMOUNT; i++)
            {
                identifiers[i] = false;
            }

            randomNumber = new Random();
        }

        public sbyte GetID()
        {
            int ind = randomNumber.Next(IDENTIFIERS_AMOUNT);

            while (identifiers[ind])
            {
                ind = (ind + 1) % IDENTIFIERS_AMOUNT;
            }
            identifiers[ind] = true;
            return (sbyte)ind;
        }

        public void FreeID(sbyte id)
        {
            if ((id >= 0) && ( id < IDENTIFIERS_AMOUNT))
            {
                identifiers[id] = false;
            }
        }
    }
}

using System.ComponentModel;

namespace NfcRW.Sdk;

public class NFCReader
{
    private string _readername;

    public int retCode;
    uint Protocol;
    nint hContext = nint.Zero, hCard = nint.Zero;
    public bool connActive = false;
    public byte[] SendBuff = new byte[263];
    public byte[] RecvBuff = new byte[263];
    public uint SendLen, RecvLen;
    internal enum SmartcardState
    {
        None = 0,
        Inserted = 1,
        Ejected = 2
    }

    public delegate void CardEventHandler();
    public event CardEventHandler CardInserted;
    public event CardEventHandler CardEjected;
    public event CardEventHandler DeviceDisconnected;


    private BackgroundWorker _worker;
    private SCARD_READERSTATE RdrState;
    private SCARD_READERSTATE[] states;


    private void WaitChangeStatus(object sender, DoWorkEventArgs e)
    {
        while (!e.Cancel)
        {
            int nErrCode = PCSCLite.SCardGetStatusChange(hContext, 1000, states, 1);

            if (nErrCode == PCSCLite.SCARD_E_SERVICE_STOPPED)
            {
                DeviceDisconnected();
                e.Cancel = true;
            }

            //Check if the state changed from the last time.
            if ((states[0].RdrEventState & 2) == 2)
            {
                //Check what changed.
                SmartcardState state = SmartcardState.None;
                if ((states[0].RdrEventState & 32) == 32 && (states[0].RdrCurrState & 32) != 32)
                {
                    //The card was inserted. 
                    state = SmartcardState.Inserted;
                }
                else if ((states[0].RdrEventState & 16) == 16 && (states[0].RdrCurrState & 16) != 16)
                {
                    //The card was ejected.
                    state = SmartcardState.Ejected;
                }
                if (state != SmartcardState.None && states[0].RdrCurrState != 0)
                {
                    switch (state)
                    {
                        case SmartcardState.Inserted:
                            {
                                //MessageBox.Show("Card inserted");
                                CardInserted();
                                break;
                            }
                        case SmartcardState.Ejected:
                            {
                                //MessageBox.Show("Card ejected");
                                CardEjected();
                                break;
                            }
                        default:
                            {
                                //MessageBox.Show("Some other state...");
                                break;
                            }
                    }
                }
                //Update the current state for the next time they are checked.
                states[0].RdrCurrState = states[0].RdrEventState;
            }
        }
    }
    public SCARD_IO_REQUEST pioSendRequest;
    public nint pioRecvPci;
    private int SendAPDUandDisplay(int reqType)
    {
        uint indx;
        string tmpStr = "";

        pioSendRequest.dwProtocol = Protocol;
        pioSendRequest.cbPciLength = 8;

        //Display Apdu In
        for (indx = 0; indx <= SendLen - 1; indx++)
        {
            tmpStr = tmpStr + " " + string.Format("{0:X2}", SendBuff[indx]);
        }

        retCode = PCSCLite.SCardTransmit(hCard, ref pioSendRequest, SendBuff, SendLen, pioRecvPci, RecvBuff, ref RecvLen);

        if (retCode != PCSCLite.SCARD_S_SUCCESS)
        {
            return retCode;
        }

        else
        {
            try
            {
                tmpStr = "";
                switch (reqType)
                {
                    case 0:
                        for (indx = RecvLen - 2; indx <= RecvLen - 1; indx++)
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        if (tmpStr.Trim() != "90 00")
                        {
                            //MessageBox.Show("Return bytes are not acceptable.");
                            return -202;
                        }

                        break;

                    case 1:

                        for (indx = RecvLen - 2; indx <= RecvLen - 1; indx++)
                        {
                            tmpStr = tmpStr + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        if (tmpStr.Trim() != "90 00")
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        else
                        {
                            tmpStr = "ATR : ";
                            for (indx = 0; indx <= RecvLen - 3; indx++)
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                            }
                        }

                        break;

                    case 2:

                        for (indx = 0; indx <= RecvLen - 1; indx++)
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return -200;
            }
        }
        return retCode;
    }
    private void ClearBuffers()
    {
        long indx;

        for (indx = 0; indx <= 262; indx++)
        {
            RecvBuff[indx] = 0;
            SendBuff[indx] = 0;
        }
    }
    private bool AuthBlock(string block)
    {
        ClearBuffers();
        SendBuff[0] = 0xFF;                         // CLA
        SendBuff[2] = 0x00;                         // P1: same for all source types 
        SendBuff[1] = 0x86;                         // INS: for stored key input
        SendBuff[3] = 0x00;                         // P2 : Memory location;  P2: for stored key input
        SendBuff[4] = 0x05;                         // P3: for stored key input
        SendBuff[5] = 0x01;                         // Byte 1: version number
        SendBuff[6] = 0x00;                         // Byte 2
        SendBuff[7] = (byte)int.Parse(block);       // Byte 3: sectore no. for stored key input
        SendBuff[8] = 0x61;                         // Byte 4 : Key B for stored key input
        SendBuff[9] = (byte)int.Parse("1");         // Byte 5 : Session key for non-volatile memory

        SendLen = 0x0A;
        RecvLen = 0x02;

        retCode = SendAPDUandDisplay(0);

        if (retCode != PCSCLite.SCARD_S_SUCCESS)
        {
            //MessageBox.Show("FAIL Authentication! No:" + retCode.ToString());
            return false;
        }

        return true;
    }
    public string GetCardUID()
    {
        string cardUID = "";
        byte[] receivedUID = new byte[256];

        SCARD_IO_REQUEST request;
        if (Protocol == PCSCLite.SCARD_PROTOCOL_T0)
            request = PCSCLite.SCARD_PCI_T0();
        else if (Protocol == PCSCLite.SCARD_PROTOCOL_T1)
            request = PCSCLite.SCARD_PCI_T1();
        else
            request = PCSCLite.SCARD_PCI_RAW();

        byte[] sendBytes = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 };
        uint outBytes = (uint)receivedUID.Length;
        int status = PCSCLite.SCardTransmit(hCard, ref request, sendBytes, (uint)sendBytes.Length, pioRecvPci, receivedUID, ref outBytes);

        if (status != PCSCLite.SCARD_S_SUCCESS)
            cardUID = "";
        else
            cardUID = BitConverter.ToString(receivedUID.Take(4).ToArray()).Replace("-", string.Empty).ToLower();
        return cardUID;
    }

    public List<string> GetReadersList()
    {
        string ReaderList = "" + Convert.ToChar(0);
        int indx;
        uint pcchReaders = 0;
        string rName = "";
        List<string> lstReaders = new List<string>();
        //Establish Context
        retCode = PCSCLite.SCardEstablishContext(PCSCLite.SCARD_SCOPE_USER, 0, 0, out hContext);

        if (retCode != PCSCLite.SCARD_S_SUCCESS)
        {
            throw new Exception("Error SCardEstablishContext");
        }

        // 2. List PC/SC card readers installed in the system

        retCode = PCSCLite.SCardListReaders(hContext, null, null, ref pcchReaders);

        if (retCode != PCSCLite.SCARD_S_SUCCESS)
        {
            throw new Exception("Error SCardListReaders");
        }

        byte[] ReadersList = new byte[pcchReaders];

        // Fill reader list
        retCode = PCSCLite.SCardListReaders(hContext, null, ReadersList, ref pcchReaders);

        if (retCode != PCSCLite.SCARD_S_SUCCESS)
        {
            throw new Exception("Error SCardListReaders");
        }

        rName = "";
        indx = 0;


        while (ReadersList[indx] != 0)
        {

            while (ReadersList[indx] != 0)
            {
                rName += (char)ReadersList[indx];
                indx++;
            }


            lstReaders.Add(rName);
            rName = "";
            indx++;

        }
        return lstReaders;
    }
    /*
    public bool CleanCard(int maxblock)
    {
        int i = 0;
        string clean = "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
        while (i < maxblock)
        {
            WriteBlock(clean, (i + 4).ToString());
            i++;
        }
        return true;
    }
    */
    public bool WriteBlock(string Text, string Block)
    {

        char[] tmpStr = Text.ToArray();
        int indx;
        if (AuthBlock(Block))
        {
            ClearBuffers();
            SendBuff[0] = 0xFF;                             // CLA
            SendBuff[1] = 0xD6;                             // INS
            SendBuff[2] = 0x00;                             // P1
            SendBuff[3] = (byte)int.Parse(Block);           // P2 : Starting Block No.
            SendBuff[4] = (byte)int.Parse("16");            // P3 : Data length

            for (indx = 0; indx <= tmpStr.Length - 1; indx++)
            {
                SendBuff[indx + 5] = (byte)tmpStr[indx];
            }
            SendLen = (uint)SendBuff[4] + 5;
            RecvLen = 0x02;

            retCode = SendAPDUandDisplay(2);

            if (retCode != PCSCLite.SCARD_S_SUCCESS)
                return false;
            else
                return true;
        }
        else
            return false;
    }
    /*
    public string ReadString() 
    {
        int i = 0;
        string ret="";
        string tmpStr= String.Concat(ReadBlock((i + 4).ToString()));
        ret += String.Concat(tmpStr);
        i++;
        while (!(tmpStr.Contains("\0\0")))
        {
            tmpStr = String.Concat(ReadBlock((i + 4).ToString()));
            ret += tmpStr;
            i++;
        }
        return ret.Replace("\0", string.Empty);

    */
    /*
    public bool WriteString(String Text) 
    {
        string[] parts = Helper.SplitByLength(Text, 16).ToArray();
        double textlen = Text.Length, delim = 16;
        int blocklen = (int)Math.Ceiling(textlen / delim);
        for (int i = 0; i < blocklen; i++)
        {
            if(!WriteBlock(parts[i],(i+4).ToString()))return false;
        }
        return true;
    }
    */
    public byte[] ReadBlock(string Block)
    {
        byte[] tmpStr;
        int indx;

        if (AuthBlock(Block))
        {
            ClearBuffers();
            SendBuff[0] = 0xFF; // CLA 
            SendBuff[1] = 0xB0;// INS
            SendBuff[2] = 0x00;// P1
            SendBuff[3] = (byte)int.Parse(Block);// P2 : Block No.
            SendBuff[4] = (byte)int.Parse("16");// Le

            SendLen = 5;
            RecvLen = (uint)SendBuff[4] + 2;

            retCode = SendAPDUandDisplay(2);

            if (retCode == -200)
            {
                return new byte[] { };
            }

            if (retCode == -202)
            {
                return new byte[] { };
            }

            if (retCode != PCSCLite.SCARD_S_SUCCESS)
            {
                return new byte[] { };
            }

            // Display data in text format
            List<byte> t = new List<byte>();
            for (indx = 0; indx <= RecvLen - 1; indx++)
            {
                t.Add(RecvBuff[indx]);
            }
            tmpStr = t.ToArray();
            return tmpStr;
        }
        else return new byte[] { };
    }
    public bool Connect()
    {
        string readerName = _readername;
        connActive = true;
        retCode = PCSCLite.SCardConnect(hContext, readerName, PCSCLite.SCARD_SHARE_SHARED, PCSCLite.SCARD_PROTOCOL_ANY, out hCard, out Protocol);
        if (retCode != PCSCLite.SCARD_S_SUCCESS)
        {
            connActive = false;
            return false;
        }
        else
            return true;
    }
    public void Disconnect()
    {
        if (connActive)
        {
            retCode = PCSCLite.SCardDisconnect(hCard, PCSCLite.SCARD_UNPOWER_CARD);
        }
        //retCode = SCardReleaseContext(hCard);
    }
    public string Transmit(byte[] buff)
    {
        string tmpStr = "";
        int indx;

        ClearBuffers();

        for (int i = 0; i < buff.Length; i++)
        {
            SendBuff[i] = buff[i];
        }
        SendLen = 5;
        RecvLen = (uint)SendBuff[SendBuff.Length - 1] + 2;

        retCode = SendAPDUandDisplay(2);


        // Display data in text format
        for (indx = 0; indx <= RecvLen - 1; indx++)
        {
            tmpStr = tmpStr + Convert.ToChar(RecvBuff[indx]);
        }

        return tmpStr;
    }
    public void Watch()
    {
        RdrState = new SCARD_READERSTATE();
        RdrState.RdrName = _readername;

        states = new SCARD_READERSTATE[1];
        states[0] = new SCARD_READERSTATE();
        states[0].RdrName = _readername;
        states[0].UserData = 0;
        states[0].RdrCurrState = PCSCLite.SCARD_STATE_EMPTY;
        states[0].RdrEventState = 0;
        states[0].ATRLength = 0;
        states[0].ATRValue = null;
        _worker = new BackgroundWorker();
        _worker.WorkerSupportsCancellation = true;
        _worker.DoWork += WaitChangeStatus;
        _worker.RunWorkerAsync();
    }

    public void SetReader(string readername)
    {
        _readername = readername;
    }

    public NFCReader()
    {
    }
}

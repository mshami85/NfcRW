using SnappyWinscard;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NfcRW;

internal class SCManager
{
    public event EventHandler<CardInsertedEventArgs>? CardInserted;
    public event EventHandler? CardEjected;
    public event EventHandler? Disconnected;

    internal enum SmartcardState
    {
        None = 0,
        Inserted = 1,
        Ejected = 2
    }

    //private uint currentState;
    private uint retCode;

    private nint hContext;
    private nint hCard;
    private nint protocol;

    private string? currentDevice;

    private BackgroundWorker _worker;

    private Winscard.SCARD_IO_REQUEST pioSendRequest;
    private Winscard.SCARD_READERSTATE rdrState;
    private Winscard.SCARD_READERSTATE[] states;

    private int swInt;
    private const int SwOk = 0x9000;
    private const int SwUnknown = -1;
    private const int SwNoContext = -2;

    private bool initilized = false;

    public SCManager(string readerName)
    {
        currentDevice = readerName;
        Initialize();
    }

    public static List<string> ListReaders()
    {
        nint hContext = 0;
        nint ReaderCount = 0;
        List<string> availableReaderList = [];

        //establishing context 
        var retCode = Winscard.SCardEstablishContext(Winscard.SCARD_SCOPE_USER, 0, 0, ref hContext);
        if (retCode != Winscard.SCARD_S_SUCCESS)
        {
            throw new Exception("Error SCardEstablishContext");
        }

        //Make sure a context has been established before
        //retrieving the list of smartcard readers.
        retCode = Winscard.SCardListReaders(hContext, null, null, ref ReaderCount);
        if (retCode != Winscard.SCARD_S_SUCCESS)
        {
            return [];
        }

        byte[] ReadersList = new byte[ReaderCount];

        //Get the list of reader present again but this time add sReaderGroup, retData as 2rd & 3rd parameter respectively.
        retCode = Winscard.SCardListReaders(hContext, null, ReadersList, ref ReaderCount);
        if (retCode != Winscard.SCARD_S_SUCCESS)
        {
            return [];
        }

        string rName = "";
        int indx = 0;
        if (ReaderCount <= 0)
        {
            return [];
        }
        // Convert reader buffer to string
        while (ReadersList[indx] != 0)
        {

            while (ReadersList[indx] != 0)
            {
                rName += (char)ReadersList[indx];
                indx += 1;
            }

            //Add reader name to list
            availableReaderList.Add(rName);
            rName = "";
            indx += 1;
        }
        return availableReaderList;

    }

    public bool ConnectCard()
    {
        if (currentDevice == null)
        {
            return false;
        }
        retCode = Winscard.SCardConnect(hContext, currentDevice, Winscard.SCARD_SHARE_SHARED,
                  Winscard.SCARD_PROTOCOL_T0 | Winscard.SCARD_PROTOCOL_T1, ref hCard, ref protocol);

        swInt = 0;

        return retCode == Winscard.SCARD_S_SUCCESS;
    }

    public string? GetCardUID()//only for mifare 1k cards
    {
        if (ConnectCard())
        {
            string cardUID = "";
            byte[] receivedUID = new byte[256];
            Winscard.SCARD_IO_REQUEST request = new()
            {
                dwProtocol = Winscard.SCARD_PROTOCOL_T1,
                cbPciLength = Marshal.SizeOf(typeof(Winscard.SCARD_IO_REQUEST))
            };
            byte[] sendBytes = [0xFF, 0xCA, 0x00, 0x00, 0x00]; //get UID command      for Mifare cards
            nint outBytes = receivedUID.Length;
            if (SCardTransmit(sendBytes, receivedUID, ref request, ref outBytes))
            {
                cardUID = receivedUID.Take(4)
                                     .Aggregate("", (a, b) => a += b.ToString("x2"));
            }
            else
            {
                cardUID = "Error";
            }

            return cardUID;
        }
        return null;
    }

    public string StatusText => Winscard.GetScardErrMsg(retCode);

    public string SubStatusText
    {
        get
        {
            switch (swInt)
            {
                case 0x9000:
                    return "Success";
                case 0x6300:
                    return "Failed";
                case 0x6a81:
                    return "Not supported";
                default:
                    return "Unexpected";
            }
        }
    }

    public void SetDevice(string device)
    {
        List<string> list = ListReaders();
        if (string.IsNullOrWhiteSpace(device) || !list.Contains(device))
        {
            throw new Exception("Wrong device");
        }
        currentDevice = device;
        Initialize();
    }

    public void StoreKey(byte[] key, byte keySlot)
    {
        const byte keyLength = 6;
        if (key.Length != keyLength)
            throw new ArgumentException("Key must be 6 bytes long", nameof(key));
        var SendLen = 5 + keyLength;
        byte[] SendBuff = new byte[SendLen];
        SendBuff[0] = 0xFF;                             // CLA
        SendBuff[1] = 0x82;                             // INS
        SendBuff[2] = 0x00;                             // P1: Key Structure - to memory
        SendBuff[3] = keySlot;                          // P2: Key slot number
        SendBuff[4] = keyLength;                             // Lc: Data length
        key.CopyTo(SendBuff, 5);

        nint RecvLen = 2;
        byte[] RecvBuff = new byte[RecvLen];

        SCardTransmit(SendBuff, RecvBuff, ref pioSendRequest, ref RecvLen);
    }

    public bool AuthenticateBlock(byte Block, byte KeyType, byte KeySlot)
    {
        byte[] SendBuff = new byte[] {
                0xFF,        // CLA
                0x86,        // INS: Authentication
                0x00,        // P1 
                0x00,        // P2: Memory location,  P2: for stored key input
                0x05,        // Lc
                                           // Authenticate Data Bytes
                0x01,        // Byte 1: Version
                0x00,        // Byte 2
                Block,       // Byte 3: Block number
                KeyType,     // Byte 4: Key type
                KeySlot,     // Byte 5: Key number
            };

        byte[] RecvBuff = new byte[2];

        nint RecvLen = RecvBuff.Length;

        return SCardTransmit(SendBuff, RecvBuff, ref pioSendRequest, ref RecvLen);
    }

    public byte[]? ReadCardBlock(byte block, byte keyType, byte keySlot)
    {
        if (AuthenticateBlock(block, keyType, keySlot))
        {
            return ReadBlock(block);
        }
        return null;
    }

    public void WriteCardBlock(byte[] data, byte block, byte keyType, byte keySlot)
    {

        if (data == null)
            return;
        if (AuthenticateBlock(block, keyType, keySlot))
        {
            var SendLen = 5 + (byte)data.Length;
            byte[] SendBuff = new byte[SendLen];
            SendBuff[0] = 0xFF;                             // Class
            SendBuff[1] = 0xD6;                             // INS
            SendBuff[2] = 0x00;                             // P1
            SendBuff[3] = block;           // P2 : Starting Block No.
            SendBuff[4] = (byte)data.Length;            // P3 : Data length

            data.CopyTo(SendBuff, 5);

            nint RecvLen = 0x02;
            byte[] RecvBuff = new byte[RecvLen];
            SCardTransmit(SendBuff, RecvBuff, ref pioSendRequest, ref RecvLen);
        }
    }

    public void Disconnect()
    {
        if (initilized)
        {
            try
            {
                _worker.CancelAsync();
                _worker.DoWork -= WatchChangeStatus;
            }
            catch { }
        }
        retCode = Winscard.SCardDisconnect(hCard, Winscard.SCARD_UNPOWER_CARD);
        initilized = false;
    }










    private byte[]? ReadBlock(byte block)
    {
        const byte blockSize = 16;
        byte[] SendBuff =
        [
            0xFF, // CLA 
            0xB0,// INS
            0x00,// P1
            block,// P2 : Block No.
            blockSize,// Le
        ];
        nint RecvLen = blockSize + 2;
        byte[] RecvBuff = new byte[RecvLen];

        if (SCardTransmit(SendBuff, RecvBuff, ref pioSendRequest, ref RecvLen))
        {
            if (swInt == SwOk)
            {
                return RecvBuff.Take((int)(RecvLen - 2)).ToArray();
            }
        }
        return null;
    }

    private bool SCardTransmit(byte[] SendBuff, byte[] RecvBuff, ref Winscard.SCARD_IO_REQUEST pioSendRequest, ref nint RecvLen)
    {
        retCode = Winscard.SCardTransmit(hCard, ref pioSendRequest, SendBuff, SendBuff.Length, ref pioSendRequest, RecvBuff, ref RecvLen);
        ReadSw(RecvBuff, (int)(RecvLen - 2));
        return retCode == Winscard.SCARD_S_SUCCESS;
    }

    private void ReadSw(byte[] buff, int i)
    {
        if (buff.Length - i < 2)
        {
            swInt = SwUnknown;
        }
        else
        {
            swInt = buff[i] * 0x100 + buff[i + 1];
        }
    }

    private void Initialize()
    {
        // Connect
        pioSendRequest.dwProtocol = 0;
        pioSendRequest.cbPciLength = 8;
        retCode = Winscard.SCardEstablishContext(Winscard.SCARD_SCOPE_SYSTEM, 0, 0, ref hContext);
        if (retCode != Winscard.SCARD_S_SUCCESS)
        {
            swInt = SwNoContext;
        }

        states = new Winscard.SCARD_READERSTATE[1];
        states[0] = new Winscard.SCARD_READERSTATE
        {
            RdrName = currentDevice,
            UserData = 0,
            RdrCurrState = Winscard.SCARD_STATE_EMPTY,
            RdrEventState = 0,
            ATRLength = 0,
            ATRValue = null
        };
        if (!initilized)
        {
            _worker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            _worker.DoWork += WatchChangeStatus; ;
            _worker.RunWorkerAsync();
        }
        initilized = true;
    }

    private void WatchChangeStatus(object? sender, DoWorkEventArgs e)
    {
        while (!e.Cancel)
        {
            retCode = Winscard.SCardGetStatusChange(hContext, 1000, states, 1);

            if (retCode == Winscard.SCARD_E_SERVICE_STOPPED)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
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
                                CardInserted?.Invoke(this, new CardInsertedEventArgs
                                {
                                    CardUID = GetCardUID()
                                });
                                break;
                            }
                        case SmartcardState.Ejected:
                            {
                                //MessageBox.Show("Card ejected");
                                CardEjected?.Invoke(this, EventArgs.Empty);
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
}

public class CardInsertedEventArgs : EventArgs
{
    public string? CardUID { get; set; }
}

public enum KeyType : byte
{
    TypeA = 0x60,
    TypeB = 0x61
}

public enum KeySlot : byte
{
    Slot0 = 0x00,
    Slot1 = 0x01,
}

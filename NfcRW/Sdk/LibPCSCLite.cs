using System.Runtime.InteropServices;

namespace NfcRW.Sdk;

[StructLayout(LayoutKind.Sequential)]
public struct SCARD_IO_REQUEST
{
    public uint dwProtocol;
    public uint cbPciLength;

    public SCARD_IO_REQUEST(uint pciLen, uint protocol)
    {
        dwProtocol = protocol;
        cbPciLength = pciLen;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct SCARD_READERSTATE
{
    public string RdrName;
    public uint UserData;
    public uint RdrCurrState;
    public uint RdrEventState;
    public uint ATRLength;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 37)]
    public byte[] ATRValue;
}

public sealed class PCSCLite
{
    public const string WINSCARD = "winscard.dll";
    /*===========================================================
    '   Error Codes
    '===========================================================*/
    public const int SCARD_F_INTERNAL_ERROR = -2146435071;
    public const int SCARD_E_CANCELLED = -2146435070;
    public const int SCARD_E_INVALID_HANDLE = -2146435069;
    public const int SCARD_E_INVALID_PARAMETER = -2146435068;
    public const int SCARD_E_INVALID_TARGET = -2146435067;
    public const int SCARD_E_NO_MEMORY = -2146435066;
    public const int SCARD_F_WAITED_TOO_LONG = -2146435065;
    public const int SCARD_E_INSUFFICIENT_BUFFER = -2146435064;
    public const int SCARD_E_UNKNOWN_READER = -2146435063;


    public const int SCARD_E_TIMEOUT = -2146435062;
    public const int SCARD_E_SHARING_VIOLATION = -2146435061;
    public const int SCARD_E_NO_SMARTCARD = -2146435060;
    public const int SCARD_E_UNKNOWN_CARD = -2146435059;
    public const int SCARD_E_CANT_DISPOSE = -2146435058;
    public const int SCARD_E_PROTO_MISMATCH = -2146435057;


    public const int SCARD_E_NOT_READY = -2146435056;
    public const int SCARD_E_INVALID_VALUE = -2146435055;
    public const int SCARD_E_SYSTEM_CANCELLED = -2146435054;
    public const int SCARD_F_COMM_ERROR = -2146435053;
    public const int SCARD_F_UNKNOWN_ERROR = -2146435052;
    public const int SCARD_E_INVALID_ATR = -2146435051;
    public const int SCARD_E_NOT_TRANSACTED = -2146435050;
    public const int SCARD_E_READER_UNAVAILABLE = -2146435049;
    public const int SCARD_P_SHUTDOWN = -2146435048;
    public const int SCARD_E_PCI_TOO_SMALL = -2146435047;

    public const int SCARD_E_READER_UNSUPPORTED = -2146435046;
    public const int SCARD_E_DUPLICATE_READER = -2146435045;
    public const int SCARD_E_CARD_UNSUPPORTED = -2146435044;
    public const int SCARD_E_NO_SERVICE = -2146435043;
    public const int SCARD_E_SERVICE_STOPPED = -2146435042;

    public const int SCARD_W_UNSUPPORTED_CARD = -2146435041;
    public const int SCARD_W_UNRESPONSIVE_CARD = -2146435040;
    public const int SCARD_W_UNPOWERED_CARD = -2146435039;
    public const int SCARD_W_RESET_CARD = -2146435038;
    public const int SCARD_W_REMOVED_CARD = -2146435037;

    public static string GetScardErrMsg(int ReturnCode)
    {
        switch (ReturnCode)
        {
            case SCARD_E_CANCELLED:
                return ("The action was canceled by an SCardCancel request.");
            case SCARD_E_CANT_DISPOSE:
                return ("The system could not dispose of the media in the requested manner.");
            case SCARD_E_CARD_UNSUPPORTED:
                return ("The smart card does not meet minimal requirements for support.");
            case SCARD_E_DUPLICATE_READER:
                return ("The reader driver didn't produce a unique reader name.");
            case SCARD_E_INSUFFICIENT_BUFFER:
                return ("The data buffer for returned data is too small for the returned data.");
            case SCARD_E_INVALID_ATR:
                return ("An ATR string obtained from the registry is not a valid ATR string.");
            case SCARD_E_INVALID_HANDLE:
                return ("The supplied handle was invalid.");
            case SCARD_E_INVALID_PARAMETER:
                return ("One or more of the supplied parameters could not be properly interpreted.");
            case SCARD_E_INVALID_TARGET:
                return ("Registry startup information is missing or invalid.");
            case SCARD_E_INVALID_VALUE:
                return ("One or more of the supplied parameter values could not be properly interpreted.");
            case SCARD_E_NOT_READY:
                return ("The reader or card is not ready to accept commands.");
            case SCARD_E_NOT_TRANSACTED:
                return ("An attempt was made to end a non-existent transaction.");
            case SCARD_E_NO_MEMORY:
                return ("Not enough memory available to complete this command.");
            case SCARD_E_NO_SERVICE:
                return ("The smart card resource manager is not running.");
            case SCARD_E_NO_SMARTCARD:
                return ("The operation requires a smart card, but no smart card is currently in the device.");
            case SCARD_E_PCI_TOO_SMALL:
                return ("The PCI receive buffer was too small.");
            case SCARD_E_PROTO_MISMATCH:
                return ("The requested protocols are incompatible with the protocol currently in use with the card.");
            case SCARD_E_READER_UNAVAILABLE:
                return ("The specified reader is not currently available for use.");
            case SCARD_E_READER_UNSUPPORTED:
                return ("The reader driver does not meet minimal requirements for support.");
            case SCARD_E_SERVICE_STOPPED:
                return ("The smart card resource manager has shut down.");
            case SCARD_E_SHARING_VIOLATION:
                return ("The smart card cannot be accessed because of other outstanding connections.");
            case SCARD_E_SYSTEM_CANCELLED:
                return ("The action was canceled by the system, presumably to log off or shut down.");
            case SCARD_E_TIMEOUT:
                return ("The user-specified timeout value has expired.");
            case SCARD_E_UNKNOWN_CARD:
                return ("The specified smart card name is not recognized.");
            case SCARD_E_UNKNOWN_READER:
                return ("The specified reader name is not recognized.");
            case SCARD_F_COMM_ERROR:
                return ("An internal communications error has been detected.");
            case SCARD_F_INTERNAL_ERROR:
                return ("An internal consistency check failed.");
            case SCARD_F_UNKNOWN_ERROR:
                return ("An internal error has been detected, but the source is unknown.");
            case SCARD_F_WAITED_TOO_LONG:
                return ("An internal consistency timer has expired.");
            case SCARD_S_SUCCESS:
                return ("No error was encountered.");
            case SCARD_W_REMOVED_CARD:
                return ("The smart card has been removed, so that further communication is not possible.");
            case SCARD_W_RESET_CARD:
                return ("The smart card has been reset, so any shared state information is invalid.");
            case SCARD_W_UNPOWERED_CARD:
                return ("Power has been removed from the smart card, so that further communication is not possible.");
            case SCARD_W_UNRESPONSIVE_CARD:
                return ("The smart card is not responding to a reset.");
            case SCARD_W_UNSUPPORTED_CARD:
                return ("The reader cannot communicate with the card, due to ATR string configuration conflicts.");
            default:
                return ("?");
        }
    }

    /*===========================================================
    '   STATE
    '===========================================================*/
    public const uint SCARD_UNKNOWN = 0x0001;
    public const uint SCARD_ABSENT = 0x0002;
    public const uint SCARD_PRESENT = 0x0004;
    public const uint SCARD_SWALLOWED = 0x0008;
    public const uint SCARD_POWERED = 0x0010;
    public const uint SCARD_NEGOTIABLE = 0x0020;
    public const uint SCARD_SPECIFIC = 0x0040;
    public const uint SCARD_STATE_UNAWARE = 0x0000;
    public const uint SCARD_STATE_IGNORE = 0x0001;
    public const uint SCARD_STATE_CHANGED = 0x0002;
    public const uint SCARD_STATE_UNKNOWN = 0x0004;
    public const uint SCARD_STATE_UNAVAILABLE = 0x0008;
    public const uint SCARD_STATE_EMPTY = 0x0010;
    public const uint SCARD_STATE_PRESENT = 0x0020;
    public const uint SCARD_STATE_EXCLUSIVE = 0x0080;
    public const uint SCARD_STATE_INUSE = 0x0100;
    public const uint SCARD_STATE_MUSE = 0x0200;

    /*===========================================================
    '   SHARE MODE
    '===========================================================*/
    public const uint SCARD_SHARE_EXCLUSIVE = 0x0001;
    public const uint SCARD_SHARE_SHARED = 0x0002;
    public const uint SCARD_SHARE_DIRECT = 0x0003;

    /*===========================================================
    '   PROTOCOL
    '===========================================================*/
    public const uint SCARD_PROTOCOL_UNDEFINED = 0x0000;
    public const uint SCARD_PROTOCOL_T0 = 0x0001;
    public const uint SCARD_PROTOCOL_T1 = 0x0002;
    public const uint SCARD_PROTOCOL_RAW = 0x0004;
    public const uint SCARD_PROTOCOL_T15 = 0x0008;
    public const uint SCARD_PROTOCOL_ANY = SCARD_PROTOCOL_T0 | SCARD_PROTOCOL_T1;

    /*===========================================================
    '   SCOPE
    '===========================================================*/
    public const uint SCARD_SCOPE_USER = 0x0000;
    public const uint SCARD_SCOPE_TERMINAL = 0x0001;
    public const uint SCARD_SCOPE_SYSTEM = 0x0002;
    public const uint SCARD_SCOPE_GLOBAL = 0x0003;

    /*===========================================================
    '   DISPOSITION
    '===========================================================*/
    public const uint SCARD_LEAVE_CARD = 0x0000;
    public const uint SCARD_RESET_CARD = 0x0001;
    public const uint SCARD_UNPOWER_CARD = 0x0002;
    public const uint SCARD_EJECT_CARD = 0x0003;

    /*===========================================================
    '   ERROR CODE
    '===========================================================*/
    public const int SCARD_S_SUCCESS = 0;


    public static SCARD_IO_REQUEST SCARD_PCI_T0()
    {
        return new SCARD_IO_REQUEST(SCARD_PROTOCOL_T0, (uint)Marshal.SizeOf(typeof(SCARD_IO_REQUEST)));
    }

    public static SCARD_IO_REQUEST SCARD_PCI_T1()
    {
        return new SCARD_IO_REQUEST(SCARD_PROTOCOL_T1, (uint)Marshal.SizeOf(typeof(SCARD_IO_REQUEST)));
    }

    public static SCARD_IO_REQUEST SCARD_PCI_RAW()
    {
        return new SCARD_IO_REQUEST(SCARD_PROTOCOL_RAW, (uint)Marshal.SizeOf(typeof(SCARD_IO_REQUEST)));
    }

    /// <summary>
    /// Creates an Application Context to the PC/SC Resource Manager.
    /// </summary>
    /// <param name="dwScope"></param>
    /// <param name="pvReserved1"></param>
    /// <param name="pvReserved2"></param>
    /// <param name="phContext"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardEstablishContext(
        [In] uint dwScope,
        [In] nint pvReserved1,
        [In] nint pvReserved2,
        [Out] out nint phContext);

    /// <summary>
    /// Destroys a communication context to the PC/SC Resource Manager.
    /// </summary>
    /// <param name="hContext"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardReleaseContext(
        [In] nint hContext);

    /// <summary>
    /// Establishes a connection to the reader specified in * szReader.
    /// </summary>
    /// <param name="hContext"></param>
    /// <param name="szReader"></param>
    /// <param name="dwShareMode"></param>
    /// <param name="dwPreferredProtocols"></param>
    /// <param name="phCard"></param>
    /// <param name="pdwActiveProtocol"></param>
    /// <returns></returns>
    [DllImport(WINSCARD, CharSet = CharSet.Ansi)]
    public static extern int SCardConnect(
        [In] nint hContext,
        [In] string szReader,
        [In] uint dwShareMode,
        [In] uint dwPreferredProtocols,
        [Out] out nint phCard,
        [Out] out uint pdwActiveProtocol);

    /// <summary>
    /// Reestablishes a connection to a reader that was previously connected to using SCardConnect().
    /// </summary>
    /// <param name="hCard"></param>
    /// <param name="dwShareMode"></param>
    /// <param name="dwPreferredProtocols"></param>
    /// <param name="dwInitialization"></param>
    /// <param name="pdActiveProtocol"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardReconnect(
        [In] nint hCard,
        [In] uint dwShareMode,
        [In] uint dwPreferredProtocols,
        [In] uint dwInitialization,
        [Out] out uint pdActiveProtocol);

    /// <summary>
    /// Terminates a connection made through SCardConnect().
    /// </summary>
    /// <param name="hCard"></param>
    /// <param name="dwDisposition"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardDisconnect(
        [In] nint hCard,
        [In] uint dwDisposition);

    /// <summary>
    /// Establishes a temporary exclusive access mode for doing a serie of commands in a transaction.
    /// </summary>
    /// <param name="hCard"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardBeginTransaction(
        [In] nint hCard);

    /// <summary>
    /// Ends a previously begun transaction.
    /// </summary>
    /// <param name="hCard"></param>
    /// <param name="dwDisposition"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardEndTransaction(
        [In] nint hCard,
        [In] uint dwDisposition);

    /// <summary>
    /// Returns the current status of the reader connected to by hCard.
    /// </summary>
    /// <param name="hCard"></param>
    /// <param name="szReaderName"></param>
    /// <param name="pcchReaderLen"></param>
    /// <param name="pdwState">Current state of this reader</param>
    /// <param name="pdwProtocol">	Current protocol of this reader</param>
    /// <param name="pbAtr"></param>
    /// <param name="pcbAtrLen"></param>
    /// <returns></returns>
    [DllImport(WINSCARD, CharSet = CharSet.Ansi)]
    public static extern int SCardStatus(
        [In] nint hCard,
        [In, Out] string szReaderName,
        [In, Out] ref uint pcchReaderLen,
        [Out] out uint pdwState,
        [Out] out uint pdwProtocol,
        [Out] byte pbAtr,
        [Out] out uint pcbAtrLen);

    /// <summary>
    /// Blocks execution until the current availability of the cards in a specific set of readers changes. 
    /// </summary>
    /// <param name="hContext"></param>
    /// <param name="dwTimeout"></param>
    /// <param name="rgReaderStates"></param>
    /// <param name="cReaders"></param>
    /// <returns></returns>
    [DllImport(WINSCARD, CharSet = CharSet.Ansi)]
    public static extern int SCardGetStatusChange(
        [In] nint hContext,
        [In] uint dwTimeout,
        [In, Out] SCARD_READERSTATE[] rgReaderStates,
        [In] uint cReaders);

    /// <summary>
    /// Sends a command directly to the IFD Handler (reader driver) to be processed by the reader. 
    /// </summary>
    /// <param name="hCard"></param>
    /// <param name="dwControlCode"></param>
    /// <param name="pbSendBuffer"></param>
    /// <param name="cbSendLength"></param>
    /// <param name="pbRecvBuffer"></param>
    /// <param name="cbRecvLength"></param>
    /// <param name="lpBytesReturned"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardControl(
        [In] nint hCard,
        [In] uint dwControlCode,
        [In] byte[] pbSendBuffer,
        [In] uint cbSendLength,
        [Out] byte[] pbRecvBuffer,
        [In] uint cbRecvLength,
        [Out] out uint lpBytesReturned);

    /// <summary>
    /// Get an attribute from the IFD Handler (reader driver). 
    /// </summary>
    /// <param name="hCard"></param>
    /// <param name="dwAttrId"></param>
    /// <param name="pbAttr"></param>
    /// <param name="pcbAttrLen"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardGetAttrib(
        [In] nint hCard,
        [In] uint dwAttrId,
        [Out] byte[] pbAttr,
        [In, Out] ref uint pcbAttrLen);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hCard"></param>
    /// <param name="dwAttrId"></param>
    /// <param name="pbAttr"></param>
    /// <param name="cbAttrLen"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardSetAttrib(
        [In] nint hCard,
        [In] uint dwAttrId,
        [In] byte[] pbAttr,
        [In] uint cbAttrLen);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hCard"></param>
    /// <param name="pioSendPci"></param>
    /// <param name="pbSendBuffer"></param>
    /// <param name="cbSendLength"></param>
    /// <param name="pioRecvPci"></param>
    /// <param name="pbRecvBuffer"></param>
    /// <param name="pcbRecvLength"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardTransmit(
        [In] nint hCard,
        [In] ref SCARD_IO_REQUEST pioSendPci,
        [In] byte[] pbSendBuffer,
        [In] uint cbSendLength,
        [In, Out] ref SCARD_IO_REQUEST pioRecvPci,
        [Out] byte[] pbRecvBuffer,
        [In, Out] ref uint pcbRecvLength);

    [DllImport(WINSCARD)]
    public static extern int SCardTransmit(
        [In] nint hCard,
        [In] ref SCARD_IO_REQUEST pioSendPci,
        [In] byte[] pbSendBuffer,
        [In] uint cbSendLength,
        [In, Out] nint pioRecvPci,
        [Out] byte[] pbRecvBuffer,
        [In, Out] ref uint pcbRecvLength);

    [DllImport(WINSCARD)]
    public static extern int SCardTransmit(
        [In] nint hCard,
        [In] nint pioSendPci,
        [In] byte[] pbSendBuffer,
        [In] uint cbSendLength,
        [In, Out] nint pioRecvPci,
        [Out] byte[] pbRecvBuffer,
        [In, Out] ref uint pcbRecvLength);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hContext"></param>
    /// <param name="mszGroups"></param>
    /// <param name="pmszReaders"></param>
    /// <param name="pcchReaders"></param>
    /// <returns></returns>
    [DllImport(WINSCARD, CharSet = CharSet.Ansi)]
    public static extern int SCardListReaders(
         [In] nint hContext,
         [In] byte[] mszGroups,
         [Out] byte[] pmszReaders,
         [In, Out] ref uint pcchReaders);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hContext"></param>
    /// <param name="pvMem"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardFreeMemory(
        [In] nint hContext,
        [In] nint pvMem);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hContext"></param>
    /// <param name="szGroups"></param>
    /// <param name="pcchGroups"></param>
    /// <returns></returns>
    [DllImport(WINSCARD, CharSet = CharSet.Ansi)]
    public static extern int SCardListReaderGroups(
        [In] nint hContext,
        [Out] byte[] szGroups,
        [In, Out] ref uint pcchGroups);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hContext"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardCancel(
        [In] nint hContext);

    /// <summary>
    /// Check if a SCARDCONTEXT is valid.
    /// </summary>
    /// <param name="hContext"></param>
    /// <returns></returns>
    [DllImport(WINSCARD)]
    public static extern int SCardIsValidContext(
        [In] nint hContext);
}
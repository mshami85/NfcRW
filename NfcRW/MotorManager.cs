using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
//using System.Windows.Forms;

namespace ProductBinder.Motor
{
    internal class MotorManager
    {
        public event EventHandler<MotorEventArgs> MotorChanged;

        private const int CMD_RUN_STEP = 1;
        private const int CMD_RUN_FREE = 2;
        private const int CMD_STOP = 3;

        private SerialPort _port;
        private volatile bool _dataRecieved;
        StringBuilder _logger = new StringBuilder();
        public string LastError { get; private set; }
        private readonly string _portName;

        public MotorManager(string port)
        {
            _portName = port;
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _dataRecieved = true;
            int recieved;
            try
            {
                while (_port.BytesToRead > 0)
                {
                    recieved = _port.ReadByte();
                    if (recieved == -1)
                        return;
                    var code = ResolveCode(recieved);
                    var cmd = ResolveCommand(recieved);
                    LogInfo($"Recieving: command ({cmd}), code ({code}) {(code == 0 ? "Success" : "Fail")}");
                    MotorChanged?.Invoke(this, new MotorEventArgs { Command = cmd, ErrorCode = code });
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void Connect()
        {
            try
            {
                if (_port == null)
                {
                    _port = new SerialPort(_portName);
                    _port.BaudRate = 9600;
                    _port.Handshake = Handshake.None;
                    _port.Parity = Parity.None;
                    _port.ReceivedBytesThreshold = 1;
                    _port.DataBits = 8;
                    _port.DiscardNull = true;
                    _port.DataReceived += DataReceived;
                }
                _port.Open();
                LogInfo("Connected");
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void Disconnect()
        {
            try
            {
                _port?.DiscardInBuffer();
                _port?.Close();
                _port = null;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            LogInfo("Disconnected");
        }

        /// <summary>
        /// Start running motor, with specific steps
        /// </summary>
        /// <param name="steps">steps to move</param>
        /// <returns>Is command completed successfuly</returns>
        public Task<bool> RunSteps(int steps)
        {
            var sendByte = ToBinary(CMD_RUN_STEP, steps);
            LogInfo($"Sending: {nameof(CMD_RUN_STEP)} with {steps}");
            return SendCommand(sendByte);
        }

        /// <summary>
        /// Running motor for about 5 seconds, unless stop is sent
        /// </summary>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        public Task<bool> RunFree()
        {
            var sendByte = ToBinary(CMD_RUN_FREE);
            LogInfo($"Sending: {nameof(CMD_RUN_FREE)} ");
            return SendCommand(sendByte);
        }

        /// <summary>
        /// stops free run
        /// </summary>
        /// <returns></returns>
        public Task<bool> Stop()
        {
            var sendByte = ToBinary(CMD_STOP);
            LogInfo($"Sending: {nameof(CMD_STOP)} ");
            return SendCommand(sendByte);
        }

        #region << Commands >>

        private async Task<bool> SendCommand(byte cmdByte)
        {
            _dataRecieved = false;
            try
            {
                if (!_port.IsOpen)
                {
                    _port.Open();
                }
                var sendByte = cmdByte;
                var readyToSend = new[] {
                    sendByte
                };

                _port.Write(readyToSend, 0, readyToSend.Length);
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var success = await Task.Run(() =>
                {
                    while (!_dataRecieved && !cts.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }
                    return true;
                }, cts.Token);
                return success && !cts.IsCancellationRequested;
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        private static int ResolveCode(int recieved)
        {
            var result = recieved & 0x3F;
            return result;
        }

        private static int ResolveCommand(int recieved)
        {
            var command = recieved >> 6;
            return command;
        }

        private static byte ToBinary(int command, int parameter = 0)
        {
            var cmd = Convert.ToString(command, 2).PadLeft(2, '0');
            var param = Convert.ToString(parameter, 2).PadLeft(6, '0');
            var result = Convert.ToByte(cmd + param, 2);
            return result;
        }
        #endregion

        #region <<Log>>
        public string GetLog()
        {
            return _logger.ToString();
        }

        public void ClearLog()
        {
            _logger.Clear();
            LastError = string.Empty;
        }

        void LogException(Exception ex)
        {
            LastError = $"[EXCEPTION]: {DateTime.Now} - {ex.Message}";
            _logger.AppendLine(LastError);
        }

        void LogError(string message)
        {
            LastError = $"[ERROR]: {DateTime.Now} - {message}";
            _logger.AppendLine(LastError);
        }

        void LogInfo(string message)
        {
            LastError = $"[INFO]: {DateTime.Now} - {message}";
            _logger.AppendLine(LastError);
        }
        #endregion
    }

    public class MotorEventArgs : EventArgs
    {
        public int ErrorCode { get; set; }
        public int Command { get; set; }

        public override string ToString()
        {
            return $"Command {Command} returns code {ErrorCode}.";
        }
    }
}

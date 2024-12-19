using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Hidwizards.IOWrapper.Libraries.ProviderLogger;

namespace Np_Provider
{
    public class NamedPipeHandler : IDisposable
    {
        private readonly static string NAMED_PIPE_PREFIX = "ucrNamedPipeInputHandler";
        private String _pipeName;
        private PipeDirection _direction;
        private NamedPipeServerStream _pipeServer;

        private readonly Logger _logger;

        private const int BufferSize = 2048;

        public Queue<InputCommand> Commands { get; private set; }

        private bool _isStopping = false;
        private object _lockingObject = new object();

        private class Info
        {
            public readonly byte[] Buffer;
            public readonly StringBuilder StringBuilder;

            public Info()
            {
                Buffer = new byte[BufferSize];
                StringBuilder = new StringBuilder();
            }
        }

        public NamedPipeHandler(int pipeNumber, PipeDirection direction, Logger logger)
        {
            if (pipeNumber < 0 || pipeNumber > 4)
                throw new ArgumentOutOfRangeException("Pipe Number must be between 0 and 3");

            _pipeName = NAMED_PIPE_PREFIX + pipeNumber;
            _direction = direction;
            _logger = logger;

            Commands = new Queue<InputCommand>();
        }

        public void Start()
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                _isStopping = false;
                _pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, null);
            }
            catch (Exception ex)
            {
                _logger.Log("Error starting server.");
                throw;
            }
        }

        private void WaitForConnectionCallBack(IAsyncResult result)
        {
            if (!_isStopping)
            {
                lock (_lockingObject)
                {
                    if (!_isStopping)
                    {
                        // Call EndWaitForConnection to complete the connection operation
                        _pipeServer.EndWaitForConnection(result);

                        BeginRead(new Info());
                    }
                }
            }
        }

        /// <summary>
        /// This callback is called when the BeginRead operation is completed.
        /// We can arrive here whether the connection is valid or not
        /// </summary>
        private void EndReadCallBack(IAsyncResult result)
        {
            var readBytes = _pipeServer.EndRead(result);
            if (readBytes > 0)
            {
                var info = (Info)result.AsyncState;

                // Get the read bytes and append them
                info.StringBuilder.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (!_pipeServer.IsMessageComplete) // Message is not complete, continue reading
                {
                    BeginRead(info);
                }
                else // Message is completed
                {
                    // Finalize the received string and fire MessageReceivedEvent
                    var message = info.StringBuilder.ToString().TrimEnd('\0');
                    InputCommand deserializedCommand = JsonConvert.DeserializeObject<InputCommand>(message);
                    Commands.Enqueue(deserializedCommand);

                    // Begin a new reading operation
                    BeginRead(new Info());
                }
            }
            else // When no bytes were read, it can mean that the client have been disconnected
            {
               if (!_isStopping)
                {
                    lock (_lockingObject)
                    {
                        if (!_isStopping)
                        {
                            Stop();

                            // Now that we're stopped... we actually want to start again for another client connection
                            Start();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method disconnects, closes and disposes the server
        /// </summary>
        public void Stop()
        {
            _isStopping = true;

            try
            {
                if (_pipeServer.IsConnected)
                {
                    _pipeServer.Disconnect();
                }
            }
            catch (Exception ex)
            {
                _logger.Log("Error stopping... " + ex.Message);
                throw;
            }
            finally
            {
                _pipeServer.Close();
                _pipeServer.Dispose();
            }
        }

        [Serializable]
        public class InputCommand
        {
            public short? LeftThumbX;
            public short? LeftThumbY;
            public short? RightThumbX;
            public short? RightThumbY;
            public byte? LeftTrigger;
            public byte? RightTrigger;
            public bool? A;
            public bool? B;
            public bool? X;
            public bool? Y;
            public bool? LB;
            public bool? RB;
            public bool? LS;
            public bool? RS;
            public bool? Back;
            public bool? Start;
            public bool? DpadUp;
            public bool? DpadRight;
            public bool? DpadLeft;
            public bool? DpadDown;
        }

        /// <summary>
        /// This method begins an asynchronous read operation.
        /// </summary>
        private void BeginRead(Info info)
        {
            try
            {
                _pipeServer.BeginRead(info.Buffer, 0, BufferSize, EndReadCallBack, info);
            }
            catch (Exception ex)
            {
                _logger.Log("Issue reading. " + ex.Message);
                throw;
            }
        }

        public void Dispose()
        {
            _isStopping = true;

            Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Iot.Device.Card;
using PCSC;
using PCSC.Exceptions;
using PCSC.Monitoring;
using PCSC.Utils;

namespace UsbPcscReader
{
    /// <summary>
    /// A smart Class class which allow to transceive data
    /// </summary>
    public class SmartCard : CardTransceiver, IDisposable
    {
        private SCardMonitor _monitor = null;
        private SCardContext _context;
        private CardReader _reader;

        /// <summary>
        /// Get the list of available readers
        /// </summary>
        public string[] ReaderNames { get; internal set; }

        /// <summary>
        /// Returns the number of readers available
        /// </summary>
        public int NumberOfReaders => ReaderNames != null ? ReaderNames.Count() : 0;

        /// <summary>
        /// Do we have a smart card present?
        /// </summary>
        public bool IsCardPResent { get; set; }

        /// <summary>
        /// Create a smart card class
        /// </summary>
        public SmartCard()
        {
            var contextFactory = ContextFactory.Instance;
            _context = (SCardContext)contextFactory.Establish(SCardScope.System);

            ReaderNames = GetReaderNames();

            if (IsEmpty(ReaderNames))
            {
                throw new IOException("Need to have at least a reader connected");
            }

            ReaderSelected = 0;

            _monitor = (SCardMonitor)MonitorFactory.Instance.Create(SCardScope.System);

            // Remember to detach, if you use this in production!
            AttachToAllEvents(_monitor);
            _monitor.Start(ReaderNames);
        }

        /// <summary>
        /// The number of the reader selected
        /// </summary>
        public int ReaderSelected { get; set; }

        private void AttachToAllEvents(ISCardMonitor monitor)
        {
            // Point the callback function(s) to the anonymous & static defined methods below.
            monitor.CardInserted += (sender, args) => CardInseted(args);
            monitor.CardRemoved += (sender, args) => CardRemoved(args);
            //monitor.Initialized += (sender, args) => CardInitialized(args);
            //monitor.StatusChanged += StatusChanged;
            //monitor.MonitorException += MonitorException;
        }

        private void CardRemoved(CardStatusEventArgs args)
        {
            _reader?.Dispose();
            _reader = null;
            IsCardPResent = false;
        }

        private void CardInseted(CardStatusEventArgs args)
        {
            _reader = (CardReader)_context.ConnectReader(ReaderNames[ReaderSelected], SCardShareMode.Shared, SCardProtocol.Any);
            IsCardPResent = true;
        }

        private string[] GetReaderNames()
        {
            using (var context = ContextFactory.Instance.Establish(SCardScope.System))
            {
                return context.GetReaders();
            }
        }

        private bool IsEmpty(ICollection<string> readerNames) => readerNames == null || readerNames.Count < 1;

        /// <summary>
        /// This is the main function to get compatibility with NFC reader
        /// </summary>
        /// <param name="targetNumber">This is ignore as only 1 card is inserted</param>
        /// <param name="dataToSend">The data to send</param>
        /// <param name="dataFromCard">The data to receive</param>
        /// <returns>The number of bytes transceived</returns>
        public override int Transceive(byte targetNumber, ReadOnlySpan<byte> dataToSend, Span<byte> dataFromCard)
        {
            byte[] received = new byte[dataFromCard.Length];
            var ret = _reader.Transmit(dataToSend.ToArray(), received);
            received.CopyTo(dataFromCard);
            return ret;
        }

        /// <summary>
        /// Dispose once finished
        /// </summary>
        public void Dispose()
        {
            if (_monitor.Monitoring)
            {
                _monitor.Cancel();
            }
        }
    }
}

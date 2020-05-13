using Iot.Device.Card.CreditCardProcessing;
using Iot.Device.Ft4222;
using Iot.Device.Pn5180;
using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;
using Iot.Device.Rfid;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using UsbPcscReader;

namespace ReadCreaditCardInfo
{
    class Program
    {
        private static Pn5180 _pn5180;

        static void Main(string[] args)
        {
            Console.WriteLine("Select the reader you have to read all credit card information");
            Console.WriteLine("  1 Smart Card reader");
            Console.WriteLine("  2 Pn532 NFC reader serial port COM4 (adjust for RPI or other port in source code)");
            Console.WriteLine("  3 Pn5180 NFC reader on a RPI");
            Console.WriteLine("  4 Pn5180 NFC reader thru FT4222");
            var selection = Console.ReadKey();
            Console.WriteLine();
            if (selection.KeyChar == '1')
            {
                Console.WriteLine("Introduce your card in the smart card reader");
                Console.WriteLine();
                ReadAndDisplayCardInfoFormCmartCard();
            }
            else if (selection.KeyChar == '2')
            {
                Console.WriteLine("Place your card on the NFC reader");
                Console.WriteLine();
                ReadAndDisplayCardInfoFromPn532();
            }
            else if (selection.KeyChar == '3')
            {
                Console.WriteLine("Place your card on the NFC reader");
                Console.WriteLine();
                ReadAndDisplayCardInfoFromPn5180();
            }
            else if (selection.KeyChar == '4')
            {
                Console.WriteLine("Place your card on the NFC reader");
                Console.WriteLine();
                ReadAndDisplayCardInfoFromPn5180Ft4222();
            }
            else
            {
                Console.WriteLine("You haven't selected either option 1, 2 or 3");
            }
        }

        static void ReadAndDisplayData(CreditCard creditCard)
        {
            creditCard.ReadCreditCardInformation();
            DisplayTags(creditCard.Tags, 0);
            // Display Log Entries
            var format = Tag.SearchTag(creditCard.Tags, 0x9F4F).FirstOrDefault();
            if (format != null)
                DisplayLogEntries(creditCard.LogEntries, format.Tags);
        }

        static void ReadAndDisplayCardInfoFromPn532()
        {
            // Adjust the serial port or use I2C or SPI
            var _nfc = new Pn532("COM4");
            byte[] retData = null;
            while (!Console.KeyAvailable)
            {
                // Polling only type B as Credit Cards are only this type
                retData = _nfc.AutoPoll(5, 500, new PollingType[] { PollingType.Passive106kbpsISO144443_4B });
                if (retData != null)
                    if (retData.Length > 1)
                        break;
                // Give time to PN532 to process
                Thread.Sleep(50);
            }

            if (retData.Length < 3)
            {
                throw new Exception($"No data available on the Credit Card, length of return buffer is less than 3");
            }
            //Check how many tags and the type
            // Console.WriteLine($"Num tags: {retData[0]}, Type: {(PollingType)retData[1]}");
            var decrypted = _nfc.TryDecodeData106kbpsTypeB(retData.AsSpan().Slice(3));
            if (decrypted != null)
            {
                var creditCard = new CreditCard(_nfc, decrypted.TargetNumber);
                ReadAndDisplayData(creditCard);
            }
        }

        static void ReadAndDisplayCardInfoFromPn5180()
        {
            _pn5180 = HardwareSpi();
            TypeB();
        }

        static void ReadAndDisplayCardInfoFromPn5180Ft4222()
        {
            _pn5180 = Ft4222();
            TypeB();
        }

        private static Pn5180 HardwareSpi()
        {
            var spi = SpiDevice.Create(new SpiConnectionSettings(0, 1) { ClockFrequency = Pn5180.MaximumSpiClockFrequency, Mode = Pn5180.DefaultSpiMode, DataFlow = DataFlow.MsbFirst });

            // Reset the device
            var gpioController = new GpioController();
            gpioController.OpenPin(4, PinMode.Output);
            gpioController.Write(4, PinValue.Low);
            Thread.Sleep(10);
            gpioController.Write(4, PinValue.High);
            Thread.Sleep(10);

            return new Pn5180(spi, 2, 3, null, true);
        }

        private static Pn5180 Ft4222()
        {
            var devices = FtCommon.GetDevices();
            Console.WriteLine($"{devices.Count} FT4222 elements found");
            foreach (var device in devices)
            {
                Console.WriteLine($"  Description: {device.Description}");
                Console.WriteLine($"  Flags: {device.Flags}");
                Console.WriteLine($"  Id: {device.Id}");
                Console.WriteLine($"  Location Id: {device.LocId}");
                Console.WriteLine($"  Serial Number: {device.SerialNumber}");
                Console.WriteLine($"  Device type: {device.Type}");
                Console.WriteLine();
            }

            var (chip, dll) = FtCommon.GetVersions();
            Console.WriteLine($"Chip version: {chip}");
            Console.WriteLine($"Dll version: {dll}");

            var ftSpi = new Ft4222Spi(new SpiConnectionSettings(0, 1) { ClockFrequency = Pn5180.MaximumSpiClockFrequency, Mode = Pn5180.DefaultSpiMode, DataFlow = DataFlow.MsbFirst });

            var gpioController = new GpioController(PinNumberingScheme.Board, new Ft4222Gpio());

            // REset the device
            gpioController.OpenPin(0, PinMode.Output);
            gpioController.Write(0, PinValue.Low);
            Thread.Sleep(10);
            gpioController.Write(0, PinValue.High);
            Thread.Sleep(10);

            return new Pn5180(ftSpi, 2, 3, gpioController, true);
        }

        private static void TypeB()
        {
            Data106kbpsTypeB card;
            // Poll the data for 20 seconds
            var ret = _pn5180.ListenToCardIso14443TypeB(TransmitterRadioFrequencyConfiguration.Iso14443B_106, ReceiverRadioFrequencyConfiguration.Iso14443B_106, out card, 20000);
            Console.WriteLine();

            if (!ret)
            {
                Console.WriteLine("Can't read properly the card");
            }
            else
            {
                Console.WriteLine($"Target number: {card.TargetNumber}");
                Console.WriteLine($"App data: {BitConverter.ToString(card.ApplicationData)}");
                Console.WriteLine($"App type: {card.ApplicationType}");
                Console.WriteLine($"UID: {BitConverter.ToString(card.NfcId)}");
                Console.WriteLine($"Bit rates: {card.BitRates}");
                Console.WriteLine($"Cid support: {card.CidSupported}");
                Console.WriteLine($"Command: {card.Command}");
                Console.WriteLine($"Frame timing: {card.FrameWaitingTime}");
                Console.WriteLine($"Iso 14443-4 compliance: {card.ISO14443_4Compliance}");
                Console.WriteLine($"Max frame size: {card.MaxFrameSize}");
                Console.WriteLine($"Nad support: {card.NadSupported}");

                var creditCard = new CreditCard(_pn5180, card.TargetNumber, 2);
                ReadAndDisplayData(creditCard);

                // Halt card
                if (_pn5180.DeselectCardTypeB(card))
                {
                    Console.WriteLine($"Card unselected properly");
                }
                else
                {
                    Console.WriteLine($"ERROR: Card can't be unselected");
                }
            }
        }

        static void ReadAndDisplayCardInfoFormCmartCard()
        {
            SmartCard smartCard = new SmartCard();

            var creditCard = new CreditCard(smartCard, 0, 2);
            while (!smartCard.IsCardPResent)
                Thread.Sleep(1);

            // After inserting the card, need a bit of time before starting to read
            Thread.Sleep(1000);
            ReadAndDisplayData(creditCard);
        }

        static string AddSpace(int level)
        {
            string space = "";
            for (int i = 0; i < level; i++)
                space += "  ";

            return space;
        }

        static void DisplayTags(List<Tag> tagToDisplay, int levels)
        {
            foreach (var tagparent in tagToDisplay)
            {
                Console.Write(AddSpace(levels) + $"{tagparent.TagNumber.ToString(tagparent.TagNumber > 0xFFFF ? "X8" : "X4")}-{TagList.Tags.Where(m => m.TagNumber == tagparent.TagNumber).FirstOrDefault()?.Description}");
                var isTemplate = TagList.Tags.Where(m => m.TagNumber == tagparent.TagNumber).FirstOrDefault();
                if ((isTemplate?.IsTemplate == true) || (isTemplate?.IsConstructed == true))
                {
                    Console.WriteLine();
                    DisplayTags(tagparent.Tags, levels + 1);
                }
                else if (isTemplate?.IsDol == true)
                {
                    //In this case, all the data inside are 1 byte only
                    Console.WriteLine(", Data Object Length elements:");
                    foreach (var dt in tagparent.Tags)
                    {
                        Console.Write(AddSpace(levels + 1) + $"{dt.TagNumber.ToString(dt.TagNumber > 0xFFFF ? "X8" : "X4")}-{TagList.Tags.Where(m => m.TagNumber == dt.TagNumber).FirstOrDefault()?.Description}");
                        Console.WriteLine($", data length: {dt.Data[0]}");
                    }
                }
                else
                {
                    TagDetails tg = new TagDetails(tagparent);
                    Console.WriteLine($": {tg.ToString()}");
                }
            }
        }

        static void DisplayLogEntries(List<byte[]> entries, List<Tag> format)
        {
            for (int i = 0; i < format.Count; i++)
                Console.Write($"{TagList.Tags.Where(m => m.TagNumber == format[i].TagNumber).FirstOrDefault()?.Description} | ");
            Console.WriteLine();

            foreach (var entry in entries)
            {
                int index = 0;
                for (int i = 0; i < format.Count; i++)
                {
                    var data = entry.AsSpan().Slice(index, format[i].Data[0]);
                    var tg = new TagDetails(new Tag() { TagNumber = format[i].TagNumber, Data = data.ToArray() });
                    Console.Write($"{tg.ToString()} | ");
                    index += format[i].Data[0];
                }
                Console.WriteLine();
            }
        }
    }
}

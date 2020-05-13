# Dumping Credit Card or other Payment Card information

This example shows how to dump a Credit Card or any other Payment Card information. The technology used is [.NET Core IoT](https://github.com/dotnet/iot). It does allow to use RFID readers like PN532 and PN5180 to read the cards as well as getting all Card information fully transparently. A support for standard SmartCard USB readers has been added as well. It's using the excellent [PCSC nuget](https://github.com/danm-de/pcsc-sharp).

You can as well use a FT4222 on your Windows or Linux normal PC to add SPI and GPIO support which is needed for the PN5180.

Few things to keep in mind:

- Antenas are leys in reading cards. So some of your cards may be read, some others not.
- You always get few more information from the SmartCard reader and directly on the chip like the card holder name which is not present using NFC.
- All the data extracted can be used for debugging purpose and understanding payment mechanism. To extact all this, a transaction is simulated. A Card Risk Management Data Object List is filled out. This fake transaction may appear in the transaction list and may increase the number of transactions stored in your card.
- No pin code is require, only public data on the card are extracted. Pin code is used to authenticate the user, nothing else.
- A lot of data are actually extracted, those data are needed for Satic Data Authenticaion and Dynamic Data Authentication
- Some cards allows to get history of transactions, some not

Please refer to [PN532](https://github.com/dotnet/iot/tree/master/src/devices/Pn532), [PN5180](https://github.com/dotnet/iot/tree/master/src/devices/Pn5180), [Cerdit Card](https://github.com/dotnet/iot/tree/master/src/devices/Card/CreditCard), [Card Transcieve](https://github.com/dotnet/iot/tree/master/src/devices/Card) and [FT4222](https://github.com/dotnet/iot/tree/master/src/devices/Ft4222)
//---------------------------------------------------------------------------------
// Copyright (c) March 2020, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Need one of TINYCLR_V2_SC20100DEV/TINYCLR_V2_FEZDUINO defined
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Rfm9x.TransmitInterrupt
{
   using System;
   using System.Diagnostics;
   using System.Text;
   using System.Threading;
   using GHIElectronics.TinyCLR.Devices.Gpio;
   using GHIElectronics.TinyCLR.Devices.Spi;
   using GHIElectronics.TinyCLR.Pins;

   public sealed class Rfm9XDevice
   {
      private readonly SpiDevice rfm9XLoraModem;
      private readonly GpioPin InterruptGpioPin = null;
      private const byte RegisterAddressReadMask = 0X7f;
      private const byte RegisterAddressWriteMask = 0x80;

      public Rfm9XDevice(string spiPortName, int chipSelectPin, int resetPin, int interruptPin)
      {
         GpioController gpioController = GpioController.GetDefault();

         GpioPin chipSelectGpio = gpioController.OpenPin(chipSelectPin);

         var settings = new SpiConnectionSettings()
         {
            ChipSelectType = SpiChipSelectType.Gpio,
            ChipSelectLine = chipSelectGpio,
            Mode = SpiMode.Mode0,
            ClockFrequency = 500000,
            ChipSelectActiveState = false,
            //ChipSelectHoldTime = new TimeSpan(50),
            //ChipSelectHoldTime = new TimeSpan(25),
            //ChipSelectHoldTime = new TimeSpan(10),
            //ChipSelectHoldTime = new TimeSpan(5),
            //ChipSelectHoldTime = new TimeSpan(1),
         };

         SpiController spiController = SpiController.FromName(spiPortName);

         rfm9XLoraModem = spiController.GetDevice(settings);

         // Factory reset pin configuration
         GpioPin resetGpioPin = gpioController.OpenPin(resetPin);
         resetGpioPin.SetDriveMode(GpioPinDriveMode.Output);
         resetGpioPin.Write(GpioPinValue.Low);
         Thread.Sleep(10);
         resetGpioPin.Write(GpioPinValue.High);
         Thread.Sleep(10);

         // Interrupt pin for RX message & TX done notification 
         InterruptGpioPin = gpioController.OpenPin(interruptPin);
         InterruptGpioPin.SetDriveMode(GpioPinDriveMode.Input);

         InterruptGpioPin.ValueChanged += InterruptGpioPin_ValueChanged;
      }

      private void InterruptGpioPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
      {
         if (e.Edge != GpioPinEdge.RisingEdge)
         {
            return;
         }

         byte irqFlags = this.RegisterReadByte(0x12); // RegIrqFlags
         Debug.WriteLine($"RegIrqFlags 0X{irqFlags:x2}");

         if ((irqFlags & 0b00001000) == 0b00001000)  // TxDone
         {
            Debug.WriteLine("Transmit-Done");
         }

         this.RegisterWriteByte(0x12, 0xff);// RegIrqFlags
      }

      public Byte RegisterReadByte(byte registerAddress)
      {
         byte[] writeBuffer = new byte[] { registerAddress &= RegisterAddressReadMask, 0x0 };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(rfm9XLoraModem != null);

         rfm9XLoraModem.TransferFullDuplex(writeBuffer, readBuffer);

         return readBuffer[1];
      }

      public ushort RegisterReadWord(byte address)
      {
         byte[] writeBuffer = new byte[] { address &= RegisterAddressReadMask, 0x0, 0x0 };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(rfm9XLoraModem != null);

         rfm9XLoraModem.TransferFullDuplex(writeBuffer, readBuffer);

         return (ushort)(readBuffer[2] + (readBuffer[1] << 8));
      }

      public byte[] RegisterRead(byte address, int length)
      {
         byte[] writeBuffer = new byte[length + 1];
         byte[] readBuffer = new byte[length + 1];
         byte[] replyBuffer = new byte[length];
         Debug.Assert(rfm9XLoraModem != null);

         writeBuffer[0] = address &= RegisterAddressReadMask;

         rfm9XLoraModem.TransferFullDuplex(writeBuffer, readBuffer);

         Array.Copy(readBuffer, 1, replyBuffer, 0, length);

         return replyBuffer;
      }

      public void RegisterWriteByte(byte address, byte value)
      {
         byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, value };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(rfm9XLoraModem != null);

         rfm9XLoraModem.TransferFullDuplex(writeBuffer, readBuffer);
      }

      public void RegisterWriteWord(byte address, ushort value)
      {
         byte[] valueBytes = BitConverter.GetBytes(value);
         byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, valueBytes[0], valueBytes[1] };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(rfm9XLoraModem != null);

         rfm9XLoraModem.TransferFullDuplex(writeBuffer, readBuffer);
      }

      public void RegisterWrite(byte address, byte[] bytes)
      {
         byte[] writeBuffer = new byte[1 + bytes.Length];
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(rfm9XLoraModem != null);

         Array.Copy(bytes, 0, writeBuffer, 1, bytes.Length);
         writeBuffer[0] = address |= RegisterAddressWriteMask;

         rfm9XLoraModem.TransferFullDuplex(writeBuffer, readBuffer);
      }

      public void RegisterDump()
      {
         Debug.WriteLine("Register dump");
         for (byte registerIndex = 0; registerIndex <= 0x42; registerIndex++)
         {
            byte registerValue = this.RegisterReadByte(registerIndex);

            Debug.WriteLine($"Register 0x{registerIndex:x2} - Value 0X{registerValue:x2}");
         }
      }
   }

   class Program
   {
      static void Main()
      {
#if TINYCLR_V2_SC20100DEV
         Rfm9XDevice rfm9XDevice = new Rfm9XDevice(SC20100.SpiBus.Spi3, SC20100.GpioPin.PA13, SC20100.GpioPin.PA14, SC20100.GpioPin.PE4);
#endif
#if TINYCLR_V2_FEZDUINO
         Rfm9XDevice rfm9XDevice = new Rfm9XDevice(SC20100.SpiBus.Spi6, SC20100.GpioPin.PB1, SC20100.GpioPin.PA15, SC20100.GpioPin.PA1); // Doesn't work
#endif
         int SendCount = 0;

         // Put device into LoRa + Sleep mode
         rfm9XDevice.RegisterWriteByte(0x01, 0b10000000); // RegOpMode 

         // Set the frequency to 915MHz
         byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 }; // RegFrMsb, RegFrMid, RegFrLsb
         rfm9XDevice.RegisterWrite(0x06, frequencyWriteBytes);

         // More power PA Boost
         rfm9XDevice.RegisterWriteByte(0x09, 0b10000000); // RegPaConfig

         // Interrupt on TxDone
         rfm9XDevice.RegisterWriteByte(0x40, 0b01000000); // RegDioMapping1 0b00000000 DI0 TxDone

         while (true)
         {
            // Set the Register Fifo address pointer
            rfm9XDevice.RegisterWriteByte(0x0E, 0x00); // RegFifoTxBaseAddress 

            // Set the Register Fifo address pointer
            rfm9XDevice.RegisterWriteByte(0x0D, 0x0); // RegFifoAddrPtr 

            string messageText = $"Hello LoRa {SendCount += 1}!";

            // load the message into the fifo
            byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
            rfm9XDevice.RegisterWrite(0x0, messageBytes); // RegFifo 

            // Set the length of the message in the fifo
            rfm9XDevice.RegisterWriteByte(0x22, (byte)messageBytes.Length); // RegPayloadLength
            Debug.WriteLine($"Sending {messageBytes.Length} bytes message {messageText}");
            rfm9XDevice.RegisterWriteByte(0x01, 0b10000011); // RegOpMode 

            Thread.Sleep(10000);
         }
      }
   }
}
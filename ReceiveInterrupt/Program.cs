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
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Rfm9x.ReceiveInterrupt
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
      private SpiDevice rfm9XLoraModem;
      private GpioPin InterruptGpioPin = null;
      private const byte RegisterAddressReadMask = 0X7f;
      private const byte RegisterAddressWriteMask = 0x80;

      public Rfm9XDevice(int chipSelectPin, int resetPin, int interruptPin)
      {
         var settings = new SpiConnectionSettings()
         {
            ChipSelectType = SpiChipSelectType.Gpio,
            ChipSelectLine = chipSelectPin,
            Mode = SpiMode.Mode0,
            ClockFrequency = 500000,
            DataBitLength = 8,
            ChipSelectActiveState = false,
         };

         SpiController spiCntroller = SpiController.FromName(FEZ.SpiBus.Spi1);

         rfm9XLoraModem = spiCntroller.GetDevice(settings);

         // Factory reset pin configuration
         GpioController gpioController = GpioController.GetDefault();
         GpioPin resetGpioPin = gpioController.OpenPin(resetPin);
         resetGpioPin.SetDriveMode(GpioPinDriveMode.Output);
         resetGpioPin.Write(GpioPinValue.Low);
         Thread.Sleep(10);
         resetGpioPin.Write(GpioPinValue.High);
         Thread.Sleep(10);

         // Interrupt pin for RX message & TX done notification 
         InterruptGpioPin = gpioController.OpenPin(interruptPin);
         resetGpioPin.SetDriveMode(GpioPinDriveMode.Input);

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
         if ((irqFlags & 0b01000000) == 0b01000000)  // RxDone 
         {
            Debug.WriteLine("Receive-Message");
            byte currentFifoAddress = this.RegisterReadByte(0x10); // RegFifiRxCurrent
            this.RegisterWriteByte(0x0d, currentFifoAddress); // RegFifoAddrPtr

            byte numberOfBytes = this.RegisterReadByte(0x13); // RegRxNbBytes

            // Allocate buffer for message
            byte[] messageBytes = new byte[numberOfBytes];

            for (int i = 0; i < numberOfBytes; i++)
            {
               messageBytes[i] = this.RegisterReadByte(0x00); // RegFifo
            }

            string messageText = UTF8Encoding.UTF8.GetString(messageBytes);
            Debug.WriteLine($"Received {messageBytes.Length} byte message {messageText}");
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
         byte[] repyBuffer = new byte[length];
         Debug.Assert(rfm9XLoraModem != null);

         writeBuffer[0] = address &= RegisterAddressReadMask;

         rfm9XLoraModem.TransferFullDuplex(writeBuffer, readBuffer);

         Array.Copy(readBuffer, 1, repyBuffer, 0, length);

         return repyBuffer;
      }

      public void RegisterWriteByte(byte address, byte value)
      {
         byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, value };
         Debug.Assert(rfm9XLoraModem != null);

         rfm9XLoraModem.Write(writeBuffer);
      }

      public void RegisterWriteWord(byte address, ushort value)
      {
         byte[] valueBytes = BitConverter.GetBytes(value);
         byte[] writeBuffer = new byte[] { address |= RegisterAddressWriteMask, valueBytes[0], valueBytes[1] };
         Debug.Assert(rfm9XLoraModem != null);

         rfm9XLoraModem.Write(writeBuffer);
      }

      public void RegisterWrite(byte address, byte[] bytes)
      {
         byte[] writeBuffer = new byte[1 + bytes.Length];
         Debug.Assert(rfm9XLoraModem != null);

         Array.Copy(bytes, 0, writeBuffer, 1, bytes.Length);
         writeBuffer[0] = address |= RegisterAddressWriteMask;

         rfm9XLoraModem.Write(writeBuffer);
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
         Rfm9XDevice rfm9XDevice = new Rfm9XDevice(FEZ.GpioPin.D10, FEZ.GpioPin.D9, FEZ.GpioPin.D2);

         // Put device into LoRa + Sleep mode
         rfm9XDevice.RegisterWriteByte(0x01, 0b10000000); // RegOpMode 

         // Set the frequency to 915MHz
         byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 }; // RegFrMsb, RegFrMid, RegFrLsb
         rfm9XDevice.RegisterWrite(0x06, frequencyWriteBytes);

         rfm9XDevice.RegisterWriteByte(0x0F, 0x0); // RegFifoRxBaseAddress 

         rfm9XDevice.RegisterWriteByte(0x40, 0b00000000); // RegDioMapping1 0b00000000 DI0 RxReady & TxReady

         rfm9XDevice.RegisterWriteByte(0x01, 0b10000101); // RegOpMode set LoRa & RxContinuous

         rfm9XDevice.RegisterDump();

         Debug.WriteLine("Receive-Wait");
         Thread.Sleep(Timeout.Infinite);
      }
   }
}
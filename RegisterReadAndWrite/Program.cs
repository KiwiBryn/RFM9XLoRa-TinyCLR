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
namespace devMobile.IoT.Rfm9x.RegisterReadAndWrite
{
   using System;
   using System.Diagnostics;
   using System.Threading;
   using GHIElectronics.TinyCLR.Devices.Gpio;
   using GHIElectronics.TinyCLR.Devices.Spi;
   using GHIElectronics.TinyCLR.Pins;

   public class Rfm9XDevice:IDisposable
   {
      private bool disposed = false;
      private GpioPin chipSelectGpio = null;
      private SpiDevice rfm9XLoraModem = null;
      private const byte RegisterAddressReadMask = 0X7f;
      private const byte RegisterAddressWriteMask = 0x80;

      public Rfm9XDevice(string spiPortName, int chipSelectPin, int resetPin)
      {
         chipSelectGpio = GpioController.GetDefault().OpenPin(chipSelectPin);

         var settings = new SpiConnectionSettings()
         {
            ChipSelectType = SpiChipSelectType.Gpio,
            ChipSelectLine = chipSelectGpio,
            Mode = SpiMode.Mode0,
            ClockFrequency = 500000,
            ChipSelectActiveState = false,
         };

         SpiController spiController = SpiController.FromName(spiPortName);

         rfm9XLoraModem = spiController.GetDevice(settings);

         // Factory reset pin configuration
         GpioController gpioController = GpioController.GetDefault();
         GpioPin resetGpioPin = gpioController.OpenPin(resetPin);
         resetGpioPin.SetDriveMode(GpioPinDriveMode.Output);
         resetGpioPin.Write(GpioPinValue.Low);
         Thread.Sleep(10);
         resetGpioPin.Write(GpioPinValue.High);
         Thread.Sleep(10);
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (!this.disposed)
         {
            if (disposing)
            {
               if (rfm9XLoraModem != null)
               {
                  rfm9XLoraModem.Dispose();
                  rfm9XLoraModem = null;
               }
               if (chipSelectGpio != null)
               {
                  chipSelectGpio.Dispose();
                  chipSelectGpio = null;
               }
            }

            this.disposed = true;
         }
      }

      ~Rfm9XDevice()
      {
         Dispose(false);
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
         Rfm9XDevice rfm9XDevice = new Rfm9XDevice(SC20100.SpiBus.Spi3, SC20100.GpioPin.PA13, SC20100.GpioPin.PA14);

         rfm9XDevice.RegisterDump();

         while (true)
         {
            Debug.WriteLine("Read RegOpMode (read byte)");
            Byte regOpMode1 = rfm9XDevice.RegisterReadByte(0x1);
            Debug.WriteLine($"RegOpMode 0x{regOpMode1:x2}");

            Debug.WriteLine("Set LoRa mode and sleep mode (write byte)");
            rfm9XDevice.RegisterWriteByte(0x01, 0b10000000);

            Debug.WriteLine("Read RegOpMode (read byte)");
            Byte regOpMode2 = rfm9XDevice.RegisterReadByte(0x1);
            Debug.WriteLine($"RegOpMode 0x{regOpMode2:x2}");

            Debug.WriteLine("Read the preamble (read word)");
            ushort preamble = rfm9XDevice.RegisterReadWord(0x20);
            Debug.WriteLine($"Preamble 0x{preamble:x2}");

            Debug.WriteLine("Set the preamble to 0x80 (write word)");
            rfm9XDevice.RegisterWriteWord(0x20, 0x80);

            Debug.WriteLine("Read the center frequency (read byte array)");
            byte[] frequencyReadBytes = rfm9XDevice.RegisterRead(0x06, 3);
            Debug.WriteLine($"Frequency Msb 0x{frequencyReadBytes[0]:x2} Mid 0x{frequencyReadBytes[1]:x2} Lsb 0x{frequencyReadBytes[2]:x2}");

            Debug.WriteLine("Set the center frequency to 915MHz (write byte array)");
            byte[] frequencyWriteBytes = { 0xE4, 0xC0, 0x00 };
            rfm9XDevice.RegisterWrite(0x06, frequencyWriteBytes);

            rfm9XDevice.RegisterDump();

            Thread.Sleep(30000);
         }
      }
   }
}

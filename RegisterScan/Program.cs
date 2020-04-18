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
namespace devMobile.IoT.Rfm9x.RegisterScan
{
   using System;
   using System.Diagnostics;
   using System.Threading;
   using GHIElectronics.TinyCLR.Devices.Spi;
   using GHIElectronics.TinyCLR.Pins;

   public sealed class Rfm9XDevice
   {
      private SpiDevice rfm9XLoraModem;

      public Rfm9XDevice(int chipSelectPin)
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

         SpiController spiController = SpiController.FromName(FEZ.SpiBus.Spi1);

         rfm9XLoraModem = spiController.GetDevice(settings);
      }

      public Byte RegisterReadByte(byte registerAddress)
      {
         byte[] writeBuffer = new byte[] { registerAddress, 0x0 };
         byte[] readBuffer = new byte[writeBuffer.Length];
         Debug.Assert(rfm9XLoraModem != null);

         rfm9XLoraModem.TransferFullDuplex(writeBuffer, readBuffer);

         return readBuffer[1];
      }
   }

   class Program
   {
      static void Main()
      {
         Rfm9XDevice rfm9XDevice = new Rfm9XDevice(FEZ.GpioPin.D10);

         while (true)
         {
            for (byte registerIndex = 0; registerIndex <= 0x42; registerIndex++)
            {
               byte registerValue = rfm9XDevice.RegisterReadByte(registerIndex);

               Debug.WriteLine($"Register 0x{registerIndex:x2} - Value 0X{registerValue:x2}");
            }
            Debug.WriteLine("");

            Thread.Sleep(10000);
         }
      }
   }
}


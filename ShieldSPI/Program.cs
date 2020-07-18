//---------------------------------------------------------------------------------
// Copyright (c) March/April 2020, devMobile Software
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
// Need one of TINYCLR_V1_FEZDUINO/TINYCLR_V2_SC20100DEV_MIKROBUS_1/TINYCLR_V2_SC20100DEV_MIKROBUS_2/TINYCLR_V2_FEZDUINO/TINYCLR_V2_FEZPORTAL defined
//---------------------------------------------------------------------------------
namespace devMobile.IoT.Rfm9x.ShieldSpi
{
   using System;
   using System.Diagnostics;

   using System.Threading;
   using GHIElectronics.TinyCLR.Devices.Spi;
   using GHIElectronics.TinyCLR.Pins;

   class Program
   {
      static void Main()
      {
         var settings = new SpiConnectionSettings()
         {
            ChipSelectType = SpiChipSelectType.Gpio,
#if TINYCLR_V1_FEZDUINO
            ChipSelectLine = FEZ.GpioPin.D10,
#endif
#if TINYCLR_V2_SC20100DEV_MIKROBUS_1
            ChipSelectLine = GHIElectronics.TinyCLR.Devices.Gpio.GpioController.GetDefault().OpenPin(SC20100.GpioPin.PD3),
#endif
#if TINYCLR_V2_SC20100DEV_MIKROBUS_2
            ChipSelectLine = GHIElectronics.TinyCLR.Devices.Gpio.GpioController.GetDefault().OpenPin(SC20100.GpioPin.PD14),
#endif
#if TINYCLR_V2_FEZDUINO
            ChipSelectLine = GHIElectronics.TinyCLR.Devices.Gpio.GpioController.GetDefault().OpenPin(GHIElectronics.TinyCLR.Pins.SC20100.GpioPin.PB1),
#endif
#if TINYCLR_V2_FEZPORTAL
            ChipSelectLine = GHIElectronics.TinyCLR.Devices.Gpio.GpioController.GetDefault().OpenPin(GHIElectronics.TinyCLR.Pins.SC20100.GpioPin.PC13),
#endif
            Mode = SpiMode.Mode0,
            //Mode = SpiMode.Mode1,
            //Mode = SpiMode.Mode2,
            //Mode = SpiMode.Mode3,
            ClockFrequency = 500000,
            //DataBitLength = 8, Removed as part of TiyCLR V2 Upgrade
            //ChipSelectActiveState = true
            ChipSelectActiveState = false,
            //ChipSelectHoldTime = new TimeSpan(0, 0, 0, 0, 500),
            //ChipSelectSetupTime = new TimeSpan(0, 0, 0, 0, 500),
         };

#if TINYCLR_V1_FEZDUINO
         var controller = SpiController.FromName(FEZ.SpiBus.Spi1);
#endif
#if TINYCLR_V2_SC20100DEV_MIKROBUS_1 || TINYCLR_V2_SC20100DEV_MIKROBUS_2
         var controller = SpiController.FromName(SC20100.SpiBus.Spi3);
#endif
#if TINYCLR_V2_FEZDUINO
         var controller = SpiController.FromName(SC20100.SpiBus.Spi6);
#endif
#if TINYCLR_V2_FEZPORTAL
         var controller = SpiController.FromName(SC20100.SpiBus.Spi3);
#endif
         var device = controller.GetDevice(settings);

         Thread.Sleep(500);

         while (true)
         {
            byte register;
            byte[] writeBuffer;
            byte[] readBuffer;

            // Silicon Version info
            register = 0x42; // RegVersion expecting 0x12

            // Frequency
            //register = 0x06; // RegFrfMsb expecting 0x6C
            //register = 0x07; // RegFrfMid expecting 0x80
            //register = 0x08; // RegFrfLsb expecting 0x00

            //register = 0x17; //RegPayoadLength expecting 0x47

            // Preamble length 
            //register = 0x18; // RegPreambleMsb expecting 0x32
            //register = 0x19; // RegPreambleLsb expecting 0x3E

            //register <<= 1;
            //register |= 0x80;

            //writeBuffer = new byte[] { register };
            writeBuffer = new byte[] { register, 0x0 };
            //writeBuffer = new byte[] {register, 0x0, 0x0};
            //writeBuffer = new byte[] {register, 0x0, 0x0, 0x0};

            readBuffer = new byte[writeBuffer.Length];

            //device.TransferSequential(writeBuffer, readBuffer);
            device.TransferFullDuplex(writeBuffer, readBuffer);

            Debug.WriteLine("Value = 0x" + BytesToHexString(readBuffer));

            Thread.Sleep(1000);
         }
      }

      private static string BytesToHexString(byte[] bytes)
      {
         string hexString = string.Empty;

         // Loop through the bytes.
         for (byte b = 0; b < bytes.Length; b++)
         {
            if (b > 0)
               hexString += "-";

            hexString += bytes[b].ToString("x2");
         }

         return hexString;
      }
   }
}
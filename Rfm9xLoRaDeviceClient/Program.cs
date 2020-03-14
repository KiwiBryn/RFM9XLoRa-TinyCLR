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
namespace devMobile.IoT.Rfm9x.LoRaDeviceClient
{
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Threading;

	using GHIElectronics.TinyCLR.Pins;

	using devMobile.IoT.Rfm9x;

	class Program
   {
      static void Main()
      {
			//const string DeviceName = "FEZLoRa";
			//const string HostName = "LoRaIoT1";
			const string DeviceName = "LoRaIoT1";
#if ADDRESSED_MESSAGES_PAYLOAD
			const string HostName = "LoRaIoT2";
#endif
			const double Frequency = 915000000.0;
			byte MessageCount = System.Byte.MaxValue;
			Rfm9XDevice rfm9XDevice = new Rfm9XDevice(FEZ.GpioPin.D10, FEZ.GpioPin.D9, FEZ.GpioPin.D2);

			rfm9XDevice.Initialise(Frequency, paBoost: true, rxPayloadCrcOn: true);
#if DEBUG
			rfm9XDevice.RegisterDump();
#endif

			rfm9XDevice.OnReceive += Rfm9XDevice_OnReceive;
#if ADDRESSED_MESSAGES_PAYLOAD
			rfm9XDevice.Receive(UTF8Encoding.UTF8.GetBytes(DeviceName));
#else
			rfm9XDevice.Receive();
#endif
			rfm9XDevice.OnTransmit += Rfm9XDevice_OnTransmit;

			Thread.Sleep(10000);

			while (true)
			{
				string messageText = string.Format("Hello from {0} ! {1}", DeviceName, MessageCount);
				MessageCount -= 1;

				byte[] messageBytes = UTF8Encoding.UTF8.GetBytes(messageText);
				Debug.WriteLine($"{DateTime.Now:HH:mm:ss}-TX {messageBytes.Length} byte message {messageText}");
#if ADDRESSED_MESSAGES_PAYLOAD
				rfm9XDevice.Send(UTF8Encoding.UTF8.GetBytes(HostName), messageBytes);
#else
				rfm9XDevice.Send(messageBytes);
#endif
				Thread.Sleep(10000);
			}
		}

		private static void Rfm9XDevice_OnReceive(object sender, Rfm9XDevice.OnDataReceivedEventArgs e)
		{
			try
			{
				string messageText = UTF8Encoding.UTF8.GetString(e.Data);

#if ADDRESSED_MESSAGES_PAYLOAD
				string addressText = UTF8Encoding.UTF8.GetString(e.Address);

				Debug.WriteLine($@"{DateTime.Now:HH:mm:ss}-RX From {addressText} PacketSnr {e.PacketSnr} Packet RSSI {e.PacketRssi}dBm RSSI {e.Rssi}dBm = {e.Data.Length} byte message ""{messageText}""");
#else
				Debug.WriteLine($@"{DateTime.Now:HH:mm:ss}-RX PacketSnr {e.PacketSnr} Packet RSSI {e.PacketRssi}dBm RSSI {e.Rssi}dBm = {e.Data.Length} byte message ""{messageText}""");
#endif
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		private static void Rfm9XDevice_OnTransmit(object sender, Rfm9XDevice.OnDataTransmitedEventArgs e)
		{
			Debug.WriteLine($"{DateTime.Now:HH:mm:ss}-TX Done");
		}
	}
}


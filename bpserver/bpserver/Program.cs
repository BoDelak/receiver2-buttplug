using Buttplug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;

namespace bpclient
{
    class Program
    {
        static ButtplugClient client;
        static bool reconnect = false;

        public static bool IsConnected(TcpClient _tcpClient)
        {
            try
            {
                if (_tcpClient != null && _tcpClient.Client != null && _tcpClient.Client.Connected)
                {
                    /* pear to the documentation on Poll:
                     * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                     * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                     * -or- true if data is available for reading; 
                     * -or- true if the connection has been closed, reset, or terminated; 
                     * otherwise, returns false
                     */

                    // Detect if client disconnected
                    if (_tcpClient.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (_tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Client disconnected
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task Connect()
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 54321);
            server.Start();
            Console.WriteLine("Server started on 127.0.0.1:54321.{0}Waiting for a connection", Environment.NewLine);

            do
            {
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                Console.WriteLine("A client connected.");
                while (true)
                {
                    while (!stream.DataAvailable)
                    {
                        if (!IsConnected(client)) break;
                    }
                    if (!IsConnected(client)) break;
                    while (client.Available < 3) ; // match against "get"

                    try
                    {
                        Byte[] bytes = new byte[client.Available];
                        stream.Read(bytes, 0, bytes.Length);
                        string s = Encoding.UTF8.GetString(bytes);

                        if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                        {
                            //Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                            // 3. Compute SHA-1 and Base64 hash of the new value
                            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                            string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                            byte[] response = Encoding.UTF8.GetBytes(
                                "HTTP/1.1 101 Switching Protocols\r\n" +
                                "Connection: Upgrade\r\n" +
                                "Upgrade: websocket\r\n" +
                                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                            stream.Write(response, 0, response.Length);
                        }
                        else
                        {
                            bool fin = (bytes[0] & 0b10000000) != 0,
                                mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                            int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                                msglen = bytes[1] - 128, // & 0111 1111
                                offset = 2;

                            if (msglen == 126)
                            {
                                // was ToUInt16(bytes, offset) but the result is incorrect
                                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                                offset = 4;
                            }
                            else if (msglen == 127)
                            {
                                Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                                // i don't really know the byte order, please edit this
                                // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                                // offset = 10;
                            }

                            if (msglen == 0)
                            {
                                //Console.WriteLine("msglen == 0");
                            }
                            else if (mask)
                            {
                                byte[] decoded = new byte[msglen];
                                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                                offset += 4;

                                for (int i = 0; i < msglen; ++i)
                                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                                string text = Encoding.UTF8.GetString(decoded);
                                //Console.WriteLine("{0}", text);
                                bool validtext = await ProcessCommand(text);
                                if (!validtext) break;
                            }
                            else
                                Console.WriteLine("mask bit not set");
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
            } while (reconnect);
            server.Stop();
        }

        private static async Task<bool> ProcessCommand(string text)
        {
            if(text.Length > 0)
            {
                switch(text)
                {
                    case "0":
                        // player shoot
                        await ControlDevice(0.3, 100);
                        break;
                    case "1":
                        // explosion
                        await ControlDevice(1.0, 200);
                        break;
                    case "2":
                        // fall
                        break;
                    case "3":
                        // glass
                        break;
                    case "4":
                        // holsterdischarge
                        await ControlDevice(0.8, 100);
                        break;
                    case "5":
                        // shock
                        await ControlDevice(1.0, 3000);
                        break;
                    case "6":
                        // shot
                        await VibratePattern(0.6, 100, 10, 5); // temp 5 repeats. maybe in future get how many shots?
                        break;
                    case "7":
                        // shrapnel
                        break;
                    default:
                        Console.WriteLine("Invalid code. Received: " + text);
                        return false;
                }
                return true;
                //int splitindex = text.IndexOf(',');
                //string sub = text.Substring(0, splitindex);
                //string sub2 = text.Substring(splitindex + 1, text.Length - splitindex - 1);
                //bool failed = false;
                //if (!float.TryParse(sub, out float strength))
                //{
                //    failed = true;
                //}
                //if (!int.TryParse(sub2, out int time))
                //{
                //    failed = true;
                //}

                //if (!failed)
                //{
                //    await ControlDevice(strength, time);
                //}
            }
            return false;
        }

        private static async Task WaitForKey()
        {
            Console.WriteLine("Press any key to continue.");
            while (!Console.KeyAvailable)
            {
                await Task.Delay(1);
            }
            Console.ReadKey(true);
        }

        private static async Task ControlDevice(double strength, int time)
        {
            if (!client.Devices.Any())
            {
                Console.WriteLine("No devices available. Please scan for a device.");
            }

            foreach (ButtplugClientDevice device in client.Devices)
            {
                Console.WriteLine(
                    $"Vibrating all motors of {device.Name} at " + strength + " for " + time + "ms");
                try
                {
                    await device.SendVibrateCmd(strength);
                    await Task.Delay(time);
                    await device.SendVibrateCmd(0);
                }
                catch (ButtplugDeviceException)
                {
                    Console.WriteLine("Device disconnected. Please try another device.");
                }
            }
            return;
        }

        private static async Task VibratePattern(double strength, int time, int downtime, int repeats)
        {
            for(int i = 0; i < repeats; i++)
            {
                await ControlDevice(strength, time);
                await Task.Delay(time + downtime);
            }
        }

        private static async Task Run()
        {
            client = new ButtplugClient("chungus");

            await client.ConnectAsync(new ButtplugWebsocketConnectorOptions(new Uri("ws://localhost:12345/buttplug")));

            void HandleDeviceAdded(object aObj, DeviceAddedEventArgs aArgs)
            {
                Console.WriteLine($"Device connected: {aArgs.Device.Name}");
            }
            void HandleDeviceRemoved(object aObj, DeviceRemovedEventArgs aArgs)
            {
                Console.WriteLine($"Device disconnected: {aArgs.Device.Name}");
            }
            client.DeviceAdded += HandleDeviceAdded;
            client.DeviceRemoved += HandleDeviceRemoved;
            
            async Task ScanForDevices()
            {
                Console.WriteLine("Scanning for devices until key is pressed.");
                Console.WriteLine("Found devices will be printed to console.");
                await client.StartScanningAsync();
                await WaitForKey();

                // Stop scanning now, 'cause we don't want new devices popping up anymore.
                await client.StopScanningAsync();
            }

            await ScanForDevices();

            while(true)
            {
                Console.WriteLine();
                Console.WriteLine("1. Scan For Devices");
                Console.WriteLine("2. Control Devices");
                Console.WriteLine("3. Toggle auto reconnect (currently " + (reconnect ? "enabled" : "disabled") + ")");
                Console.WriteLine("4. Quit");
                Console.WriteLine("Choose an option: ");
                if (!uint.TryParse(Console.ReadLine(), out var choice) ||
                    (choice == 0 || choice > 4))
                {
                    Console.WriteLine("Invalid choice, try again.");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        await ScanForDevices();
                        continue;
                    case 2:
                        await Connect();
                        continue;
                    case 3:
                        reconnect = !reconnect;
                        continue;
                    case 4:
                        return;
                    default:
                        Console.WriteLine("Invalid choice, try again.");
                        continue;
                }
            }
        }

        static void Main(string[] args)
        {
            Run().Wait();
        }
    }
}

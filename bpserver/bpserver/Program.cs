using Buttplug;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace bpclient
{
    class Items
    {
        public JSONItem playerShoot, explosion, fall, glass, holsterDischarge, shock, shot, shrapnel, impaled, hackTap, hackComplete;
    }

    class JSONItem
    {
        public double strength;
        public int time;
    }

    class Program
    {
        static ButtplugClient client;
        static bool reconnect = false;
        static Items values;
        static bool waitingForVibrations = false;

        public static void UpdateValues(string path)
        {
            try
            {
                using (StreamReader r = new StreamReader(path))
                {
                    string json = r.ReadToEnd();
                    values = JsonConvert.DeserializeObject<Items>(json);

                    Console.WriteLine($"\r\nLoaded values from \'{path}\':");
                    Console.WriteLine($"Player shoot: {values.playerShoot.strength}/{values.playerShoot.time}\r\n" +
                                    $"Explosion: {values.explosion.strength}/{values.explosion.time}\r\n" +
                                    $"Fall: {values.fall.strength}/{values.fall.time}\r\n" +
                                    $"Glass: {values.glass.strength}/{values.glass.time}\r\n" +
                                    $"Holster discharge: {values.holsterDischarge.strength}/{values.holsterDischarge.time}\r\n" +
                                    $"Shock: {values.shock.strength}/{values.shock.time}\r\n" +
                                    $"Shot: {values.shot.strength}/{values.shot.time}\r\n" +
                                    $"Shrapnel: {values.shrapnel.strength}/{values.shrapnel.time}\r\n" +
                                    $"Impaled: {values.impaled.strength}/{values.impaled.time}\r\n" +
                                    $"Hack tap: {values.hackTap.strength}/{values.hackTap.time}\r\n" +
                                    $"Hack complete: {values.hackComplete.strength}/{values.hackComplete.time}\r\n");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}\r\nLoading default values");

                values = new Items();
                values.playerShoot.strength = 0.4;
                values.playerShoot.time = 100;
                values.explosion.strength = 1.0;
                values.explosion.time = 200;
                values.fall.strength = 0.6;
                values.fall.time = 100;
                values.glass.strength = 0.0;
                values.glass.time = 0;
                values.holsterDischarge.strength = 0.8;
                values.holsterDischarge.time = 100;
                values.shock.strength = 1.0;
                values.shock.time = 3000;
                values.shot.strength = 0.8;
                values.shot.time = 600;
                values.shrapnel.strength = 0.0;
                values.shrapnel.time = 0;
                values.impaled.strength = 1.0;
                values.impaled.time = 400;
                values.hackTap.strength = 0.2;
                values.hackTap.time = 100;
                values.hackComplete.strength = 0.4;
                values.hackComplete.time = 200;
            }
        }

        public static bool IsConnected(TcpClient _tcpClient)
        {
            try
            {
                if (_tcpClient != null && _tcpClient.Client != null && _tcpClient.Client.Connected)
                {
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
            Console.WriteLine("Server started on 127.0.0.1:54321.\r\nWaiting for a connection");

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
                            Console.WriteLine("Handshaking from client\r\n");
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
                                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                                offset = 4;
                            }

                            if (msglen != 0 && mask)
                            {
                                byte[] decoded = new byte[msglen];
                                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                                offset += 4;

                                for (int i = 0; i < msglen; ++i)
                                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                                string text = Encoding.UTF8.GetString(decoded);
                                bool validtext = await ProcessCommand(text);
                                if (!validtext) break;
                            }
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
            switch(text)
            {
                case "0":
                    await ControlDevice(values.playerShoot.strength, values.playerShoot.time, "Player shoot");
                    break;
                case "1":
                    await ControlDevice(values.explosion.strength, values.explosion.time, "Explosion");
                    break;
                case "2":
                    await ControlDevice(values.fall.strength, values.fall.time, "Fall");
                    break;
                case "3":
                    await ControlDevice(values.glass.strength, values.glass.time, "Glass");
                    break;
                case "4":
                    await ControlDevice(values.holsterDischarge.strength, values.holsterDischarge.time, "Holster discharge");
                    break;
                case "5":
                    await ControlDevice(values.shock.strength, values.shock.time, "Shocked");
                    break;
                case "6":
                    //await VibratePattern(0.6, 100, 2, 5, "Shot"); // temp 5 repeats. maybe in future get how many shots?
                    await ControlDevice(values.shot.strength, values.shot.time, "Shot");
                    break;
                case "7":
                    await ControlDevice(values.shrapnel.strength, values.shrapnel.time, "Shrapnel");
                    break;
                case "8":
                    await ControlDevice(values.impaled.strength, values.impaled.time, "Impaled");
                    break;
                case "9":
                    await ControlDevice(values.hackTap.strength, values.hackTap.time, "Hack Tap");
                    break;
                case "10":
                    await ControlDevice(values.hackComplete.strength, values.hackComplete.time, "Hack Complete");
                    break;
                default:
                    Console.WriteLine("Invalid code. Received: " + text);
                    return false;
            }
            return true;
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

        private static async Task ControlDevice(double strength, int time, string label)
        {
            if (!client.Devices.Any())
            {
                Console.WriteLine("No devices available. Please scan for a device.");
            }

            else
            {
                while (waitingForVibrations) { } // wait here until previous is done
                waitingForVibrations = true;
                foreach (ButtplugClientDevice device in client.Devices)
                {
                    Console.WriteLine($"\"{label}\": Vibrating at {strength * 100}% for {time * 0.001}s");
                    try
                    {
                        await device.SendVibrateCmd(strength);
                    }
                    catch (ButtplugDeviceException)
                    {
                        Console.WriteLine($"{device.Name} disconnected");
                    }
                }
                await Task.Delay(time);
                foreach (ButtplugClientDevice device in client.Devices)
                {
                    try
                    {
                        await device.SendVibrateCmd(0);
                    }
                    catch (ButtplugDeviceException)
                    {
                        Console.WriteLine($"{device.Name} disconnected");
                    }
                }
                waitingForVibrations = false;
            }
            return;
        }

        private static async Task VibratePattern(double strength, int time, int downtime, int repeats, string label)
        {
            for(int i = 0; i < repeats; i++)
            {
                await ControlDevice(strength, time, label);
                await Task.Delay(time + downtime);
            }
        }

        private static async Task Run()
        {
            client = new ButtplugClient("Receiver 2");

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
                await client.StopScanningAsync();
            }

            await ScanForDevices();

            while(true)
            {
                Console.WriteLine("1. Scan for devices");
                Console.WriteLine("2. Control devices");
                Console.WriteLine("3. Load values");
                Console.WriteLine("4. Toggle auto reconnect (currently " + (reconnect ? "enabled" : "disabled") + ")");
                Console.WriteLine("5. Quit");
                Console.WriteLine("Choose an option: ");
                if (!uint.TryParse(Console.ReadLine(), out var choice) ||
                    (choice == 0 || choice > 5))
                {
                    Console.WriteLine("Invalid choice, try again.");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        await ScanForDevices();
                        break;
                    case 2:
                        await Connect();
                        break;
                    case 3:
                        UpdateValues("values.json");
                        break;
                    case 4:
                        reconnect = !reconnect;
                        break;
                    case 5:
                        return;
                    default:
                        Console.WriteLine("Invalid choice, try again.");
                        break;
                }
                Console.WriteLine();
            }
        }

        static void Main(string[] args)
        {
            UpdateValues("values.json");
            Run().Wait();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Ports;

namespace DrumController
{
    public class Head
    {
        private int X = 0;
        private int Y = 1;
        private int OldX = 1;
        private int OldY = 1;
        private int[,] Rows = new int[3, 16];
        private bool Run = true;
        private string PortName = ""; // Default COM port
        public void Start()
        {
            Console.WriteLine("012345678ABCDEFE");
            for(int y=0; y < 3; y++)
            {
                Console.WriteLine("----------------");    
            }
            GenerateInstructions();
            while (Run)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.UpArrow)      MoveY(-1);
                if (key.Key == ConsoleKey.DownArrow)    MoveY(1);
                if (key.Key == ConsoleKey.LeftArrow)    MoveX(-1);
                if (key.Key == ConsoleKey.RightArrow)   MoveX(1);
                if (key.Key == ConsoleKey.X) SwitchValue();
                if (key.Key == ConsoleKey.Escape) Run = false;
                if (key.Key == ConsoleKey.D1) SetValue(1);
                if (key.Key == ConsoleKey.D0) SetValue(0);
                if (key.Key == ConsoleKey.Enter) UploadRythm();
                if (key.Key == ConsoleKey.C)  PortName = GetCOMPorts();
                
            }
            
        }
        public void MoveX(int s)
        {
            X += s;
            if(X < 0) X = 15;
            if(X > 15) X = 0;
            UpdateScreen();
        }
        public void MoveY(int s)
        {
            Y += s;
            if(Y < 1) Y = 3;
            if(Y > 3) Y = 1;
            UpdateScreen();
        }
        public void SetValue(int v)
        {
            Rows[Y-1, X] = v;
            MoveX(1);
        }
        public void SwitchValue()
        {
            Console.SetCursorPosition(X,Y);
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            if(Rows[Y-1, X] == 0)
            {
                Rows[Y-1, X] = 1;
                Console.Write("x");
            }
            else
            {
                Rows[Y-1, X] = 0;
                Console.Write("-");
                
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(0,10);
        }
        private void UpdateScreen()
        {
            Console.SetCursorPosition(OldX,OldY);
            if(Rows[(OldY-1), OldX]==0)
                Console.Write("-");
                else Console.Write("x");
            Console.SetCursorPosition(X,Y);
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            if(Rows[(Y-1), X]==0)
                Console.Write("-");
                else Console.Write("x");
            Console.BackgroundColor = ConsoleColor.Black;
            OldX = X;
            OldY = Y;
            Console.SetCursorPosition(0,10);
        }
        private void UploadRythm()
        {
            Console.SetCursorPosition(0,5);
            Console.WriteLine("=== FT232RL MCU send ===");

            if (string.IsNullOrEmpty(PortName))
            {
                Console.WriteLine("Error: No port found! Is the adapter connected?");
                Console.ReadLine();
                ClearScreenInfo();
                return;
            }

            Console.WriteLine($"Selected port: {PortName}");

            byte[] myValues = new byte[6];
            int z = 0;
            for(int y=0; y < 3; y++)
            {
                for(int x=0; x < 8; x++)
                {
                    if(Rows[y,x]==1)
                    {
                        myValues[z] += (byte)(1 << x);
                    }
                    
                }
                z++;
                for(int x=8; x < 16; x++)
                {
                    if(Rows[y,x]==1)
                    {
                        myValues[z] += (byte)(1 << (x - 8));
                    }
                    
                }
                z++;
            }


            // 2. Skapa datapaketet: 'R' (0x52) + 6 bytes data (totalt 7 bytes)
            // Byt ut 0x01, 0x02 osv. mot dina faktiska värden till microkontrollern
            byte[] packet = new byte[7];
            packet[0] = 0x52; // 'R' i hex
            packet[1] = myValues[0]; // Data byte 1
            packet[2] = myValues[1]; // Data byte 2
            packet[3] = myValues[2]; // Data byte 3
            packet[4] = myValues[3]; // Data byte 4
            packet[5] = myValues[4]; // Data byte 5
            packet[6] = myValues[5]; // Data byte 6

            // 3. Öppna porten och skicka
            using (SerialPort serialPort = new SerialPort(PortName, 9600, Parity.None, 8, StopBits.One))
            {
                // --- LÄGG TILL DESSA RADER FÖR ATT LÖSA DEADLOCK ---
                serialPort.Handshake = Handshake.None; // Stäng av flödeskontroll
                serialPort.RtsEnable = true;          // Krävs ofta av FT232RL för att frigöra sändarlinjen
                serialPort.DtrEnable = true;          // Krävs ofta av FT232RL
                serialPort.WriteTimeout = 1000;       // Gör att programmet kraschar med ett felmeddelande 
                                                    // efter 1 sekund istället för att frysa i evighet
                // --------------------------------------------------

                try
                {
                    serialPort.Open();
                    Console.WriteLine("Port is open.");
                    serialPort.Write(packet, 0, packet.Length);
                    Console.WriteLine("Data sent!");
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Error: Transmission took too long (Write Timeout)!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }


                Console.WriteLine("\nPress any key to return to edit mode.");
                Console.ReadKey();
                Console.SetCursorPosition(0,5);
                ClearScreenInfo();
            
        }
        private string GetCOMPorts()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Available Serial Ports ===");
            string[] ports = SerialPort.GetPortNames();
            int index = 1;

            foreach (string port in ports)
            {
                Console.WriteLine($"{index}- {port}");
                index++;
            }
            Console.WriteLine("Please select a serial port by entering its number:");
            if(int.TryParse(Console.ReadLine(), out int selectedIndex) && selectedIndex > 0 && selectedIndex <= ports.Length)
            {
                Console.WriteLine($"Selected port: {ports[selectedIndex - 1]}. Press any key to continue.");
                Console.ReadKey();
                ClearScreenInfo();
                Console.ForegroundColor = ConsoleColor.White;
                return ports[selectedIndex - 1];
            }
            else
            {
                Console.WriteLine("Invalid selection. Defaulting to first available port. Press any key to continue.");
                Console.ReadKey();
                ClearScreenInfo();
                Console.ForegroundColor = ConsoleColor.White;
                return ports.FirstOrDefault();
            }
            
        }
        private void ClearScreenInfo()
        {
            Console.SetCursorPosition(0,5);
                for(int y=0; y < 20; y++)
                {
                    Console.WriteLine("                                                                          ");    
                }
        }
        private void GenerateInstructions()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetCursorPosition(25,0);
            Console.WriteLine("Arrow keys to move");
            Console.SetCursorPosition(25,1);
            Console.WriteLine("0/1 to set value OR X to switch value");
            Console.SetCursorPosition(25,2);
            Console.WriteLine("Enter to upload");
            Console.SetCursorPosition(25,3);
            Console.WriteLine("C to select COM port");
            Console.SetCursorPosition(25,4);
            Console.WriteLine("Esc to exit");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
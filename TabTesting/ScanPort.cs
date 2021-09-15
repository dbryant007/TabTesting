using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace TabTesting
{
    class ScanPort
    {
        static SerialPort _serialPort;

        public static void ScanComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            int portFoundCount = ports.Length;
            _serialPort = new SerialPort();

            for (int px = 0; px < portFoundCount; px++)
            {
                Console.WriteLine(ports[px]);
            }

            for (int x = 0; x < portFoundCount; x++)
            {
                //try
                //{
                _serialPort.PortName = ports[x];
                _serialPort.BaudRate = 115200;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Parity = Parity.None;
                _serialPort.Open();

                _serialPort.Write("?");
                int portTestCount = 0;
                string portTest = "";

                while ((portTest == "") && (portTestCount < 100))
                {
                    portTest = _serialPort.ReadExisting();
                    portTestCount++;
                }

                if (portTest.Contains("!"))
                {
                    Console.WriteLine("Found the ! Flag on ComPort: " + ports[x].ToString());
                    Globals.teensyComPort = Convert.ToString(ports[x]);
                    x = portFoundCount;
                    _serialPort.Close();
                }
                else
                {
                    _serialPort.Close();
                }
                if ((x == portFoundCount - 1))
                {
                    Globals.teensyComPortOK = false;
                }
                //}

                //catch
                //{
                //    Console.WriteLine(ports[x].ToString() + " Is NOT OK");
                //}                
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace DS18B201WireLib
{
    class WireSearchResult
    {
        public byte[] id = new byte[8];
        public int lastForkPoint = 0;
    }
    public class OneWire
    {
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        public void shutdown()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
                serialPort = null;
            }
        }

        async Task<bool> onewireReset(string deviceId)
        {
            try
            {
                if (serialPort != null)
                    serialPort.Dispose();

                serialPort = await SerialDevice.FromIdAsync(deviceId);

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                dataWriteObject = new DataWriter(serialPort.OutputStream);
                dataWriteObject.WriteByte(0xF0);
                await dataWriteObject.StoreAsync();

                dataReaderObject = new DataReader(serialPort.InputStream);
                await dataReaderObject.LoadAsync(1);
                byte resp = dataReaderObject.ReadByte();
                if (resp == 0xFF)
                {
                    System.Diagnostics.Debug.WriteLine("Nothing connected to UART");
                    return false;
                }
                else if (resp == 0xF0)
                {
                    System.Diagnostics.Debug.WriteLine("No 1-wire devices are present");
                    return false;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Response: " + resp);
                    serialPort.Dispose();
                    serialPort = await SerialDevice.FromIdAsync(deviceId);

                    // Configure serial settings
                    serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                    serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    serialPort.BaudRate = 115200;
                    serialPort.Parity = SerialParity.None;
                    serialPort.StopBits = SerialStopBitCount.One;
                    serialPort.DataBits = 8;
                    serialPort.Handshake = SerialHandshake.None;
                    dataWriteObject = new DataWriter(serialPort.OutputStream);
                    dataReaderObject = new DataReader(serialPort.InputStream);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return false;
            }
        }

        public async Task onewireWriteByte(byte b)
        {
            for (byte i = 0; i < 8; i++, b = (byte)(b >> 1))
            {
                // Run through the bits in the byte, extracting the
                // LSB (bit 0) and sending it to the bus
                await onewireBit((byte)(b & 0x01));
            }
        }

        async Task<byte> onewireBit(byte b)
        {
            var bit = b > 0 ? 0xFF : 0x00;
            dataWriteObject.WriteByte((byte)bit);
            await dataWriteObject.StoreAsync();
            await dataReaderObject.LoadAsync(1);
            var data = dataReaderObject.ReadByte();
            return (byte)(data & 0xFF);
        }

        async Task<byte> onewireReadByte()
        {
            byte b = 0;
            for (byte i = 0; i < 8; i++)
            {
                // Build up byte bit by bit, LSB first
                b = (byte)((b >> 1) + 0x80 * await onewireBit(1));
            }
            System.Diagnostics.Debug.WriteLine("onewireReadByte result: " + b);
            return b;
        }

        public async Task<double> getTemperature(string deviceId)
        {
            double tempCelsius = -200;

            if (await onewireReset(deviceId))
            {
                await onewireWriteByte(0xCC); //1-Wire SKIP ROM command (ignore device id)
                await onewireWriteByte(0x44); //DS18B20 convert T command 
                                              // (initiate single temperature conversion)
                                              // thermal data is stored in 2-byte temperature 
                                              // register in scratchpad memory

                // Wait for at least 750ms for data to be collated
                await Task.Delay(750);

                // Get the data
                await onewireReset(deviceId);
                await onewireWriteByte(0xCC); //1-Wire Skip ROM command (ignore device id)
                await onewireWriteByte(0xBE); //DS18B20 read scratchpad command
                                              // DS18B20 will transmit 9 bytes to master (us)
                                              // starting with the LSB

                byte tempLSB = await onewireReadByte(); //read lsb
                byte tempMSB = await onewireReadByte(); //read msb

                // Reset bus to stop sensor sending unwanted data
                await onewireReset(deviceId);

                // Log the Celsius temperature
                tempCelsius = ((tempMSB * 256) + tempLSB) / 16.0;
                var temp2 = ((tempMSB << 8) + tempLSB) * 0.0625; //just another way of calculating it

                System.Diagnostics.Debug.WriteLine("Temperature: " + tempCelsius + " degrees C " + temp2);
            }
            return tempCelsius;
        }
    }
}

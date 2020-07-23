
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;
using OwenioNet;
using OwenioNet.DataConverter;
using OwenioNet.IO;
using Modbus.Device;
using Modbus.Utility;
using System.Timers;
using System.Threading;
using System.Xml;

namespace Terlych_Thermal_Controller
{
    public partial class Form1 : Form
    {
        //ushort adress: read parameters 0-7; 
        ushort[] readAdressListMB110 = new ushort[] { 4, 10, 16, 22, 28, 34, 40, 46 };
        //ushort adress: read parameters 0-7; 
        ushort[] readAdressList = new ushort[] { 0, 2, 4, 6, 8, 10, 12, 14 };
        //read set parameters 0-7;
        ushort[] readAdressListSet = new ushort[] { 128, 130, 132, 134, 136, 138, 140, 142 };

        string path;
        string pathPress;
        bool saveFileInit = true;
        bool saveFileInitPress = true;
        bool indButton = true;
        int cntPress = 0;
        int numberPress = 0;


        List<string> readParameters = new List<string>() {};

        List<string> readParametersMB110 = new List<string>() { };
        List<string> readParametersSet = new List<string>() { };
        List<string> readTrabatto = new List<string>() { };
        List<string> readPreDrying = new List<string>() { };
        List<string> readBasicDrying = new List<string>() { };
        List<string> hexList = new List<string>() { };

        PointPairList ListPointsTrabatto = new PointPairList();
        PointPairList ListPointsPreDrying = new PointPairList();
        PointPairList ListPointsBasicDrying = new PointPairList();
        PointPairList ListPointsTrabattoHuminity = new PointPairList();
        PointPairList ListPointsPreDryingHuminity = new PointPairList();
        PointPairList ListPointsBasicDryingHuminity = new PointPairList();


        LineItem myCurvePreDrying;
        LineItem myCurveHumidity;

        int number = 0;
        double zg1time = 0;
        string selectedPath;

        public Form1()
        {           
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            if (DateTime.Now.ToString("MM") == "07" || DateTime.Now.ToString("MM") == "08")
            {
                connectButton.Enabled = true;

            }
            else
            {
                connectButton.Enabled = false;
            }


            disconnectButton.Enabled = false;

            baudBox.Items.Add(9600);
            baudBox.Items.Add(19200);
            baudBox.Items.Add(38400);
            baudBox.Items.Add(57600);
            baudBox.Items.Add(74880);
            baudBox.Items.Add(115200);
            baudBox.Items.Add(230400);
            baudBox.Items.Add(250000);
            baudBox.SelectedIndex = 0;

            baudBoxPress.Items.Add(9600);
            baudBoxPress.Items.Add(19200);
            baudBoxPress.Items.Add(38400);
            baudBoxPress.Items.Add(57600);
            baudBoxPress.Items.Add(74880);
            baudBoxPress.Items.Add(115200);
            baudBoxPress.Items.Add(230400);
            baudBoxPress.Items.Add(250000);
            baudBoxPress.SelectedIndex = 0;

            //Timer
            //------------------------------------------------
            Timer1.Interval = 1000;
            Timer1.Tick += new EventHandler(Timer1_Tick);
            timer2.Interval = 1000;
            timer2.Tick += new EventHandler(timer2_Tick);
            //------------------------------------------------
            try
            {
                string[] ports = (SerialPort.GetPortNames());
                portBox.Items.AddRange(ports);
                string[] portsPress = (SerialPort.GetPortNames());
                portBoxPress.Items.AddRange(portsPress);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Mesage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            SelectFoler();
            graph(); 
        }

        private void graph()
        {
            var myPanePreDrying = zedGraphControl1.GraphPane;
            var myPaneHumidity = zedGraphControl2.GraphPane;
            //
            myPanePreDrying.Title.Text = "Графік Температури";           
            myPanePreDrying.XAxis.Title.Text = "Час (hh:mm:ss)";
            myPanePreDrying.YAxis.Title.Text = "Температура (°С)";
            myPanePreDrying.XAxis.Type = AxisType.Date;
            myPanePreDrying.XAxis.Scale.MajorUnit = DateUnit.Hour;
            myPanePreDrying.XAxis.Scale.Format = "T";
            //
            myPaneHumidity.Title.Text = "Графік Вологості";
            myPaneHumidity.XAxis.Title.Text = "Час (hh:mm:ss)";
            myPaneHumidity.YAxis.Title.Text = "Вологість (%)";
            myPaneHumidity.XAxis.Type = AxisType.Date;
            myPaneHumidity.XAxis.Scale.MajorUnit = DateUnit.Hour;
            myPaneHumidity.XAxis.Scale.Format = "T";

            //
            myCurvePreDrying = myPanePreDrying.AddCurve(null, ListPointsTrabatto, Color.Green, SymbolType.None);
            myCurvePreDrying.Line.Width = 2.0F;
            myCurvePreDrying = myPanePreDrying.AddCurve(null, ListPointsPreDrying, Color.Blue, SymbolType.None);
            myCurvePreDrying.Line.Width = 2.0F;
            myCurvePreDrying = myPanePreDrying.AddCurve(null, ListPointsBasicDrying, Color.Red, SymbolType.None);
            myCurvePreDrying.Line.Width = 2.0F;
            myCurveHumidity = myPaneHumidity.AddCurve(null, ListPointsTrabattoHuminity, Color.Purple, SymbolType.None);
            myCurveHumidity.Line.Width = 2.0F;
            myCurveHumidity = myPaneHumidity.AddCurve(null, ListPointsPreDryingHuminity, Color.DarkRed, SymbolType.None);
            myCurveHumidity.Line.Width = 2.0F;
            myCurveHumidity = myPaneHumidity.AddCurve(null, ListPointsBasicDryingHuminity, Color.Teal, SymbolType.None);
            myCurveHumidity.Line.Width = 2.0F;           
            //Сєтка температури
            myPanePreDrying.XAxis.MajorGrid.IsVisible = true;
            myPanePreDrying.XAxis.MajorGrid.DashOn = 10;
            myPanePreDrying.XAxis.MajorGrid.DashOff = 5;
            myPanePreDrying.YAxis.MajorGrid.IsVisible = true;
            myPanePreDrying.YAxis.MajorGrid.DashOff = 5;
            myPanePreDrying.YAxis.MinorGrid.IsVisible = true;
            myPanePreDrying.YAxis.MinorGrid.DashOn = 1;
            myPanePreDrying.YAxis.MinorGrid.DashOff = 2;
            myPanePreDrying.XAxis.MinorGrid.IsVisible = true;
            myPanePreDrying.XAxis.MinorGrid.DashOn = 1;
            myPanePreDrying.XAxis.MinorGrid.DashOff = 2;
            //Сєтка вологості
            myPaneHumidity.XAxis.MajorGrid.IsVisible = true;
            myPaneHumidity.XAxis.MajorGrid.DashOn = 10;
            myPaneHumidity.XAxis.MajorGrid.DashOff = 5;
            myPaneHumidity.YAxis.MajorGrid.IsVisible = true;
            myPaneHumidity.YAxis.MajorGrid.DashOff = 5;
            myPaneHumidity.YAxis.MinorGrid.IsVisible = true;
            myPaneHumidity.YAxis.MinorGrid.DashOn = 1;
            myPaneHumidity.YAxis.MinorGrid.DashOff = 2;
            myPaneHumidity.XAxis.MinorGrid.IsVisible = true;
            myPaneHumidity.XAxis.MinorGrid.DashOn = 1;
            myPaneHumidity.XAxis.MinorGrid.DashOff = 2;
        }

        public void SelectFoler()
        {
            path = "C:\\Users\\HP\\Documents\\" + DateTime.Now.ToString("dd.M.yyyy_HH;mm") + ".txt";
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath);
                }
                path = fbd.SelectedPath + "\\" + "Сушки " + DateTime.Now.ToString("dd.M.yyyy_HH;mm") + ".txt";
                pathPress = fbd.SelectedPath + "\\" + "Прес " + DateTime.Now.ToString("dd.M.yyyy_HH;mm") + ".txt";
                selectedPath = fbd.SelectedPath;
            }
        }
        public void ReadParametrsSet()
        {
            ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(serialPort2);
            List<string> hexBuffer = new List<string>() { };
            ushort startAddress = readAdressListSet[0];
            ushort numOfPoints = 16;
            ushort[] holding_register = master.ReadHoldingRegisters(16, startAddress,
            numOfPoints);
            Thread.Sleep(10);
            foreach (var item in holding_register)
            {
                string intValue = item.ToString();
                string hexValue = item.ToString("X");
                hexBuffer.Add(hexValue);
            }
            for (ushort i = 0; i <= 15; i++)
            {
                if (hexBuffer[i] == "0")
                {
                    hexBuffer[i] = "0000";
                }
                else if (hexBuffer[i + 1] == "0")
                {
                    hexBuffer[i + 1] = "0000";
                }
                string hexParameter;
                if (hexBuffer[i + 1].Length <= 1)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "000";
                }
                else if (hexBuffer[i + 1].Length <= 2)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "00";
                }
                else if (hexBuffer[i + 1].Length <= 3)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "0";
                }
                else
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1];
                }

                var intConvertVar = Convert.ToInt32(hexParameter, 16);
                var byteConvertVar = BitConverter.GetBytes(intConvertVar);
                float temp = BitConverter.ToSingle(byteConvertVar, 0);
                var temperature = Math.Round(temp, 2);
                if (temperature <= 0)
                {
                    readParametersSet.Add("0");
                }
                else
                {
                    readParametersSet.Add(Convert.ToString(temperature));
                }
                i++;
            }
        }
        static ushort[] SetParameter(string value)
        {
            string ToHexString(float f)
            {
                var bytes = BitConverter.GetBytes(f);
                var k = BitConverter.ToInt32(bytes, 0);
                return k.ToString("X8");
            }
            //float FromHexString(string s)
            //{
            //   var k = Convert.ToInt32(s, 16);
            //   var bytes = BitConverter.GetBytes(k);
            //  return BitConverter.ToSingle(bytes, 0);
            //}

            string hexe = ToHexString(float.Parse(value));

            string[] numberArray = new string[hexe.Length];
            int counter = 0;
            for (int v = 0; v < hexe.Length; v++)
            {
                numberArray[v] = hexe.Substring(counter, 1); // 1 is split length
                counter++;
            }
            string a = "0x40" + Convert.ToString(numberArray[0]) + Convert.ToString(numberArray[1]);
            string b = "0x" + Convert.ToString(numberArray[2]) + Convert.ToString(numberArray[3]) +
            Convert.ToString(numberArray[4]) + Convert.ToString(numberArray[5]);
            ushort h = Convert.ToUInt16(a, 16);
            ushort g = Convert.ToUInt16(b, 16);
            ushort[] datta = { h, g };
            return datta;

        }
        public void ReadParametrs()
        {

            ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(serialPort2);
            List<string> hexBuffer = new List<string>() { };

            ushort startAddress = readAdressList[0];
            ushort numOfPoints = 16;
            ushort[] holding_register = master.ReadHoldingRegisters(16, startAddress,
            numOfPoints);
            Thread.Sleep(10);
            foreach (var item in holding_register)
            {
                string intValue = item.ToString();
                string hexValue = item.ToString("X");
                hexBuffer.Add(hexValue);
            }
            for (ushort i = 0; i <= 15; i++)
            {
                if (hexBuffer[i] == "0")
                {
                    hexBuffer[i] = "0000";
                }
                else if (hexBuffer[i + 1] == "0")
                {
                    hexBuffer[i + 1] = "0000";
                }
                string hexParameter;
                if (hexBuffer[i + 1].Length <= 1)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "000";
                }
                else if (hexBuffer[i + 1].Length <= 2)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "00";
                }
                else if (hexBuffer[i + 1].Length <= 3)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "0";
                }
                else
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1];
                }

                var intConvertVar = Convert.ToInt32(hexParameter, 16);
                var byteConvertVar = BitConverter.GetBytes(intConvertVar);
                float temp = BitConverter.ToSingle(byteConvertVar, 0);
                var temperature = Math.Round(temp, 2);
                if (temperature <= 0)
                {
                    readParameters.Add("0");
                }
                else
                {
                    readParameters.Add(Convert.ToString(temperature));
                }
                i++;

                //hexList.Add(hexParameter);
            }

        }
        public void ReadParametrsMB110()
        {

            ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(serialPort2);
            List<string> hexBuffer = new List<string>() { };

            ushort startAddress = readAdressListMB110[0];
            ushort numOfPoints = 16;
            ushort[] holding_register = master.ReadHoldingRegisters(8, startAddress,
            numOfPoints);
            Thread.Sleep(10);
            foreach (var item in holding_register)
            {
                string intValue = item.ToString();
                string hexValue = item.ToString("X");
                hexBuffer.Add(hexValue);
            }
            for (ushort i = 0; i <= 15; i+=2)
            {
                if (hexBuffer[i] == "0")
                {
                    hexBuffer[i] = "0000";
                }
                else if (hexBuffer[i + 1] == "0")
                {
                    hexBuffer[i + 1] = "0000";
                }
                string hexParameter;
                if (hexBuffer[i + 1].Length <= 1)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "000";
                }
                else if (hexBuffer[i + 1].Length <= 2)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "00";
                }
                else if (hexBuffer[i + 1].Length <= 3)
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1] + "0";
                }
                else
                {
                    hexParameter = hexBuffer[i] + hexBuffer[i + 1];
                }

                var intConvertVar = Convert.ToInt32(hexParameter, 16);
                var byteConvertVar = BitConverter.GetBytes(intConvertVar);
                float temp = BitConverter.ToSingle(byteConvertVar, 0);
                var temperature = Math.Round(temp, 2);
                if (temperature <= 0)
                {
                    readParametersMB110.Add("0");
                }
                else
                {
                    readParametersMB110.Add(Convert.ToString(temperature));
                }

                //hexList.Add(hexParameter);
            }

        }
        public void ReadParametrsPVT1()
        {

            ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(serialPort1);
            List<string> hexBuffer = new List<string>() { };
            byte slaveID = 16;
            ushort startAddress = 258;
            ushort numOfPoints = 2;
            ushort [] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            Thread.Sleep(30);
            StringBuilder charT = new StringBuilder(Convert.ToString(holding_register[0]));
            StringBuilder charH = new StringBuilder(Convert.ToString(holding_register[1]));
            
            readTrabatto.Add(Convert.ToString(charT[0]) + Convert.ToString(charT[1]) + "," + Convert.ToString(charT[2]) + Convert.ToString(charT[3]));
            readTrabatto.Add(Convert.ToString(charH[0]) + Convert.ToString(charH[1]) + "," + Convert.ToString(charH[2]) + Convert.ToString(charH[3]));

        }
        public void ReadParametrsPVT2()
        {

            ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(serialPort1);
            List<string> hexBuffer = new List<string>() { };
            byte slaveID = 24;
            ushort startAddress = 258;
            ushort numOfPoints = 2;
            ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            Thread.Sleep(30);
            StringBuilder charT = new StringBuilder(Convert.ToString(holding_register[0]));
            StringBuilder charH = new StringBuilder(Convert.ToString(holding_register[1]));

            readPreDrying.Add(Convert.ToString(charT[0]) + Convert.ToString(charT[1]) + "," + Convert.ToString(charT[2]) + Convert.ToString(charT[3]));
            readPreDrying.Add(Convert.ToString(charH[0]) + Convert.ToString(charH[1]) + "," + Convert.ToString(charH[2]) + Convert.ToString(charH[3]));

        }
        public void ReadParametrsPVT3()
        {

            ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(serialPort1);
            List<string> hexBuffer = new List<string>() { };
            byte slaveID = 32;
            ushort startAddress = 258;
            ushort numOfPoints = 2;
            ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            Thread.Sleep(30);
            StringBuilder charT = new StringBuilder(Convert.ToString(holding_register[0]));
            StringBuilder charH = new StringBuilder(Convert.ToString(holding_register[1]));

            readBasicDrying.Add(Convert.ToString(charT[0]) + Convert.ToString(charT[1]) + "," + Convert.ToString(charT[2]) + Convert.ToString(charT[3]));
            readBasicDrying.Add(Convert.ToString(charH[0]) + Convert.ToString(charH[1]) + "," + Convert.ToString(charH[2]) + Convert.ToString(charH[3]));
            
        }

        private void connectButton_Click_1(object sender, EventArgs e)
        {
            if (portBox.SelectedItem == null || portBoxPress.SelectedItem == null)
            {
                string title = "Помилка!";
                string message = " Виберіть COM-порт!\n";
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                connectButton.Enabled = false;
                disconnectButton.Enabled = true;
                portBox.Enabled = false;
                baudBox.Enabled = false;
                Timer1.Enabled = true;
                portBoxPress.Enabled = false;
                baudBoxPress.Enabled = false;

                timer2.Enabled = true;
                try
                {
                    serialPort1.Close();
                    serialPort1.PortName = portBox.Text;
                    serialPort1.BaudRate = Convert.ToInt32(baudBox.Text);
                    serialPort1.Parity = Parity.None;
                    serialPort1.StopBits = StopBits.One;
                    serialPort1.DataBits = 8;
                    serialPort1.Handshake = Handshake.None;
                    serialPort1.RtsEnable = true;
                    serialPort1.ReadTimeout = 1000;
                    serialPort1.WriteTimeout = 1000;
                    serialPort1.Open();
                    Thread.Sleep(50);
                    serialPort2.Close();
                    serialPort2.PortName = portBoxPress.Text;
                    serialPort2.BaudRate = Convert.ToInt32(baudBoxPress.Text);
                    serialPort2.Parity = Parity.None;
                    serialPort2.StopBits = StopBits.One;
                    serialPort2.DataBits = 8;
                    serialPort2.Handshake = Handshake.None;
                    serialPort2.RtsEnable = true;
                    serialPort2.ReadTimeout = 1000;
                    serialPort2.WriteTimeout = 1000;
                    serialPort2.Open();

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Mesage", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                try
                {
                    ReadParametrsSet();
                    //
                    textBoxSet1.Text = readParametersSet[0];

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Mesage", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } 

        }
        //Timer
        //----------------------------------------------------------------------------------
        private void Timer1_Tick(object Sender, EventArgs e)
        {
            // Set the caption to the current time.  
            label2.Text = DateTime.Now.ToString();
            if (indButton == true)
            {
                buttonStatus.BackColor = Color.LimeGreen;
                indButton = false;    
            }
            else
            {
                buttonStatus.BackColor = Color.White;
                indButton = true;

            }

            try
            {
                if (serialPort1.IsOpen)
                {
                    zg1time = Convert.ToDouble(DateTime.Now.ToOADate());

                    
                    ReadParametrsPVT1();
                    ReadParametrsPVT2();
                    ReadParametrsPVT3();

                    //
                    TrabattoLabel.Text = Convert.ToString(Math.Round(float.Parse(readTrabatto[0]), 1)) + "°С";
                    PreDryingLabel.Text = Convert.ToString(Math.Round(float.Parse(readPreDrying[0]), 1)) + "°С";
                    BasicDryingLabel.Text = Convert.ToString(Math.Round(float.Parse(readBasicDrying[0]), 1)) + "°С";
                    TrabattoLabelHumidity.Text = Convert.ToString(Math.Round(float.Parse(readTrabatto[1]), 1)) + "%";
                    PreDryingLabelHumidity.Text = Convert.ToString(Math.Round(float.Parse(readPreDrying[1]), 1)) + "%";
                    BasicDryingLabelHumidity.Text = Convert.ToString(Math.Round(float.Parse(readBasicDrying[1]), 1)) + "%";
                    //
                    TrabattoGauge.Value = float.Parse(readTrabatto[0]);
                    PreDryingGauge.Value = float.Parse(readPreDrying[0]);
                    BasicDryingGauge.Value = float.Parse(readBasicDrying[0]);
                    TrabattoGaugeHumidity.Value = float.Parse(readTrabatto[1]);
                    PreDryingGaugeHumidity.Value = float.Parse(readPreDrying[1]);
                    BasicDryingGaugeHumidity.Value = float.Parse(readBasicDrying[1]);
                    //
                    chartTemperature.Series.Clear();
                    chartTemperature.Series.Add("Temp");
                    chartTemperature.ChartAreas[0].AxisY.Maximum = 100;
                    //
                    chartTemperature.Series["Temp"].Points.Add(float.Parse(readTrabatto[0]));
                    chartTemperature.Series["Temp"].Points[0].Color = Color.SaddleBrown;
                    chartTemperature.Series["Temp"].Points[0].AxisLabel = "Трабатто";
                    chartTemperature.Series["Temp"].Points[0].LegendText = "Трабатто";
                    chartTemperature.Series["Temp"].Points[0].Label = (readTrabatto[0]);
                    //
                    chartTemperature.Series["Temp"].Points.Add(float.Parse(readPreDrying[0]));
                    chartTemperature.Series["Temp"].Points[1].Color = Color.Olive;
                    chartTemperature.Series["Temp"].Points[1].AxisLabel = "П.С.";
                    chartTemperature.Series["Temp"].Points[1].LegendText = "П.С.";
                    chartTemperature.Series["Temp"].Points[1].Label = (readPreDrying[0]);
                    //
                    chartTemperature.Series["Temp"].Points.Add(float.Parse(readBasicDrying[0]));
                    chartTemperature.Series["Temp"].Points[2].Color = Color.Red;
                    chartTemperature.Series["Temp"].Points[2].AxisLabel = "О.С.";
                    chartTemperature.Series["Temp"].Points[2].LegendText = "О.С.";
                    chartTemperature.Series["Temp"].Points[2].Label = (readBasicDrying[0]);
                    //
                    ListPointsTrabatto.Add(new PointPair(zg1time, float.Parse(readTrabatto[0])));
                    ListPointsPreDrying.Add(new PointPair(zg1time, float.Parse(readPreDrying[0])));
                    ListPointsBasicDrying.Add(new PointPair(zg1time, float.Parse(readBasicDrying[0])));
                    ListPointsPreDryingHuminity.Add(new PointPair(zg1time, float.Parse(readTrabatto[1])));
                    ListPointsTrabattoHuminity.Add(new PointPair(zg1time, float.Parse(readPreDrying[1])));
                    ListPointsBasicDryingHuminity.Add(new PointPair(zg1time, float.Parse(readBasicDrying[1])));
                    //
                    zedGraphControl1.AxisChange();
                    zedGraphControl1.Refresh();
                    zedGraphControl2.AxisChange();
                    zedGraphControl2.Refresh();

                    //Запис даних при запуску
                    if (saveFileInit == true)
                    {
                        number = 1;
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            sw.WriteLine("№ ТР ПС ОС ТР:В ПС:В ОС:В Дата Час");
                            sw.WriteLine(number.ToString() + " " + Convert.ToString(Math.Round(float.Parse(readTrabatto[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readPreDrying[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readBasicDrying[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readTrabatto[1]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readPreDrying[1]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readBasicDrying[1]), 0)) + " " +
                                                                   DateTime.Now.ToString());
                        }
                        saveFileInit = false;
                    }
                    //Створення нового файлу при змінні поточного дня                    
                    else if (DateTime.Now.ToString("HH:mm:ss") == "08:00:00")
                    {    
                        number = 1;
                        path = selectedPath + "\\" + "Сушки " + DateTime.Now.ToString("dd.M.yyyy_HH;mm") + ".txt";
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            sw.WriteLine("№ ТР ПС ОС ТР:В ПС:В ОС:В Дата Час");
                            sw.WriteLine(number.ToString() + " " + Convert.ToString(Math.Round(float.Parse(readTrabatto[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readPreDrying[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readBasicDrying[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readTrabatto[1]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readPreDrying[1]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readBasicDrying[1]), 0)) + " " +
                                                                   DateTime.Now.ToString());
                        }
                    }
                    //Запис даних

                    else if (DateTime.Now.ToString("mm:ss") == "00:00")
                    {
                        number += 1;
                        using (StreamWriter sw = File.AppendText(path))
                        {

                            sw.WriteLine(number.ToString() + " " + Convert.ToString(Math.Round(float.Parse(readTrabatto[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readPreDrying[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readBasicDrying[0]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readTrabatto[1]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readPreDrying[1]), 0)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readBasicDrying[1]), 0)) + " " +
                                                                   DateTime.Now.ToString());
                        }

                    }
                    readTrabatto.Clear();
                    readPreDrying.Clear();
                    readBasicDrying.Clear();

                    hexList.Clear();

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Mesage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void timer2_Tick(object sender, EventArgs e)
        {

            cntPress++;
            try
            {
                if (serialPort2.IsOpen)
                {
                    ReadParametrs();
                    ReadParametrsMB110();
                    labelTemp1.Text = Convert.ToString(Math.Round(float.Parse(readParametersMB110[0]), 1)) + "°С";
                    //labelTemp2.Text = Convert.ToString(Math.Round(float.Parse(readParameters[3]), 1)) + "°С";
                    labelTemp3.Text = Convert.ToString(Math.Round(float.Parse(readParameters[0]), 1)) + "°С";
                    labelTemp4.Text = Convert.ToString(Math.Round(float.Parse(readParameters[1]), 1)) + "°С";
                    labelTemp5.Text = Convert.ToString(Math.Round((float.Parse(readParameters[0]) + float.Parse(readParameters[1])) / 2, 1)) + "°С";
                    labelTemp6.Text = Convert.ToString(Math.Round(float.Parse(readParametersMB110[3]), 1)) + "°С";
                    readParameters[4] = Convert.ToString(Math.Round((float.Parse(readParameters[0]) + float.Parse(readParameters[1])) / 2, 1));

                    //Запис даних при запуску
                    if (saveFileInitPress == true)
                    {
                        numberPress = 1;
                        cntPress = 0;
                        using (StreamWriter sw = File.AppendText(pathPress))
                        {
                            sw.WriteLine("№ Тісто ТНМ Борошно ТУБвх ТУБвих ТУБсер Дата Час");
                            sw.WriteLine(numberPress.ToString() + " " + Convert.ToString(Math.Round(float.Parse(readParametersMB110[0]), 1)) + " " + 
                                                                   "0" + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParametersMB110[3]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[0]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[1]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[4]), 1)) + " " +
                                                                   DateTime.Now.ToString());
                        }
                        saveFileInitPress = false;

                    }
                    //Створення нового файлу при змінні поточного дня                    
                    else if (DateTime.Now.ToString("HH:mm:ss") == "08:00:00")
                    {
                        numberPress = 1;
                        cntPress = 0;
                        pathPress = selectedPath + "\\" + "Прес " + DateTime.Now.ToString("dd.M.yyyy_HH;mm") + ".txt";
                        using (StreamWriter sw = File.AppendText(pathPress))
                        {
                            sw.WriteLine("№ Тісто ТНМ Борошно ТУБвх ТУБвих ТУБсер Дата Час");
                            sw.WriteLine(numberPress.ToString() + " " + Convert.ToString(Math.Round(float.Parse(readParametersMB110[0]), 1)) + " " +
                                                                   "0" + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParametersMB110[3]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[0]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[1]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[4]), 1)) + " " +
                                                                   DateTime.Now.ToString());
                        }

                    }
                    //Запис даних

                    else if (cntPress == 1200)
                    {
                        numberPress += 1;
                        cntPress = 0;
                        using (StreamWriter sw = File.AppendText(pathPress))
                        {
                            sw.WriteLine(numberPress.ToString() + " " + Convert.ToString(Math.Round(float.Parse(readParametersMB110[0]), 1)) + " " + 
                                                                   "0" + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParametersMB110[3]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[0]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[1]), 1)) + " " +
                                                                   Convert.ToString(Math.Round(float.Parse(readParameters[4]), 1)) + " " +
                                                                   DateTime.Now.ToString());
                        }

                    }

                    readParameters.Clear();
                    readParametersSet.Clear();

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Mesage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void disconnectButton_Click(object sender, EventArgs e)
        {
            connectButton.Enabled = true;
            disconnectButton.Enabled = false;
            portBox.Enabled = true;
            baudBox.Enabled = true;
            Timer1.Enabled = false;
            portBoxPress.Enabled = true;
            baudBoxPress.Enabled = true;
            timer2.Enabled = false;

            try
            {
                serialPort2.Close();
                //Get commPorts
                portBoxPress.Items.Clear();
                string[] portsPress = (SerialPort.GetPortNames());
                portBoxPress.Items.AddRange(portsPress);


                serialPort1.Close();
                //Get commPorts
                portBox.Items.Clear();
                string[] ports = (SerialPort.GetPortNames());
                portBox.Items.AddRange(ports);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Mesage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }
            if (serialPort2.IsOpen)
            {
                serialPort2.Close();
            }
        }

        private void basicDryingClearGraphButton_Click_1(object sender, EventArgs e)
        {
            ListPointsTrabatto.Clear();
            ListPointsPreDrying.Clear();
            ListPointsBasicDrying.Clear();
            ListPointsPreDryingHuminity.Clear();
            ListPointsTrabattoHuminity.Clear();
            ListPointsBasicDryingHuminity.Clear();
            zg1time = 0;
            zedGraphControl1.Refresh();
            zedGraphControl2.Refresh();
            graph();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string title = "Інформація";
            string message = " Terlych Thermal Controller 3\n" +
                             " Версія програми: 1.0\n" +
                             " Назва лінії: №3\n " +
                             "Прилад: ТРМ148 v5_08(Modbus)\n " +
                             "Контакти: kseonics.technology@gmail.com\n" +
                             " © Kseonics Technology 2020\n" +
                             " Код ЕГРПОУ: 43689994\n";
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonSet1_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort2.IsOpen)
                {
                    //Задавати Уставку 
                    //---------------------------------------------------------------//                  
                    ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(serialPort2);
                    ushort Address = 128;
                    ushort[] setData = SetParameter(textBoxSet1.Text);
                    master.WriteMultipleRegisters(16, Address, setData);
                    //----------------------------------------------------------------------------------------------//

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Mesage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        
        }

        private void buttonRead_Click(object sender, EventArgs e)
        {
            try
            {
                ReadParametrsSet();
                //
                textBoxSet1.Text = readParametersSet[0];

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Mesage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}

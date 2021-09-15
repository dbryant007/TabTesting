using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace TabTesting
{
    public partial class Form1 : Form
    {
        public int trackDefectCnt1;
        public int trackDefectCnt2;
        public int trackDefectCnt3;
        public int trackDefectCnt4;
        public int trackDefectCnt5;
        public int trackDefectCnt6;
        public int trackDefectCnt7;

        public int scanDefectCnt1;
        public int scanDefectCnt2;
        public int scanDefectCnt3;
        public int scanDefectCnt4;
        public int scanDefectCnt5;
        public int scanDefectCnt6;
        public int scanDefectCnt7;

        //public int x;

        public static string scanTrack;
        public static string scanStatus;
        public static string scanReply;
        public static string scanUnknownMessage;
        public static string inData;

        double prctComplete;

        Bitmap bmp = new Bitmap(500, 500);
        Bitmap ebmp = new Bitmap(500, 500);
        
        SerialPort serialPort1;

        public string filePath;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // *** Find the Scanner Com Port by testing for "?" => "!" ****
            ScanPort.ScanComPorts();

            serialPort1 = new SerialPort();

            if (!Globals.teensyComPortOK)
            {
                MessageBox.Show("Error", "No COM port found");
            }
            else
            {
                serialPort1.PortName = Globals.teensyComPort;
                serialPort1.BaudRate = 115200;
                serialPort1.DataBits = 8;
                serialPort1.StopBits = StopBits.One;
                serialPort1.Parity = Parity.None;
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                serialPort1.ReadBufferSize = 16384;
                serialPort1.Open();


                string iniString = System.IO.File.ReadAllText(@"C:\ScanBeta\SDA100ini.ini");
                string[] iniData = iniString.Split(',');
                Globals.iniOID = iniData[0];
                Globals.mapRes = int.Parse(iniData[1]);
                Globals.waferDiam = int.Parse(iniData[2]);
                Globals.edgeRej = int.Parse(iniData[3]);
                Globals.sectorSteps = iniData[4];
                Globals.trackSteps = iniData[5];
                Globals.parkY = iniData[6];
                Globals.parkX = iniData[7];
                Globals.parkZ = iniData[8];
                Globals.centerY = iniData[9];
                Globals.centerX = iniData[10];
                Globals.pSize1Label = iniData[11];
                Globals.pSize2Label = iniData[12];
                Globals.pSize3Label = iniData[13];
                Globals.pSize4Label = iniData[14];
                Globals.pSize5Label = iniData[15];
                Globals.pSize6Label = iniData[16];
                Globals.pSize7Label = iniData[17];
                Globals.afTimeOut = iniData[18];
                Globals.dirData = iniData[19];
                Globals.dirRecipe = iniData[20];
                Globals.dirINI = iniData[21];
                Globals.dirErrorLog = iniData[22];

                serialPort1.Write("." + Globals.mapRes + "r");

                serialPort1.Write("." + Globals.waferDiam + "d");
                lblCCWaferSize_Current.Text = "." + Globals.waferDiam + "d";

                serialPort1.Write("." + Globals.edgeRej + "e");
                lblCCEdgeReject_Current.Text = "." + Globals.edgeRej + "e";

                serialPort1.Write("." + Globals.sectorSteps + "S");

                serialPort1.Write("." + Globals.trackSteps + "T");

                serialPort1.Write("." + Globals.parkY + "y");

                serialPort1.Write("." + Globals.parkX + "x");

                serialPort1.Write("." + Globals.parkZ + "z");

                serialPort1.Write("." + Globals.centerY + "w");

                serialPort1.Write("." + Globals.centerX + "v");

                //need to add the autofocus time out here when the comand is available

                string recString = System.IO.File.ReadAllText(@"C:\ScanBeta\SDA100rec.txt");
                //string[] recData = iniString.Split(',');
                lbxLoadBox.DataSource = System.IO.File.ReadAllLines(@"C:\ScanBeta\SDA100rec.txt");
                lbxScanDataFiles.DataSource = System.IO.Directory.GetFiles(@"C:\ScanBeta\","*.dat");

                //lbxLoadBox.Items.Clear();

                //string line;
                //while ((line = recString.))
            }
        }
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            inData = serialPort1.ReadLine();

            if (inData.Contains("<"))
            {
                OpenFile();
            }

            else if (inData.Contains("$"))
            {
                scanTrack = inData;

                using (System.IO.StreamWriter sw = System.IO.File.AppendText(filePath))
                {
                    sw.WriteLine(scanTrack);
                }

                BeginInvoke(new EventHandler(MapDefectData));
            }
            else if (inData.Contains("!"))
            {
                scanReply = inData;
                BeginInvoke(new EventHandler(ResponseData));
            }
            else if (inData.Contains("*"))
            {
                scanStatus = inData;
                BeginInvoke(new EventHandler(MachineStatus));
            }
            else if (inData.Contains(">"))
            {
                Console.WriteLine("Saw the >!");
                serialPort1.Write("P");
                serialPort1.Write("o");
                serialPort1.Write("N");
            }
            else if (inData.Contains("%"))
            {
                BeginInvoke(new EventHandler(ProgressBarStatus));
                //Console.WriteLine(inData);
            }
            else
            {
                BeginInvoke(new EventHandler(DisplayErrorMessage));
            }
        }

        private void ProgressBarStatus(object sender, EventArgs e)
        {
            inData = inData.Remove(inData.Length - 2, 2);
            Console.WriteLine(inData);
            prctComplete = (Convert.ToDouble(inData)) * 100;
            Console.WriteLine(prctComplete);
            progressBar.Value = Convert.ToInt32(prctComplete);
        }

        private void DisplayErrorMessage(object sender, EventArgs e)
        {
            Console.WriteLine("Unknown Response: " + scanUnknownMessage);
        }

        private void MachineStatus(object sender, EventArgs e)
        {
            Console.WriteLine("Machine Status: " + scanStatus);
        }

        private void ResponseData(object sender, EventArgs e)
        {
            Console.WriteLine("Response Data: " + scanReply);
        }

        private void OpenFile()
        {
            string folderName = @"C:\ScanBeta\";
            string fileName = "Scan" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";

            filePath = System.IO.Path.Combine(folderName, fileName);
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(filePath))
            {
                sw.Write("INI Info: ");
                sw.WriteLine(System.IO.File.ReadAllText(@"C:\ScanBeta\iniScanBeta.txt"));
            }
        }
        private void MapDefectData(object sender, EventArgs e)
        {
            const char DELIM = ';';
            string[] sectors = scanTrack.Split(DELIM);
            for (int y = 0; y < (sectors.Length - 1); y++)
            {
                string[] DefectVal = sectors[y].Split(',');
                int[] DefectValInt = Array.ConvertAll(DefectVal, int.Parse);
                for (int z = 0; z < DefectVal.Length; z++)
                {
                    switch (DefectValInt[2])
                    {
                        case 0:
                            break;
                        case 1:
                            bmp.SetPixel(DefectValInt[1], DefectValInt[0], Color.Blue);
                            trackDefectCnt1++;
                            break;
                        case 2:
                            bmp.SetPixel(DefectValInt[1], DefectValInt[0], Color.BlueViolet);
                            trackDefectCnt2++;
                            break;
                        case 3:
                            bmp.SetPixel(DefectValInt[1], DefectValInt[0], Color.Fuchsia);
                            trackDefectCnt3++;
                            break;
                        case 4:
                            bmp.SetPixel(DefectValInt[1], DefectValInt[0], Color.Red);
                            trackDefectCnt4++;
                            break;
                        case 5:
                            bmp.SetPixel(DefectValInt[1], DefectValInt[0], Color.Orange);
                            trackDefectCnt5++;
                            break;
                        case 6:
                            bmp.SetPixel(DefectValInt[1], DefectValInt[0], Color.Yellow);
                            trackDefectCnt6++;
                            break;
                        case 7:
                            bmp.SetPixel(DefectValInt[1], DefectValInt[0], Color.LightGreen);
                            trackDefectCnt7++;
                            break;
                    }
                }
                pictureBox1.Image = bmp;

            }
            // total track counts into the wafer scan counts after
            // each track is processes. Then clear track counts
            // and update Histogram bars.
            // scanDefectCnt passes values to the MaxHeight calculator 
            scanDefectCnt1 += trackDefectCnt1;
            scanDefectCnt2 += trackDefectCnt2;
            scanDefectCnt3 += trackDefectCnt3;
            scanDefectCnt4 += trackDefectCnt4;
            scanDefectCnt5 += trackDefectCnt5;
            scanDefectCnt6 += trackDefectCnt6;
            scanDefectCnt7 += trackDefectCnt7;

            trackDefectCnt1 = 0;
            trackDefectCnt2 = 0;
            trackDefectCnt3 = 0;
            trackDefectCnt4 = 0;
            trackDefectCnt5 = 0;
            trackDefectCnt6 = 0;
            trackDefectCnt7 = 0;

            postHistData();
        }
        public int GetMax(int val1, int val2)
        {
            if (val1 > val2)
            {
                return val1;
            }
            return val2;
        }
        public void postHistData()
        {
            const int maxTextHeight = 100;
            int maxHeight = 0;
            int maxHeightBar = 0;


            def1.Size = new Size(50, scanDefectCnt1);
            maxHeight = def1.Height;

            def2.Size = new Size(50, scanDefectCnt2);
            maxHeight = GetMax(maxHeight, def2.Height);

            def3.Size = new Size(50, scanDefectCnt3);
            maxHeight = GetMax(maxHeight, def3.Height);

            def4.Size = new Size(50, scanDefectCnt4);
            maxHeight = GetMax(maxHeight, def4.Height);

            def5.Size = new Size(50, scanDefectCnt5);
            maxHeight = GetMax(maxHeight, def5.Height);

            def6.Size = new Size(50, scanDefectCnt6);
            maxHeight = GetMax(maxHeight, def6.Height);

            def7.Size = new Size(50, scanDefectCnt7);
            maxHeight = GetMax(maxHeight, def7.Height);

            // processor to SNAP histogram Y Axis to fixed ranges
            if (maxHeight <= 50)
            {
                maxHeightBar = 50;
            }
            else if ((maxHeight > 50) && (maxHeight <= 100))
            {
                maxHeightBar = 100;
            }
            else if ((maxHeight > 100) && (maxHeight <= 250))
            {
                maxHeightBar = 250;
            }

            else if ((maxHeight > 250) && (maxHeight <= 500))
            {
                maxHeightBar = 500;
            }
            else if ((maxHeight > 500) && (maxHeight <= 1000))
            {
                maxHeightBar = 1000;
            }

            else if ((maxHeight > 1000) && (maxHeight <= 2500))
            {
                maxHeightBar = 2500;
            }
            else if ((maxHeight > 2500) && (maxHeight <= 5000))
            {
                maxHeightBar = 5000;
            }
            else if ((maxHeight > 5000) && (maxHeight <= 10000))
            {
                maxHeightBar = 10000;
            }
            else if ((maxHeight > 1000) && (maxHeight <= 25000))
            {
                maxHeightBar = 25000;
            }
            else if ((maxHeight > 25000) && (maxHeight <= 50000))
            {
                maxHeightBar = 50000;
            }
            else
            {
                maxHeightBar = 100000;
            }


            lblMaxHeightBar.Text = maxHeightBar.ToString();
            lblMidHeightBar.Text = (maxHeightBar / 2).ToString();
            lblMinHeightBar.Text = ("0");

            lblScanDefectCnt1.Text = scanDefectCnt1.ToString();
            lblSizeClass_PSize1_Count.Text = scanDefectCnt1.ToString();
            lblScanDefectCnt2.Text = scanDefectCnt2.ToString();
            lblSizeClass_PSize2_Count.Text = scanDefectCnt2.ToString();
            lblScanDefectCnt3.Text = scanDefectCnt3.ToString();
            lblSizeClass_PSize3_Count.Text = scanDefectCnt3.ToString();
            lblScanDefectCnt4.Text = scanDefectCnt4.ToString();
            lblSizeClass_PSize4_Count.Text = scanDefectCnt4.ToString();
            lblScanDefectCnt5.Text = scanDefectCnt5.ToString();
            lblSizeClass_PSize5_Count.Text = scanDefectCnt5.ToString();
            lblScanDefectCnt6.Text = scanDefectCnt6.ToString();
            lblSizeClass_PSize6_Count.Text = scanDefectCnt6.ToString();
            lblScanDefectCnt7.Text = scanDefectCnt7.ToString();
            lblSizeClass_PSize7_Count.Text = scanDefectCnt7.ToString();
            lblSizeClass_Total_Count.Text = (scanDefectCnt1 + scanDefectCnt2 + scanDefectCnt3
                + scanDefectCnt4 + scanDefectCnt5 + scanDefectCnt6 + scanDefectCnt7).ToString();

            //good to scale now replace def height with scaled height
            def1.Height = def1.Height * maxTextHeight / maxHeightBar;
            def2.Height = def2.Height * maxTextHeight / maxHeightBar;
            def3.Height = def3.Height * maxTextHeight / maxHeightBar;
            def4.Height = def4.Height * maxTextHeight / maxHeightBar;
            def5.Height = def5.Height * maxTextHeight / maxHeightBar;
            def6.Height = def6.Height * maxTextHeight / maxHeightBar;
            def7.Height = def7.Height * maxTextHeight / maxHeightBar;

            int disHisx = disHis.Location.X;
            int disHisy = disHis.Location.Y;
            int disHisHeight = disHis.Size.Height;
            int disHisWidth = disHis.Size.Width;

            int def1Am = def1.Size.Height;
            int def1x = def1.Location.X;
            int def1y = def1.Location.Y;

            int def2Am = def2.Size.Height;
            int def2x = def2.Location.X;
            int def2y = def2.Location.Y;

            int def3Am = def3.Size.Height;
            int def3x = def3.Location.X;
            int def3y = def3.Location.Y;

            int def4Am = def4.Size.Height;
            int def4x = def4.Location.X;
            int def4y = def4.Location.Y;

            int def5Am = def5.Size.Height;
            int def5x = def5.Location.X;
            int def5y = def5.Location.Y;

            int def6Am = def6.Size.Height;
            int def6x = def6.Location.X;
            int def6y = def6.Location.Y;

            int def7Am = def7.Size.Height;
            int def7x = def7.Location.X;
            int def7y = def7.Location.Y;

            int disYPoint = disHisy + disHisHeight;

            int bSyPoint1 = disYPoint - def1Am;
            int bSyPoint2 = disYPoint - def2Am;
            int bSyPoint3 = disYPoint - def3Am;
            int bSyPoint4 = disYPoint - def4Am;
            int bSyPoint5 = disYPoint - def5Am;
            int bSyPoint6 = disYPoint - def6Am;
            int bSyPoint7 = disYPoint - def7Am;

            def1.Location = new Point(def1x, bSyPoint1);
            def2.Location = new Point(def2x, bSyPoint2);
            def3.Location = new Point(def3x, bSyPoint3);
            def4.Location = new Point(def4x, bSyPoint4);
            def5.Location = new Point(def5x, bSyPoint5);
            def6.Location = new Point(def6x, bSyPoint6);
            def7.Location = new Point(def7x, bSyPoint7);

        }
        public void EdgeReject()
        {
            //int SizeofWaferMM = 125;
            //int EdgeRejctMM = 5;
            //int MapResolutionPixels = 500;
            int MapRadiusPixels;
            int PixelsPerMM;
            int EdgeRejectPixels;
            int WaferDiamPixels;
            int xPixelLoc;
            int yPixelLoc;
            int ZoneCenterPoint;

            // **** Setup all the variables for calculations - Second the DOUBLES for TRIG Math ****
            double j, i;
            double degrees;
            double angle;
            double sinAngle;
            double cosAngle;
            double ZoneRadiusPixels;

            pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            // **** Setup all the variables for calculations - Third the STRINGS ****
            // **** Initial Calculations ****
            PixelsPerMM = Globals.mapRes / Globals.waferDiam;
            EdgeRejectPixels = PixelsPerMM * Globals.edgeRej;
            WaferDiamPixels = PixelsPerMM * (Globals.waferDiam);
            MapRadiusPixels = Globals.mapRes / 2;

            ZoneRadiusPixels = WaferDiamPixels / 2;
            ZoneCenterPoint = Convert.ToInt32(ZoneRadiusPixels) + EdgeRejectPixels;



            // **** TRIG Calculations to draw wafer edge line ****
            for (i = 0; i < 3; i++)
            {
                for (j = 0.2; j < 360;)
                {
                    degrees = j;
                    angle = Math.PI * degrees / 180.0;
                    sinAngle = Math.Sin(angle);
                    cosAngle = Math.Cos(angle);
                    xPixelLoc = MapRadiusPixels + Convert.ToInt32(MapRadiusPixels * cosAngle);
                    yPixelLoc = MapRadiusPixels + Convert.ToInt32(MapRadiusPixels * sinAngle);

                    ((Bitmap)pictureBox1.Image).SetPixel(xPixelLoc, yPixelLoc, Color.Black);
                    //bmp.SetPixel(xPixelLoc, yPixelLoc, Color.Black);
                    j = j + 0.2;
                }
            }

            for (i = 2; i < (EdgeRejectPixels + 2); i++)
            {
                for (j = 0.2; j < 360;)
                {
                    degrees = j;
                    angle = Math.PI * degrees / 180.0;
                    sinAngle = Math.Sin(angle);
                    cosAngle = Math.Cos(angle);
                    xPixelLoc = MapRadiusPixels + Convert.ToInt32((MapRadiusPixels - i) * cosAngle);
                    yPixelLoc = MapRadiusPixels + Convert.ToInt32((MapRadiusPixels - i) * sinAngle);

                    ((Bitmap)pictureBox1.Image).SetPixel(xPixelLoc, yPixelLoc, Color.DarkGray);
                    j = j + 0.2;
                }
            }
            bmp = (Bitmap)pictureBox1.Image;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            serialPort1.Write("iI"); // Preset XYZ Alignment Positions
        }

        private void button11_Click(object sender, EventArgs e)
        {
            serialPort1.Write("h"); // Home Z-Stage
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
        private void btnRun_Click(object sender, EventArgs e)
        {
            EdgeReject();

            trackDefectCnt1 = 0;
            trackDefectCnt2 = 0;
            trackDefectCnt3 = 0;
            trackDefectCnt4 = 0;
            trackDefectCnt5 = 0;
            trackDefectCnt6 = 0;
            trackDefectCnt7 = 0;

            scanDefectCnt1 = 0;
            scanDefectCnt2 = 0;
            scanDefectCnt3 = 0;
            scanDefectCnt4 = 0;
            scanDefectCnt5 = 0;
            scanDefectCnt6 = 0;
            scanDefectCnt7 = 0;

            postHistData();
            serialPort1.Write("O"); //Turn chuck vac on
            serialPort1.Write("n"); //Close door
            serialPort1.Write("G"); //Start scan
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            serialPort1.Write("g"); // STOP Stage
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            serialPort1.Write("P Z");   // Park Stage at preScan position
            serialPort1.Write("o");     // Open door
            serialPort1.Write("N");     //Turn chuck vac off
        }

        private void btnRun_Click_1(object sender, EventArgs e)
        {
            EdgeReject();

            trackDefectCnt1 = 0;
            trackDefectCnt2 = 0;
            trackDefectCnt3 = 0;
            trackDefectCnt4 = 0;
            trackDefectCnt5 = 0;
            trackDefectCnt6 = 0;
            trackDefectCnt7 = 0;

            scanDefectCnt1 = 0;
            scanDefectCnt2 = 0;
            scanDefectCnt3 = 0;
            scanDefectCnt4 = 0;
            scanDefectCnt5 = 0;
            scanDefectCnt6 = 0;
            scanDefectCnt7 = 0;

            postHistData();
            serialPort1.Write("O"); //Turn chuck vac on
            serialPort1.Write("n"); //Close door
            serialPort1.Write("G"); //Start scan
        }

        private void btnStop_Click_1(object sender, EventArgs e)
        {
            serialPort1.Write("g"); // STOP Stage
        }

        private void btnLoad_Click_1(object sender, EventArgs e)
        {
            serialPort1.Write("P Z");   // Park Stage at preScan position
            serialPort1.Write("o");     // Open door
            serialPort1.Write("N");     //Turn chuck vac off
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            serialPort1.Write("H");     //Homes the machine
        }

        private void lbxLoadBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedRecipe = lbxLoadBox.SelectedItem.ToString();
            string[] recData = selectedRecipe.Split(',');
            
            txtSSRecipeName_Set.Text = recData[3];            
            txtSSUserID_Set.Text = recData[4];
            txtSSScanID_Set.Text = recData[5];
            cbxSSWaferSize_Set.Text = recData[6];
            cbxSSEdgeReject_Set.Text = recData[7];
            cbxSSScanOfArea_Set.Text = recData[8];
            //cbxSSZoneScanType_Set.Text = recDate[9]; //add later...currently disabled
            if (recData[10] == "1")
            {
                chboxAutoSave.Checked = true;
            }

            txtSizeClass_Size1_Limit.Text = recData[12];
            txtSizeClass_Size2_Limit.Text = recData[13];
            txtSizeClass_Size3_Limit.Text = recData[14];
            txtSizeClass_Size4_Limit.Text = recData[15];
            txtSizeClass_Size5_Limit.Text = recData[16];
            txtSizeClass_Size6_Limit.Text = recData[17];
            txtSizeClass_Size7_Limit.Text = recData[18];
            txtSizeClass_Total_Limit.Text = recData[19];
            txtTestComments.Text = recData[20];


        }

        private void chboxAutoSave_CheckedChanged(object sender, EventArgs e)
        {
            if (chboxAutoSave.Checked == true)
            {
                Globals.autoSave = "1";
            }
            else
            {
                Globals.autoSave = "0";
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string recSaveData;
            recSaveData = DateTime.Now.ToString("yyyyMMddHHmmss") + ",";
            recSaveData += DateTime.Now.ToString("yyyyMMddHHmmss") + ",";
            recSaveData += "Active" + ",";
            recSaveData += txtSSRecipeName_Set.Text + ",";
            recSaveData += txtSSUserID_Set.Text + ",";
            recSaveData += txtSSScanID_Set.Text + ",";
            recSaveData += cbxSSWaferSize_Set.Text + ",";
            recSaveData += cbxSSEdgeReject_Set.Text + ",";
            recSaveData += cbxSSScanOfArea_Set.Text + ",";
            //recSaveData += cbxSSZoneScanType_Set.Text + ","; //add later...currently disabled
            if (chboxAutoSave.Checked == true)
            {
                recSaveData += "1" + ",";
            }
            else
            {
                recSaveData += "0" + ",";
            }
            recSaveData += txtSizeClass_Size1_Limit.Text + ",";
            recSaveData += txtSizeClass_Size2_Limit.Text + ",";
            recSaveData += txtSizeClass_Size3_Limit.Text + ",";
            recSaveData += txtSizeClass_Size4_Limit.Text + ",";
            recSaveData += txtSizeClass_Size5_Limit.Text + ",";
            recSaveData += txtSizeClass_Size6_Limit.Text + ",";
            recSaveData += txtSizeClass_Size7_Limit.Text + ",";
            recSaveData += txtSizeClass_Total_Limit.Text + ",";
            recSaveData += txtTestComments.Text + ";";



        }

        public void lbxScanDataFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] eScanData = System.IO.File.ReadAllLines(lbxScanDataFiles.SelectedItem.ToString());
            string[] einiData = eScanData[2].Split(',');

            Emulator.einiOID = einiData[0];
            Emulator.emapRes = int.Parse(einiData[1]);
            Emulator.ewaferDiam = int.Parse(einiData[2]);
            Emulator.eedgeRej = int.Parse(einiData[3]);
            Emulator.esectorSteps = einiData[4];
            Emulator.etrackSteps = einiData[5];
            Emulator.eparkY = einiData[6];
            Emulator.eparkX = einiData[7];
            Emulator.eparkZ = einiData[8];
            Emulator.ecenterY = einiData[9];
            Emulator.ecenterX = einiData[10];
            Emulator.epSize1Label = einiData[11];
            Emulator.epSize2Label = einiData[12];
            Emulator.epSize3Label = einiData[13];
            Emulator.epSize4Label = einiData[14];
            Emulator.epSize5Label = einiData[15];
            Emulator.epSize6Label = einiData[16];
            Emulator.epSize7Label = einiData[17];
            Emulator.eafTimeOut = einiData[18];
            Emulator.edirData = einiData[19];
            Emulator.edirRecipe = einiData[20];
            Emulator.edirINI = einiData[21];
            Emulator.edirErrorLog = einiData[22];
            EEdgeReject();
            EMapDefectData(eScanData);
            //trackDefectCnt1 = 0;
            //trackDefectCnt2 = 0;
            //trackDefectCnt3 = 0;
            //trackDefectCnt4 = 0;
            //trackDefectCnt5 = 0;
            //trackDefectCnt6 = 0;
            //trackDefectCnt7 = 0;

            //scanDefectCnt1 = 0;
            //scanDefectCnt2 = 0;
            //scanDefectCnt3 = 0;
            //scanDefectCnt4 = 0;
            //scanDefectCnt5 = 0;
            //scanDefectCnt6 = 0;
            //scanDefectCnt7 = 0;

            //EPostHistData();
        }

        private void EMapDefectData(string[] eScanData)
        {
            for (int x = 4; x < eScanData.Length; x++)
            {
                string eScanTrack = eScanData[x];
                const char DELIM = ';';
                string[] esectors = eScanTrack.Split(DELIM);
                for (int y = 0; y < (esectors.Length - 1); y++)
                {
                    string[] eDefectVal = esectors[y].Split(',');
                    int[] eDefectValInt = Array.ConvertAll(eDefectVal, int.Parse);
                    for (int z = 0; z < eDefectVal.Length; z++)
                    {
                        switch (eDefectValInt[2])
                        {
                            case 0:
                                break;
                            case 1:
                                ebmp.SetPixel(eDefectValInt[1], eDefectValInt[0], Color.Blue);
                                Emulator.etrackDefectCnt1++;
                                break;
                            case 2:
                                ebmp.SetPixel(eDefectValInt[1], eDefectValInt[0], Color.BlueViolet);
                                Emulator.etrackDefectCnt2++;
                                break;
                            case 3:
                                ebmp.SetPixel(eDefectValInt[1], eDefectValInt[0], Color.Fuchsia);
                                Emulator.etrackDefectCnt3++;
                                break;
                            case 4:
                                ebmp.SetPixel(eDefectValInt[1], eDefectValInt[0], Color.Red);
                                Emulator.etrackDefectCnt4++;
                                break;
                            case 5:
                                ebmp.SetPixel(eDefectValInt[1], eDefectValInt[0], Color.Orange);
                                Emulator.etrackDefectCnt5++;
                                break;
                            case 6:
                                ebmp.SetPixel(eDefectValInt[1], eDefectValInt[0], Color.Yellow);
                                Emulator.etrackDefectCnt6++;
                                break;
                            case 7:
                                ebmp.SetPixel(eDefectValInt[1], eDefectValInt[0], Color.LightGreen);
                                Emulator.etrackDefectCnt7++;
                                break;
                        }
                    }
                    eScanDataImage.Image = ebmp;

                }
                // total track counts into the wafer scan counts after
                // each track is processes. Then clear track counts
                // and update Histogram bars.
                // scanDefectCnt passes values to the MaxHeight calculator 
                Emulator.escanDefectCnt1 += Emulator.etrackDefectCnt1;
                Emulator.escanDefectCnt2 += Emulator.etrackDefectCnt2;
                Emulator.escanDefectCnt3 += Emulator.etrackDefectCnt3;
                Emulator.escanDefectCnt4 += Emulator.etrackDefectCnt4;
                Emulator.escanDefectCnt5 += Emulator.etrackDefectCnt5;
                Emulator.escanDefectCnt6 += Emulator.etrackDefectCnt6;
                Emulator.escanDefectCnt7 += Emulator.etrackDefectCnt7;

                Emulator.etrackDefectCnt1 = 0;
                Emulator.etrackDefectCnt2 = 0;
                Emulator.etrackDefectCnt3 = 0;
                Emulator.etrackDefectCnt4 = 0;
                Emulator.etrackDefectCnt5 = 0;
                Emulator.etrackDefectCnt6 = 0;
                Emulator.etrackDefectCnt7 = 0;

                EPostHistData();
            }
        }

        public void EPostHistData()
        {
            const int maxTextHeight = 100;
            int maxHeight = 0;
            int maxHeightBar = 0;


            edef1.Size = new Size(50, Emulator.escanDefectCnt1);
            maxHeight = edef1.Height;

            edef2.Size = new Size(50, Emulator.escanDefectCnt2);
            maxHeight = GetMax(maxHeight, edef2.Height);

            edef3.Size = new Size(50, Emulator.escanDefectCnt3);
            maxHeight = GetMax(maxHeight, edef3.Height);

            edef4.Size = new Size(50, Emulator.escanDefectCnt4);
            maxHeight = GetMax(maxHeight, edef4.Height);

            edef5.Size = new Size(50, Emulator.escanDefectCnt5);
            maxHeight = GetMax(maxHeight, edef5.Height);

            edef6.Size = new Size(50, Emulator.escanDefectCnt6);
            maxHeight = GetMax(maxHeight, edef6.Height);

            edef7.Size = new Size(50, Emulator.escanDefectCnt7);
            maxHeight = GetMax(maxHeight, edef7.Height);

            // processor to SNAP histogram Y Axis to fixed ranges
            if (maxHeight <= 50)
            {
                maxHeightBar = 50;
            }
            else if ((maxHeight > 50) && (maxHeight <= 100))
            {
                maxHeightBar = 100;
            }
            else if ((maxHeight > 100) && (maxHeight <= 250))
            {
                maxHeightBar = 250;
            }

            else if ((maxHeight > 250) && (maxHeight <= 500))
            {
                maxHeightBar = 500;
            }
            else if ((maxHeight > 500) && (maxHeight <= 1000))
            {
                maxHeightBar = 1000;
            }

            else if ((maxHeight > 1000) && (maxHeight <= 2500))
            {
                maxHeightBar = 2500;
            }
            else if ((maxHeight > 2500) && (maxHeight <= 5000))
            {
                maxHeightBar = 5000;
            }
            else if ((maxHeight > 5000) && (maxHeight <= 10000))
            {
                maxHeightBar = 10000;
            }
            else if ((maxHeight > 1000) && (maxHeight <= 25000))
            {
                maxHeightBar = 25000;
            }
            else if ((maxHeight > 25000) && (maxHeight <= 50000))
            {
                maxHeightBar = 50000;
            }
            else
            {
                maxHeightBar = 100000;
            }


            lbleMaxHeightBar.Text = maxHeightBar.ToString();
            lbleMidHeightBar.Text = (maxHeightBar / 2).ToString();
            lbleMinHeightBar.Text = ("0");

            lbleScanDefectCnt1.Text = scanDefectCnt1.ToString();
            lbleSizeClass_PSize1_Count.Text = scanDefectCnt1.ToString();
            lbleScanDefectCnt2.Text = scanDefectCnt2.ToString();
            lbleSizeClass_PSize2_Count.Text = scanDefectCnt2.ToString();
            lbleScanDefectCnt3.Text = scanDefectCnt3.ToString();
            lbleSizeClass_PSize3_Count.Text = scanDefectCnt3.ToString();
            lbleScanDefectCnt4.Text = scanDefectCnt4.ToString();
            lbleSizeClass_PSize4_Count.Text = scanDefectCnt4.ToString();
            lbleScanDefectCnt5.Text = scanDefectCnt5.ToString();
            lbleSizeClass_PSize5_Count.Text = scanDefectCnt5.ToString();
            lbleScanDefectCnt6.Text = scanDefectCnt6.ToString();
            lbleSizeClass_PSize6_Count.Text = scanDefectCnt6.ToString();
            lbleScanDefectCnt7.Text = scanDefectCnt7.ToString();
            lbleSizeClass_PSize7_Count.Text = scanDefectCnt7.ToString();
            lbleSizeClass_Total_Count.Text = (scanDefectCnt1 + scanDefectCnt2 + scanDefectCnt3
                + scanDefectCnt4 + scanDefectCnt5 + scanDefectCnt6 + scanDefectCnt7).ToString();

            //good to scale now replace def height with scaled height
            edef1.Height = edef1.Height * maxTextHeight / maxHeightBar;
            edef2.Height = edef2.Height * maxTextHeight / maxHeightBar;
            edef3.Height = edef3.Height * maxTextHeight / maxHeightBar;
            edef4.Height = edef4.Height * maxTextHeight / maxHeightBar;
            edef5.Height = edef5.Height * maxTextHeight / maxHeightBar;
            edef6.Height = edef6.Height * maxTextHeight / maxHeightBar;
            edef7.Height = edef7.Height * maxTextHeight / maxHeightBar;

            int edisHisx = eDisHis.Location.X;
            int edisHisy = eDisHis.Location.Y;
            int edisHisHeight = eDisHis.Size.Height;
            int edisHisWidth = eDisHis.Size.Width;

            int edef1Am = edef1.Size.Height;
            int edef1x = edef1.Location.X;
            int edef1y = edef1.Location.Y;

            int edef2Am = edef2.Size.Height;
            int edef2x = edef2.Location.X;
            int edef2y = edef2.Location.Y;

            int edef3Am = edef3.Size.Height;
            int edef3x = edef3.Location.X;
            int edef3y = edef3.Location.Y;

            int edef4Am = edef4.Size.Height;
            int edef4x = edef4.Location.X;
            int edef4y = edef4.Location.Y;

            int edef5Am = edef5.Size.Height;
            int edef5x = edef5.Location.X;
            int edef5y = edef5.Location.Y;

            int edef6Am = edef6.Size.Height;
            int edef6x = edef6.Location.X;
            int edef6y = edef6.Location.Y;

            int edef7Am = edef7.Size.Height;
            int edef7x = edef7.Location.X;
            int edef7y = edef7.Location.Y;

            int edisYPoint = edisHisy + edisHisHeight;

            int ebSyPoint1 = edisYPoint - edef1Am;
            int ebSyPoint2 = edisYPoint - edef2Am;
            int ebSyPoint3 = edisYPoint - edef3Am;
            int ebSyPoint4 = edisYPoint - edef4Am;
            int ebSyPoint5 = edisYPoint - edef5Am;
            int ebSyPoint6 = edisYPoint - edef6Am;
            int ebSyPoint7 = edisYPoint - edef7Am;

            edef1.Location = new Point(edef1x, ebSyPoint1);
            edef2.Location = new Point(edef2x, ebSyPoint2);
            edef3.Location = new Point(edef3x, ebSyPoint3);
            edef4.Location = new Point(edef4x, ebSyPoint4);
            edef5.Location = new Point(edef5x, ebSyPoint5);
            edef6.Location = new Point(edef6x, ebSyPoint6);
            edef7.Location = new Point(edef7x, ebSyPoint7);

        }
        public void EEdgeReject()
        {
            //int SizeofWaferMM = 125;
            //int EdgeRejctMM = 5;
            //int MapResolutionPixels = 500;
            int MapRadiusPixels;
            int PixelsPerMM;
            int EdgeRejectPixels;
            int WaferDiamPixels;
            int xPixelLoc;
            int yPixelLoc;
            int ZoneCenterPoint;

            // **** Setup all the variables for calculations - Second the DOUBLES for TRIG Math ****
            double j, i;
            double degrees;
            double angle;
            double sinAngle;
            double cosAngle;
            double ZoneRadiusPixels;

            eScanDataImage.Image = new Bitmap(eScanDataImage.Width, eScanDataImage.Height);

            // **** Setup all the variables for calculations - Third the STRINGS ****
            // **** Initial Calculations ****
            PixelsPerMM = Emulator.emapRes / Emulator.ewaferDiam;
            EdgeRejectPixels = PixelsPerMM * Emulator.eedgeRej;
            WaferDiamPixels = PixelsPerMM * (Emulator.ewaferDiam);
            MapRadiusPixels = Emulator.emapRes / 2;

            ZoneRadiusPixels = WaferDiamPixels / 2;
            ZoneCenterPoint = Convert.ToInt32(ZoneRadiusPixels) + EdgeRejectPixels;



            // **** TRIG Calculations to draw wafer edge line ****
            for (i = 0; i < 3; i++)
            {
                for (j = 0.2; j < 360;)
                {
                    degrees = j;
                    angle = Math.PI * degrees / 180.0;
                    sinAngle = Math.Sin(angle);
                    cosAngle = Math.Cos(angle);
                    xPixelLoc = MapRadiusPixels + Convert.ToInt32(MapRadiusPixels * cosAngle);
                    yPixelLoc = MapRadiusPixels + Convert.ToInt32(MapRadiusPixels * sinAngle);

                    ((Bitmap)eScanDataImage.Image).SetPixel(xPixelLoc, yPixelLoc, Color.Black);
                    //bmp.SetPixel(xPixelLoc, yPixelLoc, Color.Black);
                    j = j + 0.2;
                }
            }

            for (i = 2; i < (EdgeRejectPixels + 2); i++)
            {
                for (j = 0.2; j < 360;)
                {
                    degrees = j;
                    angle = Math.PI * degrees / 180.0;
                    sinAngle = Math.Sin(angle);
                    cosAngle = Math.Cos(angle);
                    xPixelLoc = MapRadiusPixels + Convert.ToInt32((MapRadiusPixels - i) * cosAngle);
                    yPixelLoc = MapRadiusPixels + Convert.ToInt32((MapRadiusPixels - i) * sinAngle);

                    ((Bitmap)eScanDataImage.Image).SetPixel(xPixelLoc, yPixelLoc, Color.DarkGray);
                    j = j + 0.2;
                }
            }
            ebmp = (Bitmap)eScanDataImage.Image;
        }
    }
}
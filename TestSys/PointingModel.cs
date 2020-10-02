using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AstrometryNova.PointingModel;
using SRSLib;
using Emgu.CV;
using Emgu.CV.UI;
using Astro;
using MongoDB.Driver;
using Accord.Collections;
using Newtonsoft.Json;
using AstrometryNova.PointingModel.Log;
using MetroFramework.Components;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace TestSys
{
    partial class PointingModel : Form
    {

        private DataBaseMongo database;
      
        private Cv imageprocessing;
        private MathRaDec test;
        private string path;
        private string catalogpath=null;

        Tuple<Image<Emgu.CV.Structure.Bgr, byte>,//image output
            Image<Emgu.CV.Structure.Gray, byte>, //image conturn
            IDictionary<string, string>, // object ra and dec 
            VectorOfVectorOfPoint, // Contours
            List<int[]>> //x,y from image 
            result; // x,y from platwave 

        private IFindFluent<GaiaInfo11, GaiaInfo11> CenterS;
        private double[] radec;

        //database
        private List<string> namekey = new List<string>()
                    {
                        "OBJCTRA",
                        "OBJCTDEC",
                        "DATE",
                        "LST"
                    };
        private double[][] RaDec;
        private double[][] dbresultxy;
        private double[][] predictresultxy;
        private double[,] platconstan;
        //KDTree
        private KDTreeCluster kdt;
        private double[][] PointRaDecPrediction;

        //platwave  
        private SRSLib.ImageLib.ImageType imageType;
        private MatchLib.PlateListType centerRa2000GuessRads;

        //json 
        private JsonAstro jsonfile;

        //Scatter plot 
        private ZedGrapScatter RaDecPlot;

        //gri
        List<TypeRaDec> datagri;
        private DriveInfo[] drives;

        public PointingModel()
        {
            InitializeComponent();
           
            this.database = new DataBaseMongo();
            metroButton1.Enabled = false;
            this.jsonfile =new JsonAstro();
            WindowState = FormWindowState.Maximized;
            textBox2.Text = TyTask.f.ToString();
            textBox3.Text = TyTask.p.ToString();
            metroButton2.Enabled = false;
            TyTask.statusbuttom = false;
    

            drives = DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Removable).ToArray();
            if (drives.Length == 0)
            {
                toolStripDropDownButton1.Text = "Not Connected";
                toolStripDropDownButton1.Enabled= false;
            }
            else
            {
                foreach (DriveInfo drive in drives)
                {
                    toolStripDropDownButton1.DropDownItems.Add(drive.Name.ToString());
                }
                toolStripDropDownButton1.Enabled = true;
            }


        }

        

        private double[] Convert2180(double a, double b)
        {

            Angle Ra = Angle.FromRads(a);
            Ra = Ra.RerangeDegs(-180, 180);
            Angle Dec = Angle.FromRads(b);
            Dec = Dec.RerangeDegs(-90, 90);

            return new double[] { Ra.Degs, Dec.Degs };

        }
        private double[] ConvertRaDec(IDictionary<string, string> radec)

        {

            double Ra = Angle.FromHms(radec["OBJCTRA"]).Degs;
            double Dec = Angle.FromDms(radec["OBJCTDEC"]).Degs;

            return new double[] { Ra, Dec };

        }

        private void PlatConstan()
        {
            int k = 0;
            double[] radecl = MethodStaticFomula.StandardCoordi(ra: RaDec[0][0], dec: RaDec[0][1], radec[0], radec[1]);
            PointRaDecPrediction = new double[result.Item5.Count][];
            test = new MathRaDec(predictresultxy);
            foreach (int[] i in result.Item5)
            {
                double[] re = test.Tranfrom2invert(i);
                double[] resultPredict = MethodStaticFomula.InvertStandardCoordi(re[0], re[1], radec[0], radec[1]);
                PointRaDecPrediction[k] = resultPredict;
                k++;
            }

        }
        private void DataBase()
        {

            double[] radectranfrom = ConvertRaDec(this.result.Item3);
            double[] centermongo= Convert2180(radectranfrom[0]*(Math.PI/180), radectranfrom[1] * (Math.PI / 180));
            
            CenterS =this.database.database.GeocenterSpherestring(Convert.ToDouble(centermongo[0]), Convert.ToDouble(centermongo[1]), 0.00396);
            /**
             * 
             * 
             * 
             **/
            predictresultxy = this.database.database.XY2RaDec(data: result.Item5, radec: result.Item3);
            dbresultxy = this.database.database.RaDec2XY(CenterS, result.Item3);
            RaDec = this.database.database.GetRaDec(data: CenterS);
            radec = ConvertRaDec(result.Item3);


        }

        public void KDTree()
        {
            kdt = new KDTreeCluster(PointRaDecPrediction);
            kdt.SetupNode(kdt.tree.Root.Left.Right);
            int numberbar = 0;
            for (int i = 0; i < RaDec.Length; i++)
            {
                KDTreeNodeCollection<KDTreeNode<int>> kdtfilter = kdt.FindWithNeighbors(RaDec[i], 1);
                numberbar = ((i * 100) / RaDec.Length);
                TyTask.progreebar3 = numberbar;
                if (kdtfilter.Minimum != 0)
                    kdt.Add(
                        new TypeKDTree()
                        {
                            Distand = kdtfilter.Minimum,
                            Nearest = kdtfilter.Nearest.Position,
                            Father = RaDec[i]

                        }

                        );

            }
        }

       
        public void Json()
        {
            this.jsonfile.datetime = DateTime.Now.ToString();
            this.jsonfile.numplate = result.Item4.Size;
            this.jsonfile.data = kdt.Query();



        }


        public void DatagridView()
        {
            datagri = CreateData2GridView(PointRaDecPrediction, RaDec, kdt.Query(), result.Item3);
            var ui= new DataGrid();
            ui.SetGrid(datagri);
            ui.Show();
        }

        public void Plantwave(Cv imageprocessing)
        {
            if(!String.IsNullOrEmpty(catalogpath))
            {
                imageType = new SRSLib.ImageLib.ImageType();
                SRSLib.ImageLib.OpenAnyImageType(this.path, ref imageType); //file fit path
                MatchLib.SetCatalogLocation(catalogpath);
                centerRa2000GuessRads = new MatchLib.PlateListType()
                {
                    Px = imageType.N1,
                    Py = imageType.N2,
                    XSize = (double)imageType.N1 * 1 / 206264.806,
                    YSize = (double)imageType.N2 * 1 / 206264.806,
                    HaveStartingCoords = false
                };

                MatchLib.ExtractStars(ref imageType, ref centerRa2000GuessRads);
                MatchLib.PlateMatch(ref centerRa2000GuessRads);
            }
            
            
        }
        public void ImageProcessing()
        {
            
            this.imageprocessing = new Cv(this.path);
            this.imageprocessing.namekey = namekey;
            imageBox1.Image = this.imageprocessing.ImageJPG;
            this.result= this.imageprocessing.SegmentionWatershed(threshmin: 10, flat: false, TypeImage.JPG);
            imageBox2.Image = this.result.Item2;
            
        }

        

        private List<TypeRaDec> CreateData2GridView(double[][] centerradac, double[][] RaDec, List<TypeKDTree> prediction, IDictionary<string, string> radec)
        {
            double ra = Angle.FromHms(radec["OBJCTRA"]).Degs;
            double dec = Angle.FromDms(radec["OBJCTDEC"]).Degs;
            var list = new List<TypeRaDec>();
            for (int i = 0; i < centerradac.Length; i++)
            {
                //Console.WriteLine("[{0},{1}],", Math.Abs( centerradac[i][0] - 4096),Math.Abs( centerradac[i][1]- 4096));
                double[] xy = MethodStaticFomula.StandardCoordi(centerradac[i][0], centerradac[i][1], ra, dec);
                if(i < prediction.Count)
                {
                    list.Add(
                    new TypeRaDec()
                    {
                        X = prediction[i].Nearest[0],
                        Y = prediction[i].Nearest[1],
                        Ra = RaDec[i][0],
                        Dec = RaDec[i][1],
                        Distand = prediction[i].Distand
                    });
                }
                
            }

            

            return list;
        }

        public event EventHandler<MouseEventArgs> StatusUpdate;
        public void OnStatusUpdate(MouseEventArgs e)
        {
            var handler = StatusUpdate;
            if (handler != null)
                handler(this, e);
        }

        //Don't forget to assign the method to MouseMove event in your user control 
        private void UserControl1_MouseMove(object sender, MouseEventArgs e)
        {
            OnStatusUpdate(e);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                
                dlg.Title = "Open Image";
                dlg.Filter = "jpg files (*.fits)|*.fits";
                dlg.InitialDirectory = @"C:\Users\specter\Desktop\Mongo\MongoDBControll\lib\Fits\";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                   this.path = dlg.FileName;
                    this.jsonfile.name = dlg.FileName.ToString();
                   textBox1.Text= dlg.FileName;
                    //MessageBox.Show(this.path);

                }
            }
        }


        private void configToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string text = String.Format("{0} \n {1} \n {2} \n {3}",this.database.ip, this.database.port, this.database.databasename, this.database.datacollection);
           
            MessageBox.Show(text,"Show");

        }

        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                this.database.Show();
                metroButton1.Enabled = true;

            }
            catch(ObjectDisposedException a)
            {
                MessageBox.Show(a.ToString());
            }



        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e) { }

        private void matrixBox1_Load(object sender, EventArgs e)
        {

        }

        private void imageBox1_Click(object sender, EventArgs e)
        {

        }

        private void raDecToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.RaDecPlot  = new ZedGrapScatter();
            this.RaDecPlot.Plot(kdt.Query(),this.RaDec);
            this.RaDecPlot.Show();

        }
        private void button1_Click(object sender, EventArgs e)
        {
            


        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
        
        

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            


        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog SaveFileDialog1 = new SaveFileDialog();
            SaveFileDialog1.InitialDirectory = @"C:\Users\specter\Desktop\Mongo\MongoDBControll\lib\Fits\";
            SaveFileDialog1.RestoreDirectory = true;
            SaveFileDialog1.Title = "Browse Text Files";
            SaveFileDialog1.DefaultExt = "json";
            SaveFileDialog1.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            
            if (SaveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string json = JsonConvert.SerializeObject(this.jsonfile);
                System.IO.File.WriteAllText(SaveFileDialog1.FileName, json); 
            }
        }
        private void Mylist()
        {
            listBox1.Items.Clear();
            foreach( var i in TyTask.log.ToList())
            {
                string formatter = String.Format("[{0}][{1}] : {2}",i.type,i.time,i.message);
                listBox1.Items.Add(formatter);
            }
            
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {

        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.database.ip) == false)
                toolStripStatusLabel1.Text = "Connected !!";
            toolStripStatusLabel2.Text = DateTime.Now.ToString();
            metroProgressBar1.Value = TyTask.progreebar1;
            metroProgressBar2.Value = TyTask.progreebar2;
            ImageProcess.Value = TyTask.imageprocess;
            progressBar3.Value = TyTask.progreebar3;
            DataBase1.Value = TyTask.database1;
            DataBase2.Value = TyTask.database2;
            DataBase3.Value = TyTask.database3;
            
            if (TyTask.statusbuttom)
            {
                    metroButton2.Enabled = true;
                    

             }
            Mylist();
            
            GC.Collect();


        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void imageBox1_Click_1(object sender, EventArgs e)
        {

        }
        private void dataGridViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DatagridView();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            TyTask.f = Convert.ToDouble(textBox2.Text);
            TyTask.p = Convert.ToDouble(textBox3.Text);
            var u = Task.Run(() =>
            {
                ImageProcessing();
                Plantwave(this.imageprocessing);
                DataBase();
                PlatConstan();
                KDTree();
                Json();
                platconstan = this.test.t;
                this.jsonfile.platconstan = platconstan;
            }).ContinueWith(t => TyTask.statusbuttom = true);

            
            
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            platconstan = this.test.t;

            MessageBox.Show(string.Format("{0} {1} {2} \n {3} {4} {5}", platconstan[0, 0].ToString(), platconstan[0, 1].ToString(), platconstan[0, 2], platconstan[1, 0], platconstan[1, 1], platconstan[1, 2]));
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void xYToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var t =new Performancount();
            t.Show();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click_1(object sender, EventArgs e)
        {
           
        }

        private void progressBar2_Click(object sender, EventArgs e)
        {

        }

        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void progressBar4_Click(object sender, EventArgs e)
        {

        }

        private void progressBar5_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void toolStripDropDownButton1_Click(object sender, EventArgs e)
        {
            
        }

        private void imageBox2_Click(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void toolStripDropDownButton1_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            try
            {
                catalogpath = String.Format(@"{0}UCAC4\Kepler\", e.ClickedItem.ToString());
                toolStripDropDownButton1.Text = catalogpath;
            }
            catch(Exception o)
            {
                MessageBox.Show(o.ToString(), "Exception Error");
            }
              

          
        }
    }
}

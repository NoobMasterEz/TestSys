using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AstrometryNova.PointingModel;
using Newtonsoft.Json;


namespace TestSys
{
    public partial class DataBaseMongo : Form
    {
        private JsonConfig json;

        public Mongolib database { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public string databasename { get; set; }
        public string datacollection { get; set; }
    
        public DataBaseMongo()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ip =string.Format("mongodb://{0}:", textBox1.Text);
            port = textBox2.Text;
            databasename = textBox3.Text;
            datacollection = textBox4.Text;
            this.database = new Mongolib(string.Format("{0}{1}", ip, port),databasename);
            this.database.NAMECOLLECTION = datacollection;
            
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
               
                dlg.Title = "Open Image";
                dlg.Filter = "json files (*.json)|*.json";
                dlg.InitialDirectory = @"C:\Users\specter\source\repos\TestSys\TestSys\";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                   json  = JsonConvert.DeserializeObject<JsonConfig>(File.ReadAllText(dlg.FileName)) ;
                   ip=textBox1.Text = string.Format("mongodb://{0}:", json.ip);
                   port = textBox2.Text=json.port;
                    databasename = textBox3.Text=json.databasename;
                    datacollection = textBox4.Text=json.datacollection;
                    this.database = new Mongolib(string.Format("{0}{1}", ip, port), databasename);
                    this.database.NAMECOLLECTION = datacollection;

                    this.Close();
                }
            }
        }
    }
}

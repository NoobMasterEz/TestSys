using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestSys
{
    public partial class Performancount : Form
    {
        PerformanceCounter perfcpu = new PerformanceCounter("Memory", "Available MBytes");


        public Performancount()
        {
            InitializeComponent();
           
      
        }
        

       

 
        private void Performancount_Load(object sender, EventArgs e)
        {
            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = "CPU use:" +(int)perfcpu.NextValue() + "";
            // Force garbage collection
            GC.Collect();
        }
    }
}

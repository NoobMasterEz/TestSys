using AstrometryNova.PointingModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestSys
{
    public partial class DataGrid : MetroFramework.Forms.MetroForm
    {
        public DataGrid()
        {
            InitializeComponent();
        }

        public void SetGrid(List<TypeRaDec> datagrid)
        {
            for (int i = 0; i < datagrid.Count; i++)
            {
                metroGrid1.Rows.Add(i.ToString(),datagrid[i].Distand, datagrid[i].X, datagrid[i].Y, datagrid[i].Ra, datagrid[i].Dec);
            }
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void metroGrid1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}

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
using ZedGraph;

namespace TestSys
{
    public partial class ZedGrapScatter : Form
    {
        private PointPairList spl1;
        private GraphPane graph;
        private PointPairList spl2;

        public ZedGrapScatter()
        {
            InitializeComponent();
            this.spl1 = new PointPairList();
            this.spl2 = new PointPairList();
            this.graph = zedGraphControl1.GraphPane;
        }

        private void ZedGrap_Load(object sender, EventArgs e)
        {
           

        }

        public void Plot(List<TypeKDTree> prediction, double[][] database)
        {
            this.graph.Title.Text = "DataBase Vs Prediction";
            foreach (var j in prediction)
            {
                spl1.Add(j.Nearest[0], j.Nearest[1]);
            }
            for (int i = 0; i < database.Length; i++)
            {
                spl2.Add(database[i][0], database[i][1]);
            }
            LineItem myCurve1 = this.graph.AddCurve("predict", spl1, Color.Blue, SymbolType.Plus);
            this.graph.AddCurve("Detection", spl2, Color.Red, SymbolType.Star).Line.IsVisible = false;
            myCurve1.Line.IsVisible = false;

            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();
            zedGraphControl1.Refresh();
        }
        private void chart1_Click(object sender, EventArgs e)
        {

        }
    }
}

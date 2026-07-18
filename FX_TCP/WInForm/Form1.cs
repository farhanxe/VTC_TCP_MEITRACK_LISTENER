using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FX_TCP
{
    public partial class Form1 : Form
    {

        string message;


        public Form1(string msg)
        {
            InitializeComponent();

            message = msg;


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = message;
        }
    }
}

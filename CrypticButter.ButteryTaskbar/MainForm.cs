using Microsoft.Win32;
using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CrypticButter.ButteryTaskbar
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();



            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            //Hide();
        }
    }
}

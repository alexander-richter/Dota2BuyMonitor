using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using HtmlAgilityPack;

namespace Dota2BuyMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            groupBoxOptions.Enabled = false;

            //init timer
            timer1.Enabled = true;
            timer1.Interval = Convert.ToInt16(numericUpDown1.Value);
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //prevent overload, if html request is not done yet..
            timer1.Stop();

            //start the backgroundworkers
            this.backgroundWorker1.RunWorkerAsync();

        }

        //declare needed vars
        List<string> items = new List<string>();
        string itemname;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            HtmlWeb web = new HtmlWeb();

            new WebClient();
            HtmlAgilityPack.HtmlDocument document = web.Load("http://www.dota2wh.com/dota/hero/Jugger");
            try
            {
                foreach (HtmlNode node in document.DocumentNode.SelectNodes("//li[@class='recent-item']"))
                {
                    this.itemname = node.InnerText.Trim();

                    //exclude unwanted and already known items
                    if (!items.Contains(itemname) && itemname != "Treasure Key")
                    {
                        items.Add(itemname);
                        richTextBox1.Text += (itemname) + "\r\n";
                    }

                }
            }
            catch
            {
                richTextBox1.Text += "Error at Itemselection\r\n";
            }

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

    }
}

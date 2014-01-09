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

            //add eventlisteners for backgroundworkers at the very beginning
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
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
            backgroundWorker1.RunWorkerAsync();

        }

        //declare needed vars
        List<string> items_old = new List<string>();
        List<string> items_new = new List<string>();
        List<string> items_final = new List<string>();
        
        bool firstTime = true;
        string itemname;


        

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //items list so that it only contains the new items
            items_new.Clear();

            //init htmlagilitypack things
            HtmlWeb web = new HtmlWeb();

            new WebClient();
            HtmlAgilityPack.HtmlDocument document = web.Load("http://www.dota2wh.com/dota/hero/Jugger");//using a custom url so that it needs only to load minimal things
            try
            {
                //get all recent items from WH webpage
                foreach (HtmlNode node in document.DocumentNode.SelectNodes("//li[@class='recent-item']"))
                {
                    this.itemname = node.InnerText.Trim();//get item name from html li node

                    //exclude unwanted items
                    if (itemname != "Treasure Key")
                    {
                        if (firstTime)
                        {
                            items_old.Add(itemname);
                        }
                        else
                        {
                            items_new.Add(itemname);
                        }
                    }
                }
            }
            catch
            {
                richTextBox1.Text += "Error getting/filtering itemlist\r\n";
            }


            if (firstTime)
            {
                items_final = items_old;
            }
            else
            {
                //exclude already known items
                //remove all old items from the final item list...only new items should be looked up
                items_final = items_new.ToList();
                items_final.Remove(items_old[0]);
                items_final.Remove(items_old[1]);
                items_final.Remove(items_old[2]);
                items_final.Remove(items_old[3]);
                items_final.Remove(items_old[4]);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            richTextBox1.Clear();

            foreach (string item_output in items_final)
            {
                richTextBox1.Text += item_output + "\r\n";
            }

            if (!firstTime)
            {
                items_old = items_new.ToList();
            }
            firstTime = false;
            groupBoxOptions.Enabled = true;
        }

    }
}

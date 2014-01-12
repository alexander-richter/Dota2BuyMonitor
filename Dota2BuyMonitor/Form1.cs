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
using System.Text.RegularExpressions;
using System.Diagnostics;

using System.Runtime.InteropServices;

namespace Dota2BuyMonitor
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            //add eventlisteners for backgroundworkers at the very beginning
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            richTextBox1.LinkClicked += new LinkClickedEventHandler(richTextBox1_LinkClicked);
            notifyIcon1.BalloonTipClicked += new EventHandler(notifyIcon1_BalloonTipClicked);
        }

        //global vars
        double quote_wanted;
        int gold_threshold;
        string itemlink;
        string allow_uncommons;

        //flashing Taskbar
        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        private void buttonStart_Click(object sender, EventArgs e)
        {
            //status strip info
            toolStripStatusLabel1.Text = "Monitoring Started";

            //init timer
            timer1.Enabled = true;

            //get all vars from options
            timer1.Interval = Convert.ToInt16(numericUpDownTimer.Value) * 1000;
            quote_wanted = Convert.ToDouble(numericUpDownQuote.Value);
            gold_threshold = Convert.ToInt16(numericUpDownGold.Value);

            //timer start
            timer1.Start();

            //enable..disable things
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
            groupBoxOptions.Enabled = false;

            //user option uncommons
            if (checkBoxUncommons.Checked == false)
            {
                allow_uncommons = "common";
            }
            else
            {
                allow_uncommons = "YES";
            }

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
            //status strip info
            toolStripStatusLabel1.Text = "Loading new Items...";

            //items list so that it only contains the new items
            items_new.Clear();

            //init htmlagilitypack things
            HtmlWeb web = new HtmlWeb();

            new WebClient();
            HtmlAgilityPack.HtmlDocument document = web.Load("http://www.dota2wh.com/dota/hero/Vengeful%20Spirit");//using a custom url so that it needs only to load minimal things
            try
            {
                //get all recent items from WH webpage
                foreach (HtmlNode node in document.DocumentNode.SelectNodes("//li[@class='recent-item']"))
                {
                    //get item name from html li node
                    this.itemname = node.InnerText.Trim();

                    //get link of item
                    var node_link = node.SelectSingleNode("a[@href]");
                    HtmlAttribute attribute_link = node_link.Attributes["href"];
                    string itemlink = attribute_link.Value.ToString();

                    //get item rarity of item
                    var node_rarity = node.SelectSingleNode(".//img[@class]");
                    HtmlAttribute attribute_class = node_rarity.Attributes["class"];
                    string itemrarity = attribute_class.Value.ToString();

                    string[] filterWordsArray = new string[] { "common", "uncommon", "rare", "mythical", "legendary", "immortal" };
                    //filter itemrarity (img class link)
                    foreach (string x in filterWordsArray)
                    {
                        if (itemrarity.Contains(x))
                        {
                            itemrarity = x;
                        }
                    }
                    

                    //exclude treasure key..
                    if (itemname != "Treasure Key" )
                    {
                        //add raw itemslist to list element
                        if (firstTime)
                        {
                            items_old.Add(itemname + "|" + itemlink + "|" + itemrarity);
                        }
                        else
                        {
                            items_new.Add(itemname + "|" + itemlink + "|" + itemrarity);
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
            //exclude already known items
            //remove all old items from the final item list...only new items should be looked up
            else
            {
                items_final = items_new.ToList();
                items_final.Remove(items_old[0]);
                items_final.Remove(items_old[1]);
                items_final.Remove(items_old[2]);
                items_final.Remove(items_old[3]);
                items_final.Remove(items_old[4]);
            }

            //look up exclusive items
            string gold;
            double currency = 1.0;
            double quote;

            foreach (string itemstring in items_final)
            {
                if (backgroundWorker1.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }

                //break up single itemstrings to array
                string[] itemdata = itemstring.Split('|');

                //no common & uncommon
                if (!itemdata[2].Contains(allow_uncommons))
                {
                    document = web.Load("http://www.dota2wh.com" + itemdata[1]);
                    try
                    {
                        HtmlNode node = document.DocumentNode.SelectSingleNode("//div[@class='label price']");
                        {
                            if (node != null)//if not already sold "not available"
                            {
                                //get gold amount calculating the buy quote
                                gold = Regex.Match(node.InnerText, @"\d+").Value;

                                //look up dota market place...only if gold amount big enough
                                if (Convert.ToInt16(gold) >= gold_threshold)
                                {
                                    document = web.Load("http://steamcommunity.com/market/listings/570/" + itemdata[0].Replace("&#39;", "%27"));
                                    try
                                    {
                                        //get the €_€
                                        node = document.DocumentNode.SelectSingleNode("//span[@class='market_listing_price market_listing_price_with_fee']");
                                        if (node != null)
                                        {
                                            //all currencies...
                                            if (node.InnerText.Contains("USD"))
                                            {
                                                currency = 0.0074;
                                            }
                                            else if (node.InnerText.Contains("p&#1091;&#1073;."))
                                            {
                                                currency = 0.0223;
                                            }
                                            else if (node.InnerText.Contains("&#163;"))
                                            {
                                                currency = 0.01215;
                                            }
                                            else if (node.InnerText.Contains("&#82;"))
                                            {
                                                currency = 0.31;
                                            }
                                            else
                                            {
                                                currency = 1.0;
                                            }
                                            //price to €€
                                            double price = Convert.ToDouble(node.InnerText.Replace(" ", "").Replace("p&#1091;&#1073;.", "").Replace("&#36;", "").Replace("USD", "").Replace("&#8364;", "").Replace("&#163;", "").Replace("&#82;", "").Replace("-", "0").Trim()) * currency;
                                            //calc the buyóut quote
                                            quote = (price / (Convert.ToInt32(gold))) * 1000.0;
                                            if (quote > quote_wanted)
                                            {
                                                //data to text
                                                itemlink = "http://www.dota2wh.com" + itemdata[1];
                                                string iteminfo = string.Concat(new object[] { string.Format("{0:f2}", price), "€  (", string.Format("{0:f2}", quote), ")"});

                                                richTextBox1.AppendText(itemdata[0].Replace("&#39;", "'") + " [" + itemdata[2] + "]\r\n");//itemname
                                                richTextBox1.AppendText(iteminfo + "\r\n");//item info. Price..Quote..
                                                richTextBox1.AppendText(itemlink + "\r\n\r\n");//itemlink to wh

                                                //user options
                                                if (checkBoxNotify.Checked == true)
                                                {
                                                    notifyIcon1.BalloonTipText = itemdata[0].Replace("&#39;", "'") + " " + iteminfo;
                                                    notifyIcon1.ShowBalloonTip(3000);
                                                    System.Media.SystemSounds.Asterisk.Play();//play notify sound
                                                    //flash Taskbar
                                                    IntPtr handle = this.Handle;
                                                    FlashWindow(handle, false);
                                                }
                                                if (checkBoxBrowser.Checked == true)
                                                {
                                                    Process.Start(itemlink);
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        richTextBox1.Text += "Error Market";
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        richTextBox1.Text += "Error WH Single Item Site";
                    }
                }
            }
        }

        //search completed, start timer again
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //status strip info
            toolStripStatusLabel1.Text = "Refreshing";

            timer1.Start();

            //add already checked items to exclude for new test
            if (!firstTime)
            {
                items_old = items_new.ToList();
            }
            firstTime = false;
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            //open browser with link rtf field
            Process.Start(e.LinkText);
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            //open browser with link
            Process.Start(itemlink);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            //status strip info
            toolStripStatusLabel1.Text = "Monitoring Stopped";

            //disable timer
            timer1.Enabled = false;

            //timer stop
            timer1.Stop();

            //enable..disable
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            groupBoxOptions.Enabled = true;

            //refresh vars
            firstTime = true;
            items_old.Clear();
            items_new.Clear();
            items_final.Clear();
        }


    }
}

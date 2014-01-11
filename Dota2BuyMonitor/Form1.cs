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
                    

                    //exclude unwanted items
                    if (itemname != "Treasure Key")
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
            double quote_wanted = 5.0;

            richTextBox1.Clear();

            foreach (string itemstring in items_final)
            {
                //break up single itemstrings to array
                string[] itemdata = itemstring.Split('|');

                //no common & uncommon
                if (!itemdata[2].Equals("common") && !itemdata[2].StartsWith("u"))
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
                                //MessageBox.Show(gold);

                                //look up dota market place...
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
                                        quote = (price / ((double)Convert.ToInt32(gold))) * 1000.0;
                                        if (quote > quote_wanted)
                                        {
                                            richTextBox1.Text += itemdata[0].Replace("&#39;", "%27") + "\r\n";//itemname
                                            richTextBox1.Text += string.Concat(new object[] {string.Format("{0:f2}", price), "€  (", string.Format("{0:f2}", quote), ")\r\n"});//item info. Price..Quote..
                                            richTextBox1.Text += "http://www.dota2wh.com" + itemdata[1] + "\r\n\r\n";//itemlink to wh
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
                    catch
                    {
                        richTextBox1.Text += "Error WH Single Item Site";
                    }
                }
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (string item_output in items_final)
            {
                //richTextBox1.Text += item_output + "\r\n";
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

using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.Text.RegularExpressions;
using Npgsql;

namespace Scraper
{
    class Program
    {
        static ScrapingBrowser _browser = new ScrapingBrowser();
        static void Main(string[] args)
        {
            //You can select the tab of the Price Charting site where you will extract the data from here.
            var mainPageLinks = GetMainPageLinks("https://www.pricecharting.com/console/nes");
        }

        static List<dynamic> GetMainPageLinks(string url)
        {
            //Here, x selects every 50 items and provides progress.
            int x = 1100;

            //Here I am pulling the product information according to the css structure of the page and writing it to a list.
            List<dynamic> linksList = new List<dynamic>();
            for (int i = 0; i <= x;)
            {
                var html = GetHtml("https://www.pricecharting.com/console/nes?sort=&cursor=" + i);

                var links = html.CssSelect("tr");

                foreach (var link in links)
                {
                        Product product = new Product();
                        try
                        {
                            product.title = link.InnerHtml.Split(" </a>")[0].Split(">")[2].Replace("\n", "").Trim().ToString();
                        }
                        catch (Exception){}
                        try
                        {
                            product.LoosePrice = link.InnerHtml.Split("used_price")[1].Split("</span")[0].Split(">$")[1].ToString();
                        product.LoosePrice.Replace(",", "").Replace(".", ",");
                        }
                        catch (Exception) { }
                        try
                        {
                            product.CIBPrice = link.InnerHtml.Split("cib_price")[1].Split("</span")[0].Split(">$")[1].ToString();
                        product.CIBPrice.Replace(",", "").Replace(".", ",");
                        }
                        catch (Exception) { }
                        try
                        {
                            product.NewPrice = link.InnerHtml.Split("new_price")[1].Split("</span")[0].Split(">$")[1].ToString();
                        product.NewPrice.Replace(",", "").Replace(".", ",");
                        }
                        catch (Exception) { }                       

                        linksList.Add(product);  
                }

                i = i + 50;
            }


            //The data in the created list is being written to PostgreSQL.
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=localhost;Username=postgres;Password=postgres;Database=postgres"))
            {
                connection.Open();
                NpgsqlCommand cmd = new NpgsqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = "truncate table pc_product";
                cmd.ExecuteNonQuery();
                linksList.ForEach(x =>
                    {
               

                                cmd.CommandText = @$"insert into pc_product (title,looseprice,cibprice,newprice,created_on) values('{x.title.ToString()}',{Convert.ToDouble(x.LoosePrice)},{Convert.ToDouble(x.CIBPrice)},{Convert.ToDouble(x.NewPrice)},current_timestamp)";                               
                                cmd.ExecuteNonQuery();

                    });
                cmd.Dispose();
                connection.Close();
            };
            //Finally, I give the list as output. Anyone can output the data in csv, xls or any other format they want.
            return linksList;
        }

        static HtmlNode GetHtml(string url)
        {
            WebPage webpage = _browser.NavigateToPage(new Uri(url));
            return webpage.Html;
        }



        public class Product
        {
            public string? title { get; set; }
            public string LoosePrice { get; set; }
            public string CIBPrice { get; set; }
            public string NewPrice { get; set; }
        }
    }
}
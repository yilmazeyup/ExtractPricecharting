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
using System.Net;
using Newtonsoft.Json;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace Scraper
{
    class Program
    {
        static ScrapingBrowser _browser = new ScrapingBrowser();

        static void Main(string[] args)
        {
            var mainPageLinks = GetMainPageLinks("https://www.google.com/");
        }

        static List<dynamic> GetMainPageLinks(string url)
        {
            int productCount = 4000;
            List<List<string>> categories = new List<List<string>>();
            var startDate = DateTime.Now;

            List<ProductViewModel> productViewModel = new List<ProductViewModel>();
            List<string> category1 = new List<string> { "nes", "super-nintendo" };
            categories.Add(category1);
            List<string> category2 = new List<string> { "gameboy", "gameboy-color" };
            categories.Add(category2);
            List<string> category3 = new List<string> { "playstation", "playstation-2" };
            categories.Add(category3);
            List<string> category4 = new List<string> { "sega-genesis", "sega-dreamcast" };
            categories.Add(category4);
            List<string> category5 = new List<string> { "wii", "nintendo-switch" };
            categories.Add(category5);
            List<string> category6 = new List<string> { "xbox-one", "gamecube" };
            categories.Add(category6);
            List<string> category7 = new List<string> { "nintendo-64", "xbox" };
            categories.Add(category7);
            List<string> category8 = new List<string> { "gameboy-advance", "psp" };
            categories.Add(category8);
            List<string> category9 = new List<string> { "playstation-3", "xbox-360" };
            categories.Add(category9);
            List<string> category10 = new List<string> { "nintendo-ds", "playstation-4" };
            categories.Add(category10);
            List<string> category11 = new List<string> { "nintendo-3ds", "playstation-5" };
            categories.Add(category11);

            var eBayPrice = "";
            var amazonPrice = "";
            var image = "";
            var upcCode = "";

            List<dynamic> linksList = new List<dynamic>();

            categories.ForEach(subcategories =>
            {


                subcategories.ForEach(category =>
                {
                    Console.WriteLine(category);
                    int productArrangement = 0;
                    for (int perPage = 0; perPage <= productCount;)
                    {



                        var client = new RestClient("https://www.pricecharting.com");
                        var request = new RestRequest($"console/{category}", Method.Get);
                        request.AddParameter("format", "json");
                        request.AddParameter("cursor", perPage.ToString());
                        request.AddParameter("sort", "");
                        var response = client.Execute(request);
                        if (response.StatusCode.ToString() == "InternalServerError")
                        {
                            client = new RestClient("https://www.pricecharting.com");
                            request = new RestRequest($"console/{category}", Method.Get);
                            request.AddParameter("format", "json");
                            request.AddParameter("cursor", perPage.ToString());
                            request.AddParameter("sort", "");
                            response = client.Execute(request);
                        }
                        JObject joResponse = JObject.Parse(response.Content);
                        JArray productsArray = (JArray)joResponse["products"];
                        List<dynamic> productsList = new List<dynamic>(productsArray);

                        if (productsList.Count > 0)
                        {
                            try
                            {

                                //Parallel.ForEach(productsList, product =>
                                foreach (var product in productsList)
                                {
                                    var upcUrl = "https://www.pricecharting.com/game/" + product.consoleUri.ToString() + "/" + product.productUri.ToString();
                                    var upcHtml = GetDetailHtml(upcUrl);
                                    try
                                    {
                                        eBayPrice = upcHtml.InnerHtml.Split("Complete Price")[1]?.Split("eBay")[2]?.Split("</span")[0]?.Split(">$")[1]?.ToString()?.Replace(",", "");
                                    }
                                    catch (Exception) {
                                        eBayPrice ="";
                                    }
                                    try
                                    {
                                        amazonPrice = upcHtml.InnerHtml.Split("Complete Price")[1]?.Split("Amazon")[2]?.Split("</span")[0]?.Split(">$")[1]?.ToString()?.Replace(",", "");
                                    }
                                    catch (Exception)  {
                                        amazonPrice = "";
                                    }
                                    try
                                    {
                                        image = upcHtml.InnerHtml.Split("cover\">\n            \n            <img src='")[1].Split("' alt")[0];
                                    }
                                    catch (Exception)
                                    {
                                        image = "";
                                    }
                                    try
                                    {
                                        upcCode = upcHtml.InnerHtml.Split("UPC:")[1].Split("</td>")[1].Split("</td>")[0].Split(">")[1].Replace("\n", "").Trim().ToString();
                                    }
                                    catch
                                    {
                                        upcCode = "";

                                    }
                                    productViewModel.Add(new ProductViewModel
                                    {
                                        consoleUri = product.consoleUri.ToString(),
                                        hasProduct = product.hasProduct.ToString(),
                                        id = product.id.ToString(),
                                        LoosePrice = product.price1.ToString().Replace("$", "").ToString(),
                                        CIBPrice = product.price2.ToString().Replace("$", "").ToString(),
                                        NewPrice = product.price3.ToString().Replace("$", "").ToString(),
                                        priceChange = product.priceChange.ToString(),
                                        priceChangePercentage = product.priceChangePercentage.ToString(),
                                        priceChangeSign = product.priceChangeSign.ToString(),
                                        productName = product.productName.ToString(),
                                        productUri = product.productUri.ToString(),
                                        showCollectionLinks = product.showCollectionLinks.ToString(),
                                        wishlistHasProduct = product.wishlistHasProduct.ToString(),
                                        upcCode = upcCode.ToString(),
                                        eBayPrice = eBayPrice?.ToString(),
                                        amazonPrice = amazonPrice?.ToString(),
                                        image = image.ToString(),
                                        category = category.ToString(),
                                    });
                                    productArrangement = productArrangement + 1;
                                };
                            }
                            catch
                            {
                                
                            }

                        }

                        perPage = perPage + 50;
                    };

                });



            });


            Console.WriteLine(startDate);
            Console.WriteLine(DateTime.Now);


            //The data in the created list is being written to PostgreSQL.
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=localhost;Username=postgres;Password=12345;Database=postgres"))
            {
                connection.Open();
                productViewModel.ForEach(x=>
                {
                    using (var cmd = new NpgsqlCommand(@"insert into pricecharting (title,looseprice,cibprice,newprice,upcCode,ebayprice,amazonprice,category,image,createdon,hasproduct,updatedon,pricechartingid) 
                                    values(:title,:loosePrice,:cibPrice,:newPrice,:upcCode,:ebayPrice,:amazonPrice,:category,:image,current_timestamp,:hasProduct,current_timestamp,:priceChartingId)
                                    on conflict(priceChartingId,category)
                                    do update set
                                    looseprice = :loosePrice ,
                                    cibprice = :cibPrice ,
                                    newprice = :newPrice ,
                                    ebayprice = :ebayPrice ,
                                    amazonprice = :amazonPrice,
                                    hasproduct = :hasProduct ", connection))
                    {
                        cmd.Parameters.AddWithValue("title", x.productName.ToString() == null ? "" : x.productName.ToString());
                        cmd.Parameters.AddWithValue("loosePrice", x.LoosePrice == "" ? 0 : Convert.ToDouble(x.LoosePrice));
                        cmd.Parameters.AddWithValue("cibPrice", x.CIBPrice == "" ? 0 : Convert.ToDouble(x.CIBPrice));
                        cmd.Parameters.AddWithValue("newPrice", x.NewPrice == "" ? 0 : Convert.ToDouble(x.NewPrice));
                        cmd.Parameters.AddWithValue("upcCode", x.upcCode.ToString() == "" ? 0 : x.upcCode.ToString());
                        cmd.Parameters.AddWithValue("ebayPrice", x.eBayPrice == "" ? 0 : Convert.ToDouble(x.eBayPrice));
                        cmd.Parameters.AddWithValue("amazonPrice", x.amazonPrice == "" ? 0 : Convert.ToDouble(x.amazonPrice));
                        cmd.Parameters.AddWithValue("category", x.category.ToString());
                        cmd.Parameters.AddWithValue("image", x.image?.ToString() == null ? "" : x.image?.ToString());
                        cmd.Parameters.AddWithValue("hasProduct", x.hasProduct?.ToString() == null ? "" : x.hasProduct?.ToString());
                        cmd.Parameters.AddWithValue("priceChartingId", x.id?.ToString() == null ? 0 : Convert.ToDouble(x.id));
                        cmd.ExecuteNonQuery();

                    }



                });
                connection.Close();
            };
            return linksList;
        }

 

        static HtmlNode GetDetailHtml(string upcUrl)
        {
            WebPage webpage = _browser.NavigateToPage(new Uri(upcUrl));
            return webpage.Html;
        }

     
    }
}
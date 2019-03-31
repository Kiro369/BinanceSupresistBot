using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BinanceSupresistBot
{
    class Program
    {
        public static OrderBook OrderBook;

        static void Main(string[] args)
        {
            Console.Title = "Binance Support & Resistance Identifier [Created by Kiro]";

        theBeginning:

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Please enter a pair (for example BTCUSDT) : ");

            Console.ForegroundColor = ConsoleColor.White;
            var pair = Console.ReadLine().ToUpper();

            if (!GetOrderBook(pair))
                goto theBeginning;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("I'm waiting for you commands, type help if you don't know the available commands");

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                var cmd = Console.ReadLine().ToLower().Split(' ').Where(c => !string.IsNullOrEmpty(c)).ToArray();
                switch (cmd[0])
                {
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "resistance":
                        Console.ForegroundColor = ConsoleColor.Red;
                        var min = decimal.Parse(cmd[1]);
                        var max = decimal.Parse(cmd[2]);
                        decimal sum = 0;
                        foreach (var ask in OrderBook.Asks)
                        {
                            if (ask.Price >= min && ask.Price <= max)
                                sum += ask.Quantity;
                        }
                        Console.WriteLine("Resistance is " + sum);
                        break;
                    case "support":
                        Console.ForegroundColor = ConsoleColor.Green;
                        min = decimal.Parse(cmd[1]);
                        max = decimal.Parse(cmd[2]);
                        sum = 0;
                        foreach (var bid in OrderBook.Bids)
                        {
                            if (bid.Price >= min && bid.Price <= max)
                                sum += bid.Quantity;
                        }
                        Console.WriteLine("Support is " + sum);
                        break;
                    case "refresh":
                        {
                            GetOrderBook(pair);
                            break;
                        }
                    case "help":
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Available commands : ");
                            Console.WriteLine("support min max => gets you the support between min and max");
                            Console.WriteLine("resistance min max => gets you the resistance between min and max");
                            Console.WriteLine("resistance min max => gets you the resistance between min and max");
                            Console.WriteLine("refresh => refreshes the orderbook");
                            Console.WriteLine("Note that : the minimum and maximum should be between the ranges written above");
                            break;
                        }
                    default:
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine("Unknown command, please write help for more info about the available commands");
                            break;
                        }
                }
            }
        }

        private static bool GetOrderBook(string pair)
        {

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Sending the request to Binance depth API...");

            var address = "https://www.binance.com/api/v1/depth?symbol=" + pair + "&limit=1000";

            // Send depth web request to Binance to get the order book with the max limit (1000)
            var request = (HttpWebRequest)WebRequest.Create(address);

            // Get the requests's response
            Console.WriteLine("Getting the response...");
            string json = string.Empty;
            try
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (request.HaveResponse && response != null)
                    {
                        Console.WriteLine("Creating response reader...");
                        // Creating a stream reader to read the response 
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            json = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        Console.WriteLine("Creating response reader...");
                        // Creating a stream reader to read the response 
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            json = reader.ReadToEnd();
                            if (JObject.Parse(json).ContainsKey("msg"))
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine((string)JObject.Parse(json)["msg"]);
                                Console.WriteLine("Please try again!");
                                return false;
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Reading & parsing the resposne...");
            // Read the response to end, get it as a dynamic object, parse it
            OrderBook = GetParsedOrderBook(JsonConvert.DeserializeObject(json));

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Range for supports available is [" + OrderBook.Bids.Min(o => o.Price) + " , " + OrderBook.Bids.Max(o => o.Price) + "]");
            Console.WriteLine("Range for resistances available is [" + OrderBook.Asks.Min(o => o.Price) + " , " + OrderBook.Asks.Max(o => o.Price) + "]");
            return true;
        }

        public static OrderBook GetParsedOrderBook(dynamic orderBookData)
        {
            var result = new OrderBook
            {
                LastUpdateId = orderBookData.lastUpdateId.Value
            };

            var bids = new List<Order>();
            var asks = new List<Order>();

            foreach (JToken item in ((JArray)orderBookData.bids).ToArray())
            {
                bids.Add(new Order() { Price = decimal.Parse(item[0].ToString()), Quantity = decimal.Parse(item[1].ToString()) });
            }

            foreach (JToken item in ((JArray)orderBookData.asks).ToArray())
            {
                asks.Add(new Order() { Price = decimal.Parse(item[0].ToString()), Quantity = decimal.Parse(item[1].ToString()) });
            }

            result.Bids = bids;
            result.Asks = asks;

            return result;
        }

    }
    public class Order
    {
        public decimal Price, Quantity;
    }
    public class OrderBook
    {
        public long LastUpdateId;
        public List<Order> Bids, Asks;
    }
}

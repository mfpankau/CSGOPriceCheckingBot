using System;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Runtime.CompilerServices;

namespace CaseProfitCalc
{
    class Program
    {
        static DiscordClient discord;

        //token
        // NzYwNTgyNjk2MTE5OTU5NTgy.X3OJ1Q.G5dqwPPEkGIDuM1W_LLv3Rn_9uM

        static void Main(string[] args)
        {

            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        }
        //start bot and stuff
        static async Task MainAsync(string[] args)
        {
            string data = "";
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = "<Token>",
                TokenType = TokenType.Bot
                
            });
            //if message created
            discord.MessageCreated += async e =>
            {
                //~~price command
                if (e.Message.Content.ToLower().StartsWith("~~price"))
                    if (!e.Message.Author.IsBot)
                    {
                        data = FindPrice(e.Message.Content.ToLower().Replace("~~price ", ""), false);
                        Console.WriteLine("Price: " + e.Message.Content);
                        await e.Message.RespondAsync(data);
                    }
                //~~average command
                if (e.Message.Content.ToLower().StartsWith("~~average"))
                    if (!e.Message.Author.IsBot)
                    {
                        var averagePrices = FindPrice(e.Message.Content.ToLower().Replace("~~average ", ""), true);
                        averagePrices.Replace(",", ".");
                        Console.WriteLine("Average: " + e.Message.Content);
                        await e.Message.RespondAsync("Average profit if you pull the " + e.Message.Content.Replace("~~average ", "") + ": "  + "$" + averagePrices);
                    }
            };
            //create status
            discord.Ready += async x =>
            {
                DiscordGame game = new DiscordGame("The CS:GO Economy");
                await discord.UpdateStatusAsync(game);
            };

            //start bot
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        //find price method
        public static string FindPrice(string name, bool profit)
        {
            //replaces whitespace in name to create url
            name.Replace(" ", "+");
            //loads steam community market web page for specified name
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load($"https://steamcommunity.com/market/search?appid=730&q={name}");


            //arrays that hold prices and names
            HtmlNode[] nodes = new HtmlNode[10];
            HtmlNode[] names = new HtmlNode[10];

            for (int i = 0; i < 10; i++)
            {

                try
                {
                    //find names and prices
                    nodes[i] = doc.DocumentNode.SelectNodes($"//*[@id=\"result_{i}\"]/div[1]/div[2]/span[1]/span[1]").FirstOrDefault();
                    names[i] = doc.DocumentNode.SelectNodes($"//*[@id=\"result_{i}_name\"]").FirstOrDefault();
                }
                catch
                {
                    //if nodes result in a null, it skips the item(in case theres no results or no stattrack, etc)
                    continue;
                }


            }
            string allText = "";
            for (int x = 0; x < 10; x++)
            {
                //again, if none, skips
                if (nodes[x] == null)
                {
                    continue;
                }
                //adds each value to final return
                allText += ("\n" + nodes[x].InnerText + "   " + names[x].InnerText);

                

            }



            if(profit)
            {

                string[] prices = new string[nodes.Length];
                string[] nameTexts = new string[nodes.Length];

                for (int i = 0; i < 10; i++)
                {

                    if (nodes[i] == null)
                    {
                        prices[i] = "";
                        nameTexts[i] = "";
                        continue;
                    }
                    else
                    {
                        prices[i] = nodes[i].InnerText;
                        nameTexts[i] = names[i].InnerText;
                    }

                    //Console.WriteLine(nameTexts[i]);
                }
                
                return ProfitCalc(prices, nameTexts).ToString();
            }


            //returns text or error if something went wrong
            if (allText != "")
            {

                return allText;

            }
            else
            {
                return "error: something went wrong! probably couldn't find the item!";
            }

        }

        //find average profit method
        public static double ProfitCalc(string[] prices, string[] names)
        {
            //setup shit
            double[] weights = new double[names.Length];
            string[] priceTexts = new string[names.Length];
            double average = 0;

            //cycles through each item
            for (int i = 0; i < names.Length; i++)
            {
                //conversions and more setup
                priceTexts[i] = prices[i];

                weights[i] = 1;
                //if name has stattrak, then it has extra lowered weight
                if(names[i].Contains("StatTrak"))
                {
                    weights[i] *= 0.1;
                }
                //skip empty items
                if(names[i] == "")
                {
                    continue;
                }

                //add weight based on grade
                if (names[i].Contains("Well-Worn"))
                {
                    weights[i] *= 7.92;
                }
                if (names[i].Contains("Battle-Scarred"))
                {
                    weights[i] *= 9.93;
                }
                if (names[i].Contains("Field-Tested"))
                {
                    weights[i] *= 43.18;
                }
                if (names[i].Contains("Minimal Wear"))
                {
                    weights[i] *= 24.68;
                }
                if (names[i].Contains("Factory New"))
                {
                    weights[i] *= 14.71;
                }
                //calculate average profit
                priceTexts[i] = priceTexts[i].Replace("$", "");
                priceTexts[i] = priceTexts[i].Replace(".", ",");
                average += (Convert.ToDouble(priceTexts[i]) * weights[i]) / 100;
            }
            //return final answer formatted
            return Math.Round(average, 2);
        }

    }
}


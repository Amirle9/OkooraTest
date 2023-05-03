using CsvHelper;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OkooraTest
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var pairs = new List<ExchangePair>
            {
                new ExchangePair("USD/ILS", "https://www.xe.com/currencyconverter/convert/?Amount=1&From=USD&To=ILS"),
                new ExchangePair("GBP/EUR", "https://www.xe.com/currencyconverter/convert/?Amount=1&From=GBP&To=EUR"),
                new ExchangePair("EUR/JPY", "https://www.xe.com/currencyconverter/convert/?Amount=1&From=EUR&To=JPY"),
                new ExchangePair("EUR/USD", "https://www.xe.com/currencyconverter/convert/?Amount=1&From=EUR&To=USD")
            };

            // Create a Chrome driver service
            var service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;

            // Configure Chrome options
            var options = new ChromeOptions();
            options.AddArguments("--headless"); // Run Chrome in headless mode
            options.AddArguments("--disable-gpu"); // Disable the GPU for performance
            options.AddArguments("--disable-extensions"); // Disable extensions for stability
            options.AddArgument("--log-level=3");


            // Use the Chrome driver to fetch exchange rates for each pair
            using (var driver = new ChromeDriver(options))
            {
                await FetchExchangeRatesAsync(pairs, driver);

                Console.WriteLine("Current exchange rates:");

                foreach (var pair in pairs)
                {
                    Console.WriteLine($"{pair.Name}: {pair.Value} (updated at {pair.Date})");
                }
            }

            Console.ReadLine();
        }

        // Fetches the exchange rates for a list of pairs
        static async Task FetchExchangeRatesAsync(List<ExchangePair> pairs, IWebDriver driver)
        {
            string csvFilePath = "exchange_rates.csv";
            bool fileExists = File.Exists(csvFilePath);

            // Write exchange rates to a CSV file
            using (var writer = new StreamWriter(csvFilePath, true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                if (!fileExists)
                {
                    csv.WriteField("Name");
                    csv.WriteField("Value");
                    csv.WriteField("Date");
                    csv.NextRecord();
                }

                foreach (var pair in pairs)
                {
                    try
                    {
                        driver.Navigate().GoToUrl(pair.Url); // Navigate to the pair's URL and wait for the page to fully load
                        await Task.Delay(5000); // Wait for the page to fully load and update the data
                        driver.Navigate().Refresh();
                        var valueElement = driver.FindElement(By.XPath("//p[@class='result__BigRate-sc-1bsijpp-1 iGrAod']"));

                        // Extract the exchange rate value and current date
                        pair.Value = new string(valueElement.Text.Where(x => x == '.' || Char.IsDigit(x)).ToArray());
                        pair.Date = DateTime.Now.ToString("yyyy-MM-dd");

                        csv.WriteField(pair.Name);
                        csv.WriteField(pair.Value);
                        csv.WriteField(pair.Date);
                        csv.NextRecord();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to fetch {pair.Name}: {ex.Message}");
                    }
                }
            }
        }


    }
    class ExchangePair
    {
        public string Name { get; }
        public string Url { get; }
        public string Value { get; set; }
        public string Date { get; set; }

        public ExchangePair(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }
}

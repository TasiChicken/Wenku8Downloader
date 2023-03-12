﻿using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Wenku8Downloader
{
    public partial class Form2 : Form
    {
        #region Crawlers

        internal static HttpClient client = new HttpClient();
        
        private static IWebDriver _driver;
        private static IWebDriver driver
        {
            get
            {
                if (_driver == null) initSelenium();
                return _driver;
            }
            set { _driver = value; }
        }
        
        private static void initSelenium()
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--headless=new");

            options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);

            driver = new ChromeDriver(service, options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
        }
        
        private static void disposeSelenium()
        {
            if (_driver == null) return;
            try { driver.Close(); }
            catch { }
            try { driver.Quit(); }
            catch { }
            try { driver.Dispose(); }
            catch { }
        }

        #endregion

        #region Public API

        public static string[] GetIndexes(Form1 form1, int bookcase)
        {
            new Form2(form1, bookcase).ShowDialog();
            return indexes;
        }

        public static void Download(string[] indexes, string folder, string encoding, bool separate)
        {
            Form2.indexes = indexes;
            new Form2(folder, encoding, separate).ShowDialog();
        }

        public static void ConvertToMobi(Form1 form1, string[] indexes, string folder, bool separate)
        {
            Form2.indexes = indexes;
            new Form2(form1, folder, separate).ShowDialog();
        }

        public static void dispose()
        {
            disposeSelenium();
            client.Dispose();
        }

        #endregion

        #region Fields

        private string folder;
        private Form1 form1;
        private static string[] indexes;
        private bool separate;
        private Action action;
        private bool running = true;

        #endregion

        #region Events

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            running = false;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            label2.Text = this.Text;
            action();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion
        
        #region Download

        private string encoding;

        private Form2(string folder, string encoding, bool separate)
        {
            this.folder = folder;
            this.encoding = encoding;
            this.separate = separate;
            action = downloadBooks;

            InitializeComponent();

            this.Text = "Download Books";
        }
        
        private async void downloadBooks()
        {
            progressBar1.Maximum = indexes.Length;
            for (int i = 0; i < indexes.Length; i++)
            {
                if (!running) return;

                label1.Text = $"Running {i}/{indexes.Length}";
                progressBar1.Value = i;
                await downLoadImgAndTxt(indexes[i]);
            }
            this.Close();
        }

        private async Task downLoadImgAndTxt(string index)
        {
            char head = index.Length < 4 ? '0' : index[0];

            string location = folder + (separate ? $"//{index}" : null);
            Directory.CreateDirectory(location);
            
            try
            {
                byte[] imgData = await client.GetByteArrayAsync($"https://img.wenku8.com/image/{head}/{index}/{index}s.jpg");
                File.WriteAllBytes(location + $"//{index}.jpg", imgData);
            }
            catch { Form1.log.AppendLine($"{DateTime.Now}\t:\t{index}-image"); }

            try
            {
                byte[] bytes = await client.GetByteArrayAsync($"https://dl1.wenku8.com/txt{encoding}/{head}/{index}.txt");
                string txtContent = Encoding.GetEncoding("gbk").GetString(bytes);
                File.WriteAllText(location + $"//{index}.txt", txtContent);
            }
            catch { Form1.log.AppendLine($"{DateTime.Now}\t:\t{index}-txt"); }
        }

        #endregion

        #region GetIndexes

        int bookcase;

        private Form2(Form1 form1, int bookcase)
        {
            this.form1 = form1;
            this.bookcase = bookcase;
            action = startGet;

            InitializeComponent();

            this.Text = "Search Books in Bookcase";
        }

        private void startGet()
        {
            Task.Run(getIndexes);
        }

        private async Task getIndexes()
        {
            string bookcaseUrl = $@"https://www.wenku8.net/modules/article/bookcase.php?classid={bookcase}";
            driver.Navigate().GoToUrl(bookcaseUrl);
            int i = 0;

            if (driver.Url != bookcaseUrl)
            {
                for (i = 0; i < 2; i++)
                {
                    var tb = driver.FindElement(By.XPath($@"/html/body/div[4]/div/div/form/table/tbody/tr[1]/td/table/tbody/tr[{i + 1}]/td[2]/input"));
                    tb.SendKeys(form1.accountInfoTbs[i].Text);
                }
                driver.FindElement(By.XPath(@"/html/body/div[4]/div/div/form/table/tbody/tr[1]/td/table/tbody/tr[4]/td[2]/input")).Click();
                driver.Navigate().GoToUrl(bookcaseUrl);
            }

            List<string> list = new List<string>();

            Action upLbl = () => label1.Text = $"Running {i - 2}/Unknown"; ;
            Action upPgB = () => progressBar1.Value = i - 2;

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);

            for (i = 2; ; i++)
            {
                if (!running) break;

                this.Invoke(upLbl);
                this.Invoke(upPgB);

                try
                {
                    var element = driver.FindElement(By.XPath($@"/html/body/div[5]/div[2]/div/form/table/tbody/tr[{i}]/td[2]/a"));
                    string href = element.GetAttribute("href");
                    int start = href.IndexOf("aid=") + 4;
                    list.Add(href.Substring(start, href.IndexOf("&bid") - start));

                    if (!form1.saveMobi) continue;

                    form1.names.Add(element.Text);
                    element = driver.FindElement(By.XPath($@"/html/body/div[5]/div[2]/div/form/table/tbody/tr[{i}]/td[3]/a"));
                    form1.authors.Add(element.Text);
                }
                catch { break; }
            }

            indexes = list.ToArray();

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
            this.Invoke((Action)(() => this.Close()));
        }

        #endregion

        #region ConvertToMobi

        private Form2(Form1 form1, string folder, bool separate)
        {
            this.form1 = form1; 
            this.folder = folder;
            this.separate = separate;
            action = convertBooks;

            InitializeComponent();

            this.Text = "Convert Books to MOBI Files";
        }

        private async void convertBooks()
        {
            progressBar1.Maximum = indexes.Length;

            for (int i = 0; i < indexes.Length; i++)
            {
                if (!running) return;

                label1.Text = $"Running {i}/{indexes.Length}";
                progressBar1.Value = i;

                await convertABook(indexes[i], form1.names[i], form1.authors[i]);
            }

            this.Close();
        }

        private async Task convertABook(string index, string name, string author)
        {
            string location = folder + (separate ? $"//{index}" : null);

            try
            {
                driver.Navigate().GoToUrl("https://ebook.cdict.info/mobi/#google_vignette");

                driver.FindElement(By.XPath(@"/html/body/div[1]/div[4]/div/center[1]/table/tbody/tr[2]/td/input")).SendKeys(name);
                driver.FindElement(By.XPath(@"/html/body/div[1]/div[4]/div/center[1]/table/tbody/tr[3]/td/input")).SendKeys(author);
                driver.FindElement(By.XPath(@"/html/body/div[1]/div[4]/div/center[1]/table/tbody/tr[1]/td/input[1]")).SendKeys($@"{location}/{index}.txt");
                driver.FindElement(By.XPath(@"/html/body/div[1]/div[4]/div/center[1]/table/tbody/tr[8]/td/input")).SendKeys($@"{location}/{index}.jpg");
                driver.FindElement(By.XPath(@"/html/body/div[1]/div[4]/div/center[1]/table/tbody/tr[10]/td/center/input[1]")).Click();

                try
                {
                    var alert = driver.SwitchTo().Alert();
                    alert.Accept();
                }
                catch { }

                IWebElement element;

                element = driver.FindElement(By.XPath(@"/html/body/div[1]/div[4]/div/center[1]/table/tbody/tr[10]/td/center/input[2]"));
                while (element.GetAttribute("style").Contains("none"))
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                element.Click();

                element = driver.FindElement(By.XPath(@"/html/body/div[1]/div[4]/div/center[1]/table/tbody/tr[10]/td/center/input[3]"));
                while (element.GetAttribute("style").Contains("none"))
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                element.Click();
            }
            catch { Form1.log.AppendLine($"{DateTime.Now}\t:\t{index}-mobi"); }
        }

        #endregion
    }
}
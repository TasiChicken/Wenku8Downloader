using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Wenku8Downloader
{
    public partial class Form2 : Form
    {
        #region Crawlers

        internal static HttpClient client = new HttpClient();
        
        private static IWebDriver _driver;
        private IWebDriver driver
        {
            get
            {
                if (_driver == null) initSelenium();
                return _driver;
            }
            set { _driver = value; }
        }
        
        private void initSelenium()
        {
            new DriverManager().SetUpDriver(new EdgeConfig());

            EdgeDriverService service = EdgeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            EdgeOptions options = new EdgeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--headless=new");

            options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            options.AddUserProfilePreference("download.default_directory", folder);

            _driver = new EdgeDriver(service, options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
        }
        
        private static void disposeSelenium()
        {
            if (_driver == null) return;
            try { _driver.Close(); }
            catch { }
            try { _driver.Quit(); }
            catch { }
            try { _driver.Dispose(); }
            catch { }
        }

        #endregion

        #region Public API

        public static string[] GetIndexes(Form1 form1, int bookcase, string folder)
        {
            new Form2(form1, bookcase, folder).ShowDialog();
            return indexes;
        }

        public static void Download(string[] indexes, string folder, string encoding, bool separate)
        {
            Form2.indexes = indexes;
            new Form2(folder, encoding, separate).ShowDialog();
        }

        public static void DownloadAndConvertToMobi(Form1 form1, string[] indexes, string folder, string encoding, bool separate, bool delete)
        {
            Form2.indexes = indexes;
            new Form2(form1, folder, encoding, separate, delete).ShowDialog();
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
            Form1.canceled = true;
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
                await downloadImgAndTxt(indexes[i]);
            }
            this.Close();
        }

        private async Task downloadImgAndTxt(string index)
        {
            char head = index.Length < 4 ? '0' : index[0];

            string location = folder + (separate ? $"//{index}" : null);
            Directory.CreateDirectory(location);
            
            try
            {
                byte[] imgData = await client.GetByteArrayAsync($"https://img.wenku8.com/image/{head}/{index}/{index}s.jpg");
                if (!running) return;
                File.WriteAllBytes(location + $"//{index}.jpg", imgData);
            }
            catch (Exception ex)
            {
                Form1.log.AppendLine($"{DateTime.Now}\t:\t{index}-image"); 
                Form1.log.AppendLine(ex.Message); 
            }

            try
            {
                byte[] bytes;
                while (true)
                {
                    HttpResponseMessage response = await client.GetAsync($"https://dl1.wenku8.com/down/txt{encoding}/{head}/{index}.txt");
                    if (!running) return;
                    if (response.IsSuccessStatusCode)
                    {
                        bytes = await response.Content.ReadAsByteArrayAsync();
                        break;
                    }
                    if((int)response.StatusCode == 429)
                    {
                        await Task.Delay(1000);
                    }
                    else
                    {
                        Form1.log.AppendLine($"{DateTime.Now}\t:\t{index}-txt");
                        Form1.log.AppendLine($"Error: HTTP {(int)response.StatusCode} {response.StatusCode}");
                        return;
                    }
                }

                string txtContent = (encoding == "gbk" ? Encoding.GetEncoding("gbk") : Encoding.UTF8).GetString(bytes);
                File.WriteAllText(location + $"//{index}.txt", txtContent);
            }
            catch (Exception ex)
            {
                Form1.log.AppendLine($"{DateTime.Now}\t:\t{index}-txt");
                Form1.log.AppendLine(ex.Message);
            }
        }

        #endregion

        #region GetIndexes

        int bookcase;

        private Form2(Form1 form1, int bookcase, string folder)
        {
            this.folder = folder;
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

        private static readonly string[] logInEleNames = { "username", "password" };
        private async Task getIndexes()
        {
            string bookcaseUrl = $@"https://www.wenku8.net/modules/article/bookcase.php?classid={bookcase}";
            driver.Navigate().GoToUrl(bookcaseUrl);
            int i = 0;

            if (driver.Url != bookcaseUrl)
            {
                for (i = 0; i < 2; i++)
                {
                    var tb = driver.FindElement(By.Name(logInEleNames[i]));
                    tb.SendKeys(form1.accountInfoTbs[i].Text);
                }
                driver.FindElement(By.Name("submit")).Click();
                driver.Navigate().GoToUrl(bookcaseUrl);
            }

            List<string> list = new List<string>();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);

            var bookItems = driver.FindElements(By.Id("checkid[]"));

            Action setMax = () => progressBar1.Maximum = bookItems.Count;
            this.Invoke(setMax);

            Action upLbl = () => label1.Text = $"Running {i}/{bookItems.Count}";
            Action upPgB = () => progressBar1.Value = i;

            for (i = 0; i < bookItems.Count; i++)
            {
                if (!running) break;

                this.Invoke(upLbl);
                this.Invoke(upPgB);

                try
                {
                    Console.WriteLine(bookItems[i].Text);
                   
                    var element = bookItems[i].FindElement(By.XPath(@"../../td[2]/a"));
                    string href = element.GetAttribute("href");
                    int start = href.IndexOf("aid=") + 4;
                    list.Add(href.Substring(start, href.IndexOf("&bid") - start));

                    if (!form1.saveMobi) continue;

                    form1.names.Add(element.Text);
                    element = bookItems[i].FindElement(By.XPath(@"../../td[3]/a"));
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

        bool delete;
        private Form2(Form1 form1, string folder, string encoding, bool separate, bool delete)
        {
            this.form1 = form1; 
            this.folder = folder;
            this.separate = separate;
            this.delete = delete;
            this.encoding = encoding;
            action = downloadAndConvertBooks;

            InitializeComponent();

            this.Text = "Convert Books to MOBI Files";
        }

        private async void downloadAndConvertBooks()
        {
            progressBar1.Maximum = indexes.Length;

            for (int i = 0; i < indexes.Length; i++)
            {
                if (!running) return;

                label1.Text = $"Running {i}/{indexes.Length}";
                progressBar1.Value = i;
                
                await downloadImgAndTxt(indexes[i]);

                driver.Navigate().GoToUrl("https://ebook.cdict.info/mobi/#google_vignette");

                await convertABook(indexes[i], form1.names[i], form1.authors[i]);

                if(delete)
                {
                    if (separate)
                        Directory.Delete($@"{folder}/{indexes[i]}", true);
                    else
                    {
                        File.Delete($@"{folder}/{indexes[i]}.txt");
                        File.Delete($@"{folder}/{indexes[i]}.jpg");
                    }
                }
            }

            this.Close();
        }

        private async Task convertABook(string index, string name, string author)
        {
            string location = folder + (separate ? $"//{index}" : null);

            try
            {
                driver.Navigate().Refresh();

                driver.FindElement(By.Name("title")).SendKeys(name);
                driver.FindElement(By.Name("author")).SendKeys(author);
                driver.FindElement(By.Id("txt_file")).SendKeys($@"{location}/{index}.txt");
                driver.FindElement(By.Id("cover_file")).SendKeys($@"{location}/{index}.jpg");
                driver.FindElement(By.Id("nextbutton")).Click();

                try
                {
                    var alert = driver.SwitchTo().Alert();
                    alert.Accept();
                }
                catch { }
                IWebElement element;

                element = driver.FindElement(By.Id("submit_button"));
                while (element.GetAttribute("style").Contains("none") || !running || !element.Enabled)
                    await Task.Delay(500);
                if (!running) return;
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                element.Click();

                await Task.Delay(2000);
                element = driver.FindElement(By.Id("download_button"));
                while (element.GetAttribute("style").Contains("none") || !running || !element.Enabled)
                    await Task.Delay(500);
                if (!running) return;
                element.Click();
            }
            catch (Exception ex) 
            {
                Form1.log.AppendLine($"{DateTime.Now}\t:\t{index}-mobi"); 
                Form1.log.AppendLine(ex.Message); 
            }
        }

        #endregion
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wenku8Downloader
{
    public partial class Form1 : Form
    {
        public static bool canceled = false;

        #region Form

        public Form1()
        {
            InitializeComponent();

            comboBox1.SelectedIndex = 0;

            accountInfoTbs = new TextBox[] { textBox2, textBox3 };
            encodingRbs = new RadioButton[] { radioButton1, radioButton2, radioButton3 };

            string[] s = File.ReadAllLines(CookieForm.CookieLocation);
            for (int i = 0; i < accountInfoTbs.Length && i < s.Length; i++)
                accountInfoTbs[i].Text = s[i];
            textBox4.Text = (s.Length >= 3) ? s[2] : Environment.GetEnvironmentVariable("USERPROFILE") + "\\Downloads";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Form2.dispose();
        }

        #endregion

        #region Properties

        internal TextBox[] accountInfoTbs;
        internal List<string> names = new List<string>();
        internal List<string> authors = new List<string>();
        internal bool saveMobi = false;
        internal string folder { get { return textBox4.Text; } }
        internal static StringBuilder log = new StringBuilder();

        #endregion

        #region Fields
        
        private RadioButton[] encodingRbs;
        private string[] encodings = { "gbk", "utf8", "big5" };

        #endregion

        #region Buttons

        private void button4_Click(object sender, EventArgs e)
        {
            new CookieForm(new TextBox[] { textBox2, textBox3, textBox4 }).Show();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            canceled = false;

            if (!check())
            {
                MessageBox.Show("Input data are not complete or wrong!"); 
                return;
            }

            button2.Enabled = textBox4.Enabled = false;

            saveMobi = false;
            string[] indexes = await getIndexes();


            if (!canceled) Form2.Download(indexes, textBox4.Text, getEncoding(), checkBox2.Checked);

            if (log.Length > 0)
            {
                File.WriteAllText(folder + "\\log.txt", log.ToString());
                MessageBox.Show("Errors occur! Please check 「log.txt」 in the save folder!");
                log.Clear();
            }
            else MessageBox.Show("Finished!");
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            canceled = false;

            if (!check())
            {
                MessageBox.Show("Input data are not complete or wrong!");
                return;
            }

            button2.Enabled = textBox4.Enabled = false;

            saveMobi = true;
            names.Clear();
            authors.Clear();

            string[] indexes = await getIndexes();
            if (!canceled)
            {
                string encoding = getEncoding();
                bool separate = checkBox2.Checked;

                Form2.DownloadAndConvertToMobi(this, indexes, textBox4.Text, encoding, separate, checkBox3.Checked);
            }

            if (log.Length > 0)
            {
                File.WriteAllText(folder + "\\log.txt", log.ToString());
                MessageBox.Show("Errors occur! Please check 「log.txt」 in the save folder!");
                log.Clear();
            }
            else MessageBox.Show("Finished!");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                textBox4.Text = dialog.SelectedPath;
            }
        }

        #endregion

        # region Functions

        private async Task<string[]> getIndexes()
        {
            if (tabControl1.SelectedIndex == 0)
            {
                char sign = textBox1.Text.FirstOrDefault((c) => c > '9' || c < '0');
                string[] indexes = textBox1.Text.Split(sign);

                if (saveMobi)
                    foreach(string index in indexes)
                    {
                        byte[] bytes = await Form2.client.GetByteArrayAsync($@"https://www.wenku8.net/book/{index}.htm");
                        string html = Encoding.GetEncoding(encodings[0]).GetString(bytes);

                        int start = html.IndexOf("<title>") + "<title>".Length;
                        html = html.Substring(start, html.IndexOf(@"</title>") - start);

                        string[] data = html.Split(new string[] { " - " }, StringSplitOptions.None);
                        names.Add(data[0]);
                        authors.Add(data[1]);
                    }

                return indexes;
            }

            return Form2.GetIndexes(this, comboBox1.SelectedIndex, textBox4.Text);
        }

        private string getEncoding()
        {
            for (int i = 0; i < 3; i++)
                if (encodingRbs[i].Checked) return encodings[i];
            return encodings[0];
        }

        private bool check()
        {
            if (!Directory.Exists(textBox4.Text)) Directory.CreateDirectory(textBox4.Text);

            if (tabControl1.SelectedIndex == 0)
                return textBox1.TextLength > 0;
            else
                return textBox2.TextLength > 0 && textBox3.TextLength > 0;
        }

        #endregion

    }
}

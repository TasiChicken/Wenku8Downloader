using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wenku8Downloader
{
    public partial class CookieForm : Form
    {
        public CookieForm()
        {
            InitializeComponent();

            tbs = new TextBox[] { textBox2, textBox3, textBox4 };

            string[] s = File.ReadAllLines(CookieLocation);
            for(int i = 0; i < tbs.Length && i < s.Length; i++)
                tbs[i].Text = s[i];
        }
        TextBox[] tbs;

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            
            for(int i = 0; i < tbs.Length; i++)
                builder.AppendLine(tbs[i].Text);
            File.WriteAllText(CookieLocation, builder.ToString());

            this.Close();
        }

        public static string CookieLocation
        {
            get
            {
                return Application.StartupPath + @"\Properties\Cookie.txt";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                textBox4.Text = dialog.SelectedPath;
            }
        }
    }
}

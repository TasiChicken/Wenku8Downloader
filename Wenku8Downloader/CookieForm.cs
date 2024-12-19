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
        public CookieForm(TextBox[] oriTbs)
        {
            InitializeComponent();
            
            this.oriTbs = oriTbs;
            tbs = new TextBox[] { textBox2, textBox3, textBox4 };

            string[] s = File.ReadAllLines(CookieLocation);
            for(int i = 0; i < tbs.Length && i < s.Length; i++)
                tbs[i].Text = s[i];
            if(s.Length < 3) tbs[2].Text = oriTbs[2].Text;
        }
        TextBox[] oriTbs;
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

            for (int i = 0; i < oriTbs.Length; i++)
                oriTbs[i].Text = tbs[i].Text;

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

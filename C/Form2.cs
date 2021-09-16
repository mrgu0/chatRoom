using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _233聊天室
{
    public partial class Form2 : Form
    {
        public string IP;
        public string name;
        public Form2()
        {
           // 
            InitializeComponent();
        checkBox1.CheckState = (CheckState)1;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                IP = textBox1.Text;
                name = textBox2.Text;
                DialogResult = DialogResult.OK;
                this.Hide();
               
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                textBox1.Enabled = false;
            }
            else
                textBox1.Enabled = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _233聊天室
{
    static class Program
    {
        static Form1 f1;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form2 f2 = new Form2();
            if (f2.ShowDialog() == DialogResult.OK)
            {
                f1 = new Form1(f2.IP, f2.name);
                Application.Run(f1);
            }
        }
    }
}

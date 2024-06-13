using ProductBinder.Motor;
using System.IO.Ports;
using static System.Net.WebRequestMethods;

namespace NfcRW
{
    public partial class FrmTest : Form
    {
        SCManager scManager;
        MotorManager motorManager;

        public FrmTest()
        {
            InitializeComponent();
        }

        private void FmTest_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange([.. SCManager.ListReaders()]);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
            {
                return;
            }
            scManager?.Disconnect();
            scManager = new SCManager(comboBox1.SelectedItem.ToString());
            //scManager.SetDevice(comboBox1.SelectedItem.ToString());
            scManager.CardInserted += Reader_CardInserted;
            scManager.CardEjected += Reader_CardEjected;
            scManager.Disconnected += Reader_Disconnected;
        }

        private void Reader_Disconnected(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => Reader_Disconnected(sender, e));
                return;
            }
            label1.Text = ("disconnected");
        }

        private void Reader_CardEjected(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => Reader_CardEjected(sender, e));
                return;
            }
            label1.Text = ("ejected");

        }

        private async void Reader_CardInserted(object? sender, CardInsertedEventArgs e)
        {
            var result = await motorManager.Stop();
            if (InvokeRequired)
            {
                Invoke(() => Reader_CardInserted(sender, e));
                return;
            }
            label1.Text = ("inserted");
            //textBox1.AppendText(e.CardUID);
        }

        private void FrmTest_FormClosing(object sender, FormClosingEventArgs e)
        {
            scManager?.Disconnect();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();
            comboBox2.Items.AddRange(SerialPort.GetPortNames());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (motorManager == null)
            {
                motorManager = new MotorManager(comboBox2.SelectedItem as string);
                motorManager.MotorChanged += MotorChanged;
            }
            motorManager.Connect();
        }

        private void MotorChanged(object? sender, MotorEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => MotorChanged(sender, e));
                return;
            }
            var log = motorManager.GetLog();
            textBox2.Text = log;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            motorManager?.Disconnect();
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            textBox1.Text = string.Empty;
            int stp = (int)numericUpDown1.Value;
            var snt = await motorManager.RunSteps(stp);
            textBox1.Text = snt.ToString();
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            textBox1.Text = string.Empty;
            var snt = await motorManager.RunFree();
            textBox1.Text = snt.ToString();
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            textBox1.Text = string.Empty;
            var snt = await motorManager.Stop();
            textBox1.Text = snt.ToString();
        }
    }
}

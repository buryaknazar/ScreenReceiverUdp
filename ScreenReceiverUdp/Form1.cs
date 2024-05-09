using PartLibrary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace ScreenReceiverUdp
{
    public partial class Form1 : Form
    {
        int port = 0;
        IPEndPoint address = null;
        ScreenForm form = null;

        public Form1()
        {
            InitializeComponent();
            port = Convert.ToInt32(tbPort.Text.Trim());
            address = new IPEndPoint(IPAddress.Parse(tbAddress.Text.Trim()), port);
            form = new ScreenForm();
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            btStart.BackColor = Color.YellowGreen;
            Thread thread = new Thread(ReceiveScreens);
            thread.IsBackground = true;
            thread.Start();
            form.Show();
        }

        private void ReceiveScreens()
        {
            try
            {
                UdpClient receiver = new UdpClient(address);
                IPEndPoint ep = null;
                Dictionary<Int32, Part> buffer = new Dictionary<int, Part>();
                long key = 0;
                int total = 0;
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                Bitmap bmp = null;

                while (true) 
                {
                    byte[] data = receiver.Receive(ref ep);
                    Part part = null;
                    
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        part = (Part?)binaryFormatter.Deserialize(ms);
                    }
                    if(key != part.Key)
                    {
                        buffer.Clear();
                        key = part.Key;
                        total = part.Total;
                    }
                    buffer.Add(part.Number, part);
                    if(buffer.Count == total)
                    {
                        Task task = Task.Run( () => {
                            Dictionary<Int32, Part> copy = new Dictionary<int, Part>(buffer);
                            long key1 = 0;
                            byte[] bytes = new byte[0];
                            MemoryStream stream = new MemoryStream();
                            for(int i = 0; i < copy.Count; i++)
                            {
                                Part part1 = copy[i];
                                key1 = part1.Key;
                                stream.Write(part1.Data, 0, part1.Data.Length);
                            }
                            bmp = new Bitmap(stream);               //TODO
                        });
                        task.Wait();
                        this.Invoke(delegate () {
                            form.pictureBox1.Image = bmp;
                            form.Refresh();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ReceiveScreens:" + ex.Message);
            }
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            btStart.BackColor = SystemColors.ButtonFace;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        const int N = 200;
        bool f = true;

        #region Manager Variables

        private SerialPort comPort = new SerialPort();
        private byte[] WriteBytes = new byte[32];
        private byte[] ReadBytes = new byte[32];

        private byte NTxD;
        private byte NRxD;
        int count = 0;
        private int RowCanal1;
        private int RowCanal2;

        #endregion

        public Window1()
        {
            InitializeComponent();
        }

        //private void PortDefault()
        //{
        //    String xmlString = File.ReadAllText("parameters.xml");
        //    XDocument xdoc = XDocument.Load(new StringReader(xmlString));

        //    var xmlList = (from article in xdoc.Descendants("COMPORT")
        //        select new
        //        {
        //            Name = article.Descendants("Name").SingleOrDefault(),
        //            BaudRate = article.Descendants("BaudRate").SingleOrDefault(),
        //            DataBits = article.Descendants("DataBits").SingleOrDefault(),
        //            StopBits = article.Descendants("StopBits").SingleOrDefault(),
        //            Parity = article.Descendants("Parity").SingleOrDefault(),
        //            Handshake = article.Descendants("Handshake").SingleOrDefault(),
        //            RtsEnable = article.Descendants("RtsEnable").SingleOrDefault(),
        //            ReadTimeout = article.Descendants("ReadTimeout").SingleOrDefault(),
        //            ReadBufferSize = article.Descendants("ReadBufferSize").SingleOrDefault()
        //        }).ToList();
        //    var articleList = (from item in xmlList
        //        select new
        //        {
        //            Name = item.Name != null ? item.Name.Value : null,
        //            BaudRate = item.BaudRate != null ? item.BaudRate.Value : null,
        //            DataBits = item.DataBits != null ? item.DataBits.Value : null,
        //            StopBits = item.StopBits != null ? item.StopBits.Value : null,
        //            Parity = item.Parity != null ? item.Parity.Value : null,
        //            Handshake = item.Handshake != null ? item.Handshake.Value : null,
        //            RtsEnable = item.RtsEnable != null ? item.RtsEnable.Value : null,
        //            ReadTimeout = item.ReadTimeout != null ? item.ReadTimeout.Value : null,
        //            ReadBufferSize = item.ReadBufferSize != null ? item.ReadBufferSize.Value : null
        //        });
        //    foreach (var article in articleList)
        //    {
        //        comPort.BaudRate = Convert.ToInt32(article.BaudRate);
        //        comPort.PortName = article.Name;
        //        comPort.DataBits = Convert.ToInt32(article.DataBits);
        //        comPort.RtsEnable = false;
        //        comPort.ReadTimeout = Convert.ToInt32(article.ReadTimeout);
        //        comPort.StopBits = StopBits.One;
        //        comPort.Parity = Parity.None;
        //        comPort.Handshake = Handshake.None;
        //        comPort.ReadBufferSize = Convert.ToInt32(article.ReadBufferSize);
        //    }

        //    try
        //    {
        //        comPort.Open();
        //        textBox1.AppendText("Порт открыт" + "\r\n");
        //    }
        //    catch
        //    {
        //        textBox1.AppendText("Ошибка открытия порта");
        //    }

        //    // 

        //    byte ks;
        //    byte error;
        //    String result;

        //    // команда "Установить канал"
        //    error = 0;

        //    NTxD = 6;
        //    NRxD = 8;

        //    WriteBytes[0] = 0x05;
        //    WriteBytes[1] = 0x30;
        //    WriteBytes[2] = 0x31;

        //    // контрольная сумма
        //    ks = (byte)(WriteBytes[1] + WriteBytes[2]);

        //    //байт в hex-формате, с добавлением нулей:
        //    result = ks.ToString("X2");
        //    byte[] ASCIIValues = Encoding.ASCII.GetBytes(result);
        //    WriteBytes[3] = ASCIIValues[0];
        //    WriteBytes[4] = ASCIIValues[1];
        //    WriteBytes[5] = 0x0d;

        //    Transmit();

        //    string tempString = null;
        //    tempString = Encoding.GetEncoding(1251).GetString(ReadBytes, 1, 4);

        //    textBox1.AppendText("Найдено устройство: " + tempString);

        //    NTxD = 6;
        //    NRxD = 12;

        //    WriteBytes[0] = 0x05;
        //    WriteBytes[1] = 0x30;
        //    WriteBytes[2] = 0x32;

        //    // контрольная сумма
        //    ks = (byte)(WriteBytes[1] + WriteBytes[2]);

        //    //байт в hex-формате, с добавлением нулей:
        //    result = ks.ToString("X2");
        //    ASCIIValues = Encoding.ASCII.GetBytes(result);
        //    WriteBytes[3] = ASCIIValues[0];
        //    WriteBytes[4] = ASCIIValues[1];
        //    WriteBytes[5] = 0x0d;

        //    Transmit();

        //    tempString = null;

        //    tempString = Encoding.GetEncoding(1251).GetString(ReadBytes, 1, 7);

        //    if (f)
        //        textBox1.AppendText(" " + tempString + "\r\n");
        //}

        //private void Read_prib()
        //{
        //    byte ks;
        //    byte error;
        //    String result;

        //    // команда "Чтение установленной температуры"
        //    error = 0;

        //    NTxD = 6;
        //    NRxD = 6;

        //    WriteBytes[0] = 0x05;
        //    WriteBytes[1] = 0x38;
        //    WriteBytes[2] = 0x30;

        //    // контрольная сумма
        //    ks = (byte)(WriteBytes[1] + WriteBytes[2]);

        //    //байт в hex-формате, с добавлением нулей:
        //    result = ks.ToString("X2");
        //    byte[] ASCIIValues = Encoding.ASCII.GetBytes(result);
        //    WriteBytes[3] = ASCIIValues[0];
        //    WriteBytes[4] = ASCIIValues[1];
        //    WriteBytes[5] = 0x0d;

        //    Transmit();


        //    result = Char.ConvertFromUtf32(ReadBytes[1]) + Char.ConvertFromUtf32(ReadBytes[2]);


        //    int num = Int32.Parse(result, System.Globalization.NumberStyles.HexNumber);


        //    if (num >= 30)
        //    {
        //        numericUpDown1.Value = num;
        //        checkBox1.Checked = true;
        //    }
        //    else
        //    {
        //        numericUpDown1.Value = 30;
        //        checkBox1.Checked = false;

        //    }

        //    // команда "Чтение текущей температуры"

        //    NTxD = 6;
        //    NRxD = 6;

        //    WriteBytes[0] = 0x05;
        //    WriteBytes[1] = 0x38;
        //    WriteBytes[2] = 0x31;

        //    // контрольная сумма
        //    ks = (byte)(WriteBytes[1] + WriteBytes[2]);

        //    //байт в hex-формате, с добавлением нулей:
        //    result = ks.ToString("X2");
        //    ASCIIValues = Encoding.ASCII.GetBytes(result);
        //    WriteBytes[3] = ASCIIValues[0];
        //    WriteBytes[4] = ASCIIValues[1];
        //    WriteBytes[5] = 0x0d;

        //    Transmit();

        //    result = Char.ConvertFromUtf32(ReadBytes[1]) + Char.ConvertFromUtf32(ReadBytes[2]);

        //    num = Int32.Parse(result, System.Globalization.NumberStyles.HexNumber);


        //    label4.Text = (num + " °C");

        //    // команда "Чтение канала измерения"

        //    NTxD = 6;
        //    NRxD = 6;

        //    WriteBytes[0] = 0x05;
        //    WriteBytes[1] = 0x38;
        //    WriteBytes[2] = 0x32;

        //    // контрольная сумма
        //    ks = (byte)(WriteBytes[1] + WriteBytes[2]);

        //    //байт в hex-формате, с добавлением нулей:
        //    result = ks.ToString("X2");
        //    ASCIIValues = Encoding.ASCII.GetBytes(result);
        //    WriteBytes[3] = ASCIIValues[0];
        //    WriteBytes[4] = ASCIIValues[1];
        //    WriteBytes[5] = 0x0d;

        //    Transmit();


        //    result = Char.ConvertFromUtf32(ReadBytes[1]) + Char.ConvertFromUtf32(ReadBytes[2]);

        //    num = Int32.Parse(result, System.Globalization.NumberStyles.HexNumber);

        //    if (num > 0)
        //    { radioButton2.Checked = true; }
        //    else { radioButton1.Checked = true; }

        //    // команда "Чтение диапазона измерения канала 1"

        //    NTxD = 6;
        //    NRxD = 6;

        //    WriteBytes[0] = 0x05;
        //    WriteBytes[1] = 0x38;
        //    WriteBytes[2] = 0x34;

        //    // контрольная сумма
        //    ks = (byte)(WriteBytes[1] + WriteBytes[2]);

        //    //байт в hex-формате, с добавлением нулей:
        //    result = ks.ToString("X2");
        //    ASCIIValues = Encoding.ASCII.GetBytes(result);
        //    WriteBytes[3] = ASCIIValues[0];
        //    WriteBytes[4] = ASCIIValues[1];
        //    WriteBytes[5] = 0x0d;

        //    Transmit();


        //    result = Char.ConvertFromUtf32(ReadBytes[1]) + Char.ConvertFromUtf32(ReadBytes[2]);

        //    num = Int32.Parse(result, System.Globalization.NumberStyles.HexNumber);

        //    comboBox1.SelectedIndex = num;

        //    // команда "Чтение диапазона измерения канала 1"

        //    NTxD = 6;
        //    NRxD = 6;

        //    WriteBytes[0] = 0x05;
        //    WriteBytes[1] = 0x38;
        //    WriteBytes[2] = 0x35;

        //    // контрольная сумма
        //    ks = (byte)(WriteBytes[1] + WriteBytes[2]);

        //    //байт в hex-формате, с добавлением нулей:
        //    result = ks.ToString("X2");
        //    ASCIIValues = Encoding.ASCII.GetBytes(result);
        //    WriteBytes[3] = ASCIIValues[0];
        //    WriteBytes[4] = ASCIIValues[1];
        //    WriteBytes[5] = 0x0d;

        //    Transmit();


        //    result = Char.ConvertFromUtf32(ReadBytes[1]) + Char.ConvertFromUtf32(ReadBytes[2]);

        //    num = Int32.Parse(result, System.Globalization.NumberStyles.HexNumber);

        //    comboBox2.SelectedIndex = num;

        //    // команда "Чтение диапазона измерения канала 1"

        //    NTxD = 6;
        //    NRxD = 11;

        //    WriteBytes[0] = 0x05;
        //    WriteBytes[1] = 0x38;
        //    WriteBytes[2] = 0x33;

        //    // контрольная сумма
        //    ks = (byte)(WriteBytes[1] + WriteBytes[2]);

        //    //байт в hex-формате, с добавлением нулей:
        //    result = ks.ToString("X2");
        //    ASCIIValues = Encoding.ASCII.GetBytes(result);
        //    WriteBytes[3] = ASCIIValues[0];
        //    WriteBytes[4] = ASCIIValues[1];
        //    WriteBytes[5] = 0x0d;

        //    Transmit();


        //    result = Char.ConvertFromUtf32(ReadBytes[1]) + Char.ConvertFromUtf32(ReadBytes[2]) + Char.ConvertFromUtf32(ReadBytes[3])
        //             + Char.ConvertFromUtf32(ReadBytes[4]) + Char.ConvertFromUtf32(ReadBytes[5])
        //             + Char.ConvertFromUtf32(ReadBytes[6]) + Char.ConvertFromUtf32(ReadBytes[7]);

        //    label3.Text = result + " Ом";

        //    comboBox2.SelectedIndex = num;
        //    textBox1.AppendText(result);
        //}
        
        //// Отправка на устройство
        //private void Transmit()
        //{
        //    Array.Clear(ReadBytes, 0, ReadBytes.Length);
        //    comPort.DiscardInBuffer();
        //    comPort.Write(WriteBytes, 0, NTxD);
        //    Thread.Sleep(500);
        //    try
        //    {
        //        comPort.Read(ReadBytes, 0, NRxD);
        //        f = true;
        //    }
        //    catch (TimeoutException)
        //    {
        //        f = false;
        //        textBox1.AppendText("Ошибка: \r\n");
        //    }
        //}

        //private void Window1_OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    dataGridView1.Rows.Add(200);
        //    comboBox1.SelectedIndex = 0;
        //    comboBox1.MaxDropDownItems = 13;
        //    comboBox2.SelectedIndex = 0;
        //    comboBox2.MaxDropDownItems = 13;

        //    RowCanal1 = 1;
        //    RowCanal2 = 1;

        //    PortDefault();
        //}

        //private void ReadBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (f)
        //            Read_prib();
        //        else
        //            TextBoxInfo.AppendText("Устройство не найдено! \r\n");
        //    }
        //    catch (TimeoutException)
        //    {
        //        TextBoxInfo.AppendText("ошибка 1: \r\n");
        //    }
        //}

        //private void Window1_OnDeactivated(object sender, EventArgs e)
        //{
        //    comPort.Close();
        //}
    }
}

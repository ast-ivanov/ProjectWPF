using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для Window2.xaml
    /// </summary>
    public partial class Window2
    {
        private readonly string[] _speeds = {"4600", "9600", "14400"};
        private readonly string[] _names = {"COM1", "COM2"};
        private bool AllFilled => DataBitsBox.Text != "" && BufferSizeBox.Text != "" && HandshakeBox.Text != "" &&
                                 ParityBox.Text != "" && RtsEnableBox.Text != "" && StopBitsBox.Text != "" &&
                                 TimeoutBox.Text != "" && BaudRateCombo.SelectedIndex != -1 &&
                                 NameCombo.SelectedIndex != -1;

        public Window2()
        {
            InitializeComponent();

            NameCombo.ItemsSource = _names;
            BaudRateCombo.ItemsSource = _speeds;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            string path = Application.Current.StartupUri.AbsolutePath + "/parameters.xml";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            //path = Application.StartupPath + "/parameters.xml";
            try
            {

                XDocument xdoc = new XDocument(new XElement("XML",
                    new XElement("COMPORT",
                    new XElement("Name", NameCombo.Text),
                    new XElement("BaudRate", BaudRateCombo.Text),
                    new XElement("DataBits", "8"),
                    new XElement("StopBits", StopBits.One),
                    new XElement("Parity", Parity.None),
                    new XElement("Handshake", Handshake.None),
                    new XElement("RtsEnable", false),
                    new XElement("ReadTimeout", "500"),
                    new XElement("ReadBufferSize", "32")),
                    new XElement("WINDOW",
                    new XElement("Width", "1200"),
                    new XElement("Height", "650"))));
                xdoc.Save(path);
            }
            catch
            {
                // ignored
            }
        }

        private void Read_Click(object sender, RoutedEventArgs e)
        {
            String xmlString = File.ReadAllText("parameters.xml");
            XDocument xdoc = XDocument.Load(new StringReader(xmlString));

            var xmlList = from article in xdoc.Descendants("COMPORT")
                          select new
                          {
                              Name = article.Descendants("Name").SingleOrDefault(),
                              BaudRate = article.Descendants("BaudRate").SingleOrDefault(),
                              DataBits = article.Descendants("DataBits").SingleOrDefault(),
                              StopBits = article.Descendants("StopBits").SingleOrDefault(),
                              Parity = article.Descendants("Parity").SingleOrDefault(),
                              Handshake = article.Descendants("Handshake").SingleOrDefault(),
                              RtsEnable = article.Descendants("RtsEnable").SingleOrDefault(),
                              ReadTimeout = article.Descendants("ReadTimeout").SingleOrDefault(),
                              ReadBufferSize = article.Descendants("ReadBufferSize").SingleOrDefault()
                          };
            var articleList = from item in xmlList
                              select new
                              {
                                  Name = item.Name?.Value,
                                  BaudRate = item.BaudRate?.Value,
                                  DataBits = item.DataBits?.Value,
                                  StopBits = item.StopBits?.Value,
                                  Parity = item.Parity?.Value,
                                  Handshake = item.Handshake?.Value,
                                  RtsEnable = item.RtsEnable?.Value,
                                  ReadTimeout = item.ReadTimeout?.Value,
                                  ReadBufferSize = item.ReadBufferSize?.Value
                              };
            foreach (var article in articleList)
            {
                for (int i = 0; i < _speeds.Length; i++)
                {
                    if (article.BaudRate == _speeds[i])
                    {
                        BaudRateCombo.SelectedIndex = i;
                    }
                }
                for (int i = 0; i < _names.Length; i++)
                {
                    if (article.Name == _names[i])
                    {
                        NameCombo.SelectedIndex = i;
                    }
                }
                DataBitsBox.Text = article.DataBits;
                RtsEnableBox.Text = article.RtsEnable;
                TimeoutBox.Text = article.ReadTimeout;
                StopBitsBox.Text = article.StopBits;
                ParityBox.Text = article.Parity;
                HandshakeBox.Text = article.Handshake;
                BufferSizeBox.Text = article.ReadBufferSize;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnSubmit.IsEnabled = AllFilled;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            BtnSubmit.IsEnabled = AllFilled;
        }
    }
}

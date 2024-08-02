using System.IO.Ports;
using System.Windows;
using System.Windows.Media;
using System.Text;
using Microsoft.Win32;
using System.IO;


namespace SerialPortDgn
{
    public partial class MainWindow : Window
    {
        private SerialPort serialPort;
        private StringBuilder receivedData = new StringBuilder();
        private string filePath;
        private string scaleName;
        private int limitLine = 100;
        private string version = "1.0.1";
        public MainWindow()
        {
            InitializeComponent();
            LoadSerialPorts();
            LoadBaudRates();
            Init();
        }

        private void LoadSerialPorts()
        {
            string[] ports = SerialPort.GetPortNames()
                                       .OrderBy(p => int.Parse(p.Replace("COM", string.Empty)))
                                       .ToArray();

            cbSerialPort.Items.Clear();
            foreach (string port in ports)
            {
                cbSerialPort.Items.Add(port);
            }
            if (cbSerialPort.Items.Count > 0)
            {
                cbSerialPort.SelectedIndex = 0;
            }
        }
        private void LoadBaudRates()
        {
            int[] baudRates = { 1200, 2400, 4800, 9600, 19200 };

            cbBaudrate.Items.Clear();
            foreach (int rate in baudRates)
            {
                cbBaudrate.Items.Add(rate);
            }
            if (cbBaudrate.Items.Count > 0)
            {
                cbBaudrate.SelectedIndex = 3;
            }
        }
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbScaleName.Text))
            {
                MessageBox.Show("Digite um nome para a balança");
                return;
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("Por favor selecione um caminho de arquivo");
                return;
            }
            try
            {
                string selectedPort = cbSerialPort.SelectedItem.ToString();
                int selectedBaudRate = int.Parse(cbBaudrate.SelectedItem.ToString());
                filePath = Path.Combine(tbFolderPath.Text, $"{tbScaleName.Text}.txt");

                serialPort = new SerialPort(selectedPort, selectedBaudRate, Parity.None, 8, StopBits.One);
                serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                serialPort.Open();
                btnConnect.IsEnabled = false;
                btnConnect.Foreground = Brushes.LightGray;
                btnDisconnect.IsEnabled = true;
                btnDisconnect.Foreground = Brushes.DarkRed;
                lbConnect.Content = $"Porta {selectedPort} conectada!";

                tbScaleName.IsEnabled = false;
                tbFolderPath.IsEnabled = false;
                btnBrowse.IsEnabled = false;
                rbHundredLines.IsEnabled = false;
                rbOneLine.IsEnabled = false;
                cbOnlyWeight.IsEnabled = false;
                limitLine = rbOneLine.IsChecked == true ? 1 : 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar: {ex.Message}");
            }

        }
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                lbConnect.Content = "Aguardando Conexão...";
                btnConnect.IsEnabled = true;
                btnConnect.Foreground = Brushes.Green;
                btnDisconnect.IsEnabled = false;
                btnDisconnect.Foreground = Brushes.LightGray;
                lbWeight.Content = "---------- kg";

                tbScaleName.IsEnabled = true;
                tbFolderPath.IsEnabled = true;
                btnBrowse.IsEnabled = true;
                rbHundredLines.IsEnabled = true;
                rbOneLine.IsEnabled = true;
                cbOnlyWeight.IsEnabled = true;
            }
        }
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Leia os dados recebidos
                string data = serialPort.ReadExisting();
                receivedData.Append(data);

                // Processar os dados recebidos quando uma linha completa é recebida
                string completeData = receivedData.ToString();
                int index;

                while ((index = completeData.IndexOf('\r')) != -1)
                {
                    string line = completeData.Substring(1, index).Trim();
                    completeData = completeData.Substring(index + 1);

                  
                    Dispatcher.Invoke(() =>
                    {
                        string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        string logEntry = cbOnlyWeight.IsChecked == true ? $"{line}" : $"{timestamp} {line}";
                        lbWeight.Content = $"Peso: {line} kg";
                        AppendLineToFileWithLimit(filePath, logEntry, limitLine);
                    });
                }

                // Limpar os dados recebidos já processados
                receivedData.Clear();
                receivedData.Append(completeData);
            }
            catch (TimeoutException) { }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erro ao ler dados: {ex.Message}");
                });
            }
        }
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Selecione uma pasta",
                Filter = "Folders|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = Path.GetDirectoryName(dialog.FileName);
                tbFolderPath.Text = selectedPath;
                filePath = Path.Combine(selectedPath, $"{tbScaleName.Text}.txt");
            }
        }
        private void AppendLineToFileWithLimit(string path, string line, int limit)
        {
            var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();
            lines.Add(line);

            if (lines.Count > limit)
            {
                lines.RemoveAt(0); // Remove a primeira linha se o limite for excedido
            }

            File.WriteAllLines(path, lines);
        }
        private void Init()
        {
            lbVersion.Content = $"Versão:{version}";
        }
    }
}
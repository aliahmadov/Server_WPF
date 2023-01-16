using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Commands;
using Server.Helpers;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Server.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public int Id { get; set; }

        public RelayCommand StartCommand { get; set; }

        public Item ClientItem { get; set; }


        private ObservableCollection<Item> clientItems;

        public ObservableCollection<Item> ClientItems
        {
            get { return clientItems; }
            set { clientItems = value; OnPropertyChanged(); }
        }


        public bool IsLoaded { get; set; }



        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return null;
        }

        private static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }



        public async void ReceiveMessages(Socket client)
        {
            await Task.Run(() =>
            {
                MessageBox.Show($"{client.RemoteEndPoint} connected");
                var length = 0;
                var bytes = new byte[1024 * 1024 * 128];

                string jsonString;
                do
                {

                    try
                    {
                        length = client.Receive(bytes);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($@"{client.RemoteEndPoint} closed connection");
                        break;
                    }

                    jsonString = Encoding.UTF8.GetString(bytes);

                    ClientItem = FileHelper<Item>.Deserialize(jsonString);

                    Image image = Converter.byteArrayToImage(ClientItem.ImageBytes);
                    var bitmap = new Bitmap(image);  
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    bitmap.Save(desktopPath + $@"\serverImages\Picture{Id++}.jpg",ImageFormat.Jpeg);
                    
                    App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
                    {
                        ClientItems.Add(ClientItem);
                    });

                } while (true);

            });
        }


        public async void StartAction()
        {
            var localIP = IPAddress.Parse(GetLocalIPAddress());
            var port = 26000;

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                var endPoint = new IPEndPoint(localIP, port);
                socket.Bind(endPoint);
                socket.Listen(10);// maximum amount of clients in the queue connected to the server
                MessageBox.Show($"Listen over {socket.LocalEndPoint}");
                while (true)
                {
                    var client = await socket.AcceptAsync();
                    ReceiveMessages(client);
                }


            }
        }

        public MainViewModel()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (!Directory.Exists(desktopPath + @"\serverImages"))
            {
                Directory.CreateDirectory(desktopPath + @"\serverImages");
            }
            ClientItems = new ObservableCollection<Item>();
            StartCommand = new RelayCommand((c) =>
            {
                StartAction();
            });

        }


    }
}

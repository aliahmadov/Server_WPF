using Server.Commands;
using Server.Helpers;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public RelayCommand StartCommand { get; set; }

        public Item ClientItem { get; set; }


        private ObservableCollection<Item> clientItems;

        public ObservableCollection<Item> ClientItems
        {
            get { return clientItems; }
            set { clientItems = value; OnPropertyChanged(); }
        }


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





        public async void StartAction()
        {
            var localIP = IPAddress.Parse(GetLocalIPAddress());
            var port = 80;

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                var endPoint = new IPEndPoint(localIP, port);
                socket.Bind(endPoint);
                socket.Listen(10);// maximum amount of clients in the queue connected to the server
                MessageBox.Show($"Listen over {socket.LocalEndPoint}");
                while (true)
                {
                    var client = await socket.AcceptAsync();
                    await Task.Run(() =>
                     {
                         MessageBox.Show($"{client.RemoteEndPoint} connected");
                         var length = 0;
                         var bytes = new byte[1024 * 128];

                         do
                         {
                             length = client.Receive(bytes);
                             var jsonString = Encoding.UTF8.GetString(bytes, 0, length);
                             ClientItem = FileHelper<Item>.Deserialize(jsonString) as Item;
                             ClientItems.Add(ClientItem);

                         } while (true);

                     });
                }


            }
        }

        public MainViewModel()
        {
            StartCommand = new RelayCommand((c) =>
            {
                StartAction();
            });

        }


    }
}

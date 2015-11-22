using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenZWaveDotNet;
using System.Threading;

namespace ZWaveFun
{
    class Program
    {
        static List<Node> _nodeList = new List<Node>();
        static ZWManager _manager;
        static uint _homeId;
        static ManualResetEvent _mre;

        static void Main(string[] args)
        {
            Console.WriteLine("Begin");
            _mre = new ManualResetEvent(false);

            OpenZWaveDotNet.ZWManager manager = new OpenZWaveDotNet.ZWManager();

            var options = new ZWOptions();
            options.Create(@"..\..\config", @"", @"");

            // Add any app specific options here...
            options.AddOptionInt("SaveLogLevel", (int)ZWLogLevel.None);
            // ordinarily, just write "Detail" level messages to the log
            options.AddOptionInt("QueueLogLevel", (int)ZWLogLevel.None);
            // save recent messages with "Debug" level messages to be dumped if an error occurs
            options.AddOptionInt("DumpTriggerLevel", (int)ZWLogLevel.None);
            // only "dump" Debug  to the log emessages when an error-level message is logged

            // Lock the options
            options.Lock();

            _manager = new ZWManager();

            _manager.Create();

            _manager.OnNotification += new ManagedNotificationsHandler(NotificationHandler);

            var driverPort = @"\\.\COM3";
            _manager.AddDriver(driverPort);

            

            _mre.WaitOne();

            var sensor = PrintValuesForDevice("Routing Binary Sensor");
            _manager.SetNodeProductName(sensor.HomeId, sensor.Id, "Garage Door Sensor");

            foreach (var n in _nodeList)
            {
                Console.WriteLine("======================================");
                Console.WriteLine("Label: " + n.Label);
                Console.WriteLine("location: " + n.Location);
                Console.WriteLine("Manufacturer: " + n.ManufacturerName);

                var name = _manager.GetNodeName(sensor.HomeId, n.Id);
                n.Name = name;

                Console.WriteLine("Name: " + n.Name);
                Console.WriteLine("Product: " + n.Product);
                Console.WriteLine("Node Id: " + n.Id);
                Console.WriteLine("======================================");
            }

            // var node = _nodeList.FirstOrDefault(x => x.Product == "45609 On/Off Relay Switch");

            // _manager.SetPollInterval(1000, true);
            var node = PrintValuesForDevice("Binary Power Switch");


            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Getting value of basement light switch:");
            ZWValueID v = node.ValueIds.First(x => _manager.GetValueLabel(x) == "Switch");
            bool ret;
            bool b;
            ret = _manager.GetValueAsBool(v, out b);
            Console.WriteLine("SWITCH: Got bool value of " + b + ", success: " + ret);

            var v2 = sensor.ValueIds.First(x => _manager.GetValueLabel(x) == "Sensor");
            ret = _manager.GetValueAsBool(v2, out b);
            Console.WriteLine("SENSOR: Got bool value of " + b + ", success: " + ret);

            //int i;
            //ret = _manager.GetValueAsInt(v, out i);
            //Console.WriteLine("got in value of " + i + ", success: " + ret);

            //string str;
            //ret = _manager.GetValueAsString(v, out str);
            //Console.WriteLine("got string value of " + str + ", success: " + ret);

            // ret = _manager.SetValue(v, true);
            //Console.WriteLine("Set bool value to false, success: " + ret);


            Console.WriteLine("Press enter...");
            Console.ReadLine();
        }

        public static void NotificationHandler(ZWNotification notification)
        {
            // Console.WriteLine("Notification! " + notification.GetType() + ": NodeID:" + notification.GetNodeId() + ", HomeId:" + notification.GetHomeId());
            var node = FindNode(notification.GetHomeId(), notification.GetNodeId());

            switch (notification.GetType())
            {
                case ZWNotification.Type.AllNodesQueried:
                    {
                        Console.WriteLine("***** AllNodesQueried");
                        _manager.WriteConfig(notification.GetHomeId());
                        _mre.Set();
                        break;
                    }
                case ZWNotification.Type.AllNodesQueriedSomeDead:
                    {
                        Console.WriteLine("Ready:  All nodes queried but some are dead.");
                        _manager.WriteConfig(notification.GetHomeId());
                        break;
                    }
                case ZWNotification.Type.AwakeNodesQueried:
                    {
                        Console.WriteLine("Ready:  Awake nodes queried (but not some sleeping nodes).");
                        _manager.WriteConfig(notification.GetHomeId());
                        _mre.Set();
                        break;
                    }
                case (ZWNotification.Type.NodeAdded):
                    {
                        node.Id = notification.GetNodeId();
                        node.HomeId = notification.GetHomeId();
                        //FillInfo(node);
                        break;
                    }

                case (ZWNotification.Type.NodeNaming):
                    {
                        Console.WriteLine("Node naming event!");
                        node.ManufacturerName = _manager.GetNodeManufacturerName(node.HomeId, node.Id);
                        node.Product = _manager.GetNodeProductName(node.HomeId, node.Id);
                        node.Location = _manager.GetNodeLocation(node.HomeId, node.Id);
                        node.Name = _manager.GetNodeName(node.HomeId, node.Id);
                        Console.WriteLine("Product: " + node.Product + ", Location:" + node.Location + ", Name:" + node.Name);
                        break;
                    }

                case ZWNotification.Type.NodeProtocolInfo:
                    {
                        node.Label = _manager.GetNodeType(node.HomeId, node.Id);
                        Console.WriteLine("***********************");
                        Console.WriteLine("NodeProtocolInfo: label is " + node.Label);
                        Console.WriteLine("***********************");
                        break;
                    }

                case ZWNotification.Type.PollingDisabled:
                    {
                        Console.WriteLine("Polling disabled notification");
                        break;
                    }

                case ZWNotification.Type.PollingEnabled:
                    {
                        Console.WriteLine("Polling enabled notification");
                        break;
                    }

                case ZWNotification.Type.DriverReady:
                    {
                        _homeId = notification.GetHomeId();
                        // Console.WriteLine("Home Id is :" + _homeId);
                        break;
                    }
                case ZWNotification.Type.NodeQueriesComplete:
                    {
                        Console.WriteLine(node.Label + ": node queries complete");
                        break;
                    }
                case ZWNotification.Type.EssentialNodeQueriesComplete:
                    {
                        Console.WriteLine(node.Label + ": essential node queries complete");
                        break;
                    }

                case ZWNotification.Type.ValueAdded:
                    {
                        node.ValueIds.Add(notification.GetValueID());
                        break;
                    }

                case ZWNotification.Type.ValueChanged:
                    {
                        Console.WriteLine("");
                        string s;
                        bool b;

                        Console.WriteLine(node.Name + ": " + node.Location);
                        Console.WriteLine("Notification type is " + notification.GetType());
                        var valueId = notification.GetValueID();
                        var valueType = notification.GetValueID().GetType();
                        _manager.GetValueAsString(valueId, out s);
                        _manager.GetValueAsBool(valueId, out b);
                        byte bt;
                        _manager.GetValueAsByte(valueId, out bt);
                        Console.WriteLine("Value Type: " + valueType);
                        Console.WriteLine(System.DateTime.Now +     "** VALUE CHANGED ** <<" + node.Label + ">> <<" + notification.GetValueID().GetId().ToString() + ">> string:" + s + ",bool:" + b + ",byte=" + bt); 
                        Console.WriteLine("Genre: " + valueId.GetGenre().ToString());
                        Console.WriteLine("");
                        break;
                    }

            }
        }

        public static void FillInfo(Node node)
        {
           node.ManufacturerName = _manager.GetNodeManufacturerName(node.HomeId, node.Id);
            Console.WriteLine("Found manufacturer name: " + node.ManufacturerName);
        }

        public static Node FindNode(uint homeId, byte Id)
        {
            var node =  _nodeList.Where(x => x.HomeId == homeId && x.Id == Id).FirstOrDefault();
            if (node == null)
            {
                node = new Node { HomeId = homeId, Id = Id };
                _nodeList.Add(node);
            }
            return node;
        }

        public static Node PrintValuesForDevice(string label)
        {
            var node = _nodeList.FirstOrDefault(x => x.Label == label);

            Console.WriteLine("");
            Console.WriteLine("Value types for " + label);
            Console.WriteLine("");

            foreach (var vid in node.ValueIds)
            {
                
                Console.WriteLine("Value type is " + vid.GetType());
                Console.WriteLine("Value units is " + _manager.GetValueUnits(vid));
                Console.WriteLine("Value Genre is " + vid.GetGenre());
                Console.WriteLine("Value index is " + vid.GetIndex());
                Console.WriteLine("Value instance is " + vid.GetInstance());
                Console.WriteLine("Value label is " + _manager.GetValueLabel(vid));
                Console.WriteLine("Value genre is " + vid.GetGenre().ToString());
                string s;
                _manager.GetValueAsString(vid, out s);
                Console.WriteLine("Value string is " + s);
                Console.WriteLine("----------------");
            }
            return node;
        }

    }

    public class Node
    {
        public Node()
        {
            ValueIds = new List<ZWValueID>();
        }
        public uint HomeId { get; set; }
        public byte Id { get; set; }
        public string ManufacturerName { get; set; }
        public string Label { get; set; }
        public string Product { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public List<ZWValueID> ValueIds { get; private set; }
    }
}

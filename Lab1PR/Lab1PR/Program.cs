using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections;
using System.Collections.ObjectModel;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml;
using System.Threading;


namespace Lab1PR
{
    public class TableData
    {
        public List<string> columns = new List<string>();

        public List<List<string>> data = new List<List<string>>(); //data is a list of lists, because it has keys and values for the keys. I could also be implemented as a dictionary

        //public Dictionary<string,string> data = new Dictionary<string, string>();
    }

    public static class Helpers//this class contains methods which return the TableData with values, through returnData object
    {
        public static TableData GetTableData(JObject rawObject) //this method returns the TableData with values for every item
        {
            TableData returnData = null;

            var mime_type = rawObject["mime_type"];
            var data = rawObject["data"];


            if (mime_type == null)
            {
                return returnData = ObjArrayToTD(data.ToString());
            }
            else
                if (mime_type.ToString() == "application/xml")
            {
                return returnData = GetXMLToTD(data.ToString());
            }
            /* else
             if (mime_type.ToString() == "text/csv")
             {
                 return returnData = GetCSVToTD(data.ToString());
             }*/


            return null;
        }

        private static TableData ObjArrayToTD(string rawString)//this method returns TD with all names of columns, after deserializing string to object.
        {
            TableData returnData = new TableData();
            var array = (JArray)JsonConvert.DeserializeObject(rawString);
            foreach (var item in array.Children<JObject>())
            {
                var tempItem = new List<string>();//a temporary list is created, for storing every value returned by foreach loop
                foreach (var value in item)
                {
                    if (!returnData.columns.Contains(value.Key))
                    {
                        returnData.columns.Add(value.Key);
                    }
                    tempItem.Add(value.Value.ToString());
                }
                returnData.data.Add(tempItem);
            }

            return returnData;
        }
        private static TableData GetXMLToTD(string rawString)
        {
            TableData returnData = new TableData();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(rawString);
            var nodes = xml.SelectSingleNode("dataset");
            foreach (XmlNode record in nodes.ChildNodes)
            {
                var tempItem = new List<string>();
                foreach (XmlNode value in record.ChildNodes)
                {
                    if (!returnData.columns.Contains(value.Name))
                    {
                        returnData.columns.Add(value.Name);
                    }
                    tempItem.Add(value.InnerText);
                }
                returnData.data.Add(tempItem);
            }


            return returnData;
        }

        /*private static TableData GetCSVToTD(string rawString)
        {
            TableData returnData = new TableData();
            var array = new List<string>(rawString.Split('\n'));
            var temp = array[0].Split(',');
            var temp2 = array[0];

            foreach (var column in array[0].Split(','))
            {
                returnData.columns.Add(column);

            }
            array.RemoveAt(0);

            foreach (var record in array)
            {
                var tempItem = new List<string>();
                foreach (var i in record.Split(','))
                {
                    tempItem.Add(i);

                }
                returnData.data.Add(tempItem);
            }

            return returnData;
        }*/





        public static List<string> GetColumn(List<TableData> list, string columnName)//method for finding the column by columnName
        {
            List<string> response = new List<string>();//initialize a new list where we'll store

            foreach (var table in list)
            {
                if (table != null)
                {
                    if (columnName != null)
                    {
                        if (table.columns.Contains(columnName))//verify if there's a column which contains the string given by the user
                        {
                            var index = table.columns.IndexOf(columnName);//gets the index of that found column

                            foreach (var record in table.data)
                            {
                                if (record.Count > index)
                                {
                                    response.Add(record[index]);
                                }
                            }
                        }
                    }
                }
            }


            if (response.Count == 0)
            {
                response.Add("Column does not exist");
            }
            return response;
        }
    }

    class ServerSocket
    {
        class PacketHandler
        {
            static class ProcessCommand
            {
                public static byte[] Execute(string commandStr)
                {
                    Console.WriteLine("Command received: " + commandStr);

                    string[] args = commandStr.Split(' ');
                    string command = args[0];
                    string answer = "Command not valid";
                    string[] columnNames;
                    List<string> columns = new List<string>();
                    Console.WriteLine(args.Length);

                    switch (command)
                    {
                        case "returncolumn":
                            try
                            {
                                string columnParams = commandStr.Substring(commandStr.IndexOf(' ')).Trim();

                                

                                if (columnParams == "all" || columnParams == "*")
                                {
                                    
                                    foreach (var table in Program.fetchedResult)
                                    {
                                        if (table != null)
                                        {
                                            foreach (var column in table.columns)
                                            {
                                                if (column != null)
                                                    columns.Add(column);
                                            }
                                        }

                                    }
                                    columnNames = columns.Distinct<string>().ToArray();
                                }
                                else
                                    columnNames = columnParams.Split(',');

                                //Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
                                List<List<string>> result = new List<List<string>>();

                                foreach (string columnName in columnNames)
                                {
                                    List<string> res = Helpers.GetColumn(Program.fetchedResult, columnName.Trim());
                                    result.Add(res);
                                }


                                answer = JArray.FromObject(result).ToString();




                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message + " \n " + e.StackTrace);
                                answer = "Try again. An  internal error occured.";
                            }
                            break;
                        case "getcolumns":
                            
                                foreach (var table in Program.fetchedResult)
                                {
                                    if (table != null)
                                    {
                                        foreach (var column in table.columns)
                                        {
                                            if (column != null)
                                                columns.Add(column);
                                        }
                                    }

                                }
                            columns = columns.Distinct<string>().ToList<string>();
                                answer = String.Empty;
                            foreach (var item in columns)
                            {
                                answer = answer + ", "+item;
                            }
                            answer = String.Format("\nColumns:  {0}\n",answer.Substring(2));
                                break;
                        default:
                            break;
                    }



                    byte[] packet = new byte[answer.Length + 2]; //2 bytes for the package length

                    byte[] packetLength = BitConverter.GetBytes((ushort)answer.Length);

                    Array.Copy(packetLength, packet, 2);
                    Array.Copy(Encoding.ASCII.GetBytes(answer), 0, packet, 2, answer.Length);

                    return packet;

                }
            }

            public static void Handle(byte[] packet, Socket clientSocket)
            {
                ushort packetLength = BitConverter.ToUInt16(packet, 0);
                byte[] data = new byte[packetLength];
                Array.Copy(packet, 2, data, 0, packetLength);
                string commandName = Encoding.Default.GetString(data);
                byte[] res = ProcessCommand.Execute(commandName); // a reason why ProcessCommand is an inner class
                clientSocket.Send(res);
            }
        }
        class ConnectionInfo
        {
            public byte[] data = new byte[8192];
            public Socket socket;
            public const int BUFF_SIZE = 8192;
        }



        private const int DEFAULT_CONNECTIONS_NR = 50;
        private Socket serverSocket;

        public ServerSocket()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Bind(string ipString, int port)
        {
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ipString), port));
        }

        public void Listen(int nrOfConnections = DEFAULT_CONNECTIONS_NR)
        {
            serverSocket.Listen(nrOfConnections);
        }

        public void Accept()
        {
            serverSocket.BeginAccept(AcceptedCallback, null);
        }

        private void AcceptedCallback(IAsyncResult result)
        {
            try
            {
                ConnectionInfo connection = new ConnectionInfo();

                connection.socket = serverSocket.EndAccept(result);
                connection.data = new byte[ConnectionInfo.BUFF_SIZE];
                connection.socket.BeginReceive(connection.data, 0, connection.data.Length, SocketFlags.None, ReceiveCallback, connection);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't accept, {e.Message}");
            }
            finally
            {
                Accept();
            }

        }

        private void ReceiveCallback(IAsyncResult result)
        {
            ConnectionInfo connection = result.AsyncState as ConnectionInfo;

            try
            {
                Socket clientSocket = connection.socket;
                SocketError response;
                int buffSize = clientSocket.EndReceive(result, out response);

                if (response == SocketError.Success)
                {
                    byte[] packet = new byte[buffSize];
                    Array.Copy(connection.data, packet, packet.Length);

                    PacketHandler.Handle(packet, clientSocket);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't receive data from Client, {e.Message}");
            }
            finally
            {
                try
                {
                    connection.socket.BeginReceive(connection.data, 0, connection.data.Length, SocketFlags.None, ReceiveCallback, connection);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                    connection.socket.Close();
                }
            }
        }
    }

    class Program
    {
        public static List<TableData> fetchedResult = new List<TableData>();
        public static int isDone = 0;
        public static List<string> finalResult = new List<string>();
        public static Stopwatch timer = new Stopwatch();
        private static ServerSocket socket = new ServerSocket();

        public static void SendRequest(string route, string access_token)
        {
            using (var httpClient = new HttpClient())
            {
                Console.WriteLine("Accessing " + route + "...");
                httpClient.DefaultRequestHeaders.Add("X-Access-Token", access_token);
                string responseBody = httpClient.GetStringAsync("http://localhost:5000" + route).Result;
                JObject jsonResponse = JObject.Parse(responseBody);

                //Console.WriteLine(responseBody + "qqq");
                if (jsonResponse["link"] != null)
                {
                    JObject linkNodes = (JObject)jsonResponse["link"];
                    foreach (var item in linkNodes.Children())
                    {
                        SendRequestWithNewThread(item.First.ToString(), access_token);
                        //ThreadPool.QueueUserWorkItem(state => SendRequest(item.First.ToString(), access_token));

                    }
                }
                if (jsonResponse["data"] != null)
                {
                    fetchedResult.Add(Helpers.GetTableData(jsonResponse));
                    finalResult.Add(jsonResponse["data"].ToString());
                }

            }
        }



        public static void SendRequestWithNewThread(string route, string access_token)
        {
            var thread = new System.Threading.Thread(() =>
            {
                isDone++;
                SendRequest(route, access_token);
                isDone--;
                if (isDone == 0)
                {
                    timer.Stop();
                    outputResponse();
                    //startServer();
                }
            });
            thread.Start();
        }


        public static void outputResponse()
        {
            Console.WriteLine("Done!!!");
            finalResult.ForEach(x => Console.WriteLine(x + "\n******************************************************************************"));
            Console.Write("Process done in " + timer.Elapsed.Seconds + " seconds.");
        }

        public static void startServer()
        {
            socket.Bind("127.0.0.1", 9001);
            socket.Listen();
            socket.Accept();
            Console.WriteLine("\n\nServer started and listening ");
            Console.ReadLine();

        }

        static void Main(string[] args)
        {
            string access_token = String.Empty;
            string homeURL = "/home";
            using (var httpClient = new HttpClient()) //for sending/receiving the HTTP requests/responses from a URL
            {
                string responseBody = httpClient.GetStringAsync("http://localhost:5000/register").Result;
                JObject json = JObject.Parse(responseBody);
                access_token = json["access_token"].ToString();
                timer.Start();
                SendRequest(homeURL, access_token);


            }
            //timer.Stop();
            //outputResponse();

            Console.ReadLine();
            startServer();















            //connect to the server
            string sURL;
            sURL = "http://localhost:5000/";

            WebRequest wrGETURL;
            wrGETURL = WebRequest.Create(sURL); //creates URL


            //get json answer
            Stream objStream;
            objStream = wrGETURL.GetResponse().GetResponseStream(); //accesses URL

            StreamReader objReader = new StreamReader(objStream);

            string sLine = objReader.ReadLine(); //reads the content of the filerite
            Console.WriteLine(sLine);


            //Deserializes json into a dynamic object
            var serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new[] { new DynamicJsonConverter() });

            dynamic obj = serializer.Deserialize(sLine, typeof(object)); //obj 



            //parse json answer to get the authentication ticket
            string registerURL = String.Format("http://localhost:5000{0}", obj.register.link);

            wrGETURL = WebRequest.Create(registerURL);

            objStream = wrGETURL.GetResponse().GetResponseStream();
            DateTime dt_start = DateTime.Now;

            objReader = new StreamReader(objStream);

            sLine = objReader.ReadLine();
            Console.WriteLine("\n" + sLine);

            serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new[] { new DynamicJsonConverter() });

            obj = serializer.Deserialize(sLine, typeof(object));

            string accessToken = obj.access_token;
            homeURL = obj.link;




            //access server with authentication ticket to get the answer
            sURL = String.Format("http://localhost:5000{0}", homeURL);

            wrGETURL = WebRequest.Create(sURL);
            wrGETURL.Headers.Add("X-Access-Token", accessToken);//we add a specification for the server, called header, which in our case is the access token

            objStream = wrGETURL.GetResponse().GetResponseStream();

            objReader = new StreamReader(objStream);

            sLine = objReader.ReadLine();
            Console.WriteLine("\n" + sLine);

            serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new[] { new DynamicJsonConverter() });

            obj = serializer.Deserialize(sLine, typeof(object));




            Dictionary<string, string> nodes = new Dictionary<string, string>();

            // put your JSON object here
            JObject rootObject = JObject.Parse(sLine);
            ParseJson(rootObject, nodes);
            // nodes dictionary contains xpath-like node locations
            Console.WriteLine("");
            Console.WriteLine("JSON:");

            Dictionary<string, string> routes = new Dictionary<string, string>();
            foreach (string key in nodes.Keys)
            {
                if (key.Contains("link"))
                {
                    string newURL = nodes[key];

                    Console.WriteLine(sURL = String.Format("http://localhost:5000{0}", newURL));
                    wrGETURL = WebRequest.Create(sURL);
                    wrGETURL.Headers.Add("X-Access-Token", accessToken);
                    objStream = wrGETURL.GetResponse().GetResponseStream();

                    objReader = new StreamReader(objStream);

                    sLine = objReader.ReadLine();

                    routes[key] = sLine;
                }
            }

            foreach (string key in nodes.Keys)
            {
                Console.WriteLine(key + " = " + nodes[key]);
                if (key.Contains("link"))
                {
                    string newURL = nodes[key];

                    Console.WriteLine(sURL = String.Format("http://localhost:5000{0}", newURL));
                    wrGETURL = WebRequest.Create(sURL);
                    wrGETURL.Headers.Add("X-Access-Token", accessToken);
                    objStream = wrGETURL.GetResponse().GetResponseStream();

                    objReader = new StreamReader(objStream);

                    sLine = objReader.ReadLine();
                    Dictionary<string, string> subnodes = new Dictionary<string, string>();
                    rootObject = JObject.Parse(sLine);
                    ParseJson(rootObject, subnodes);
                    // nodes dictionary contains xpath-like node locations
                    Console.WriteLine("");
                    Console.WriteLine("JSON:");
                    foreach (string subKey in subnodes.Keys)
                    {
                        Console.WriteLine(subKey + " = " + subnodes[subKey]);
                        if (subKey.Contains("link"))
                        {
                            string newSubURL = subnodes[subKey];

                            Console.WriteLine(sURL = String.Format("http://localhost:5000{0}", newSubURL));
                            wrGETURL = WebRequest.Create(sURL);
                            wrGETURL.Headers.Add("X-Access-Token", accessToken);
                            objStream = wrGETURL.GetResponse().GetResponseStream();

                            objReader = new StreamReader(objStream);

                            sLine = objReader.ReadLine();
                            Dictionary<string, string> sub2nodes = new Dictionary<string, string>();
                            //Dictionary<string, string> subnodes = new Dictionary<string, string>();
                            rootObject = JObject.Parse(sLine);
                            ParseJson(rootObject, sub2nodes);
                            // nodes dictionary contains xpath-like node locations
                            Console.WriteLine("");
                            Console.WriteLine("JSON:");
                            foreach (string sub2Key in sub2nodes.Keys)
                            {
                                Console.WriteLine(sub2Key + " = " + sub2nodes[sub2Key]);
                            }
                        }
                    }
                }
                /*if (nodes[key] == "/route/4")
                {
                    string newURL = nodes[key];
                    Console.WriteLine(sURL = String.Format("http://localhost:5000{0}", newURL));
                    wrGETURL = WebRequest.Create(sURL);
                    wrGETURL.Headers.Add("X-Access-Token", accessToken);
                    objStream = wrGETURL.GetResponse().GetResponseStream();
                    objReader = new StreamReader(objStream);
                    sLine = objReader.ReadLine();
                    Dictionary<string, string> subnodes = new Dictionary<string, string>();
                    rootObject = JObject.Parse(sLine);
                    ParseJson(rootObject, subnodes);
                    // nodes dictionary contains xpath-like node locations
                    Console.WriteLine("");
                    Console.WriteLine("JSON:");
                    foreach (string subKey in subnodes.Keys)
                    {
                        Console.WriteLine(subKey + " = " + subnodes[subKey]);
                    }
                }*/

                Console.WriteLine(DateTime.Now - dt_start);

            }
            //Int32 length = nodes.Count;
            //Console.WriteLine(length);







            // sURL = String.Format("http://localhost:5000{0}", newURL);

            // wrGETURL = WebRequest.Create(sURL);
            //wrGETURL.Headers.Add("X-Access-Token", accessToken);//we add a specification for the server, called header, which in our case is the access token

            //objStream = wrGETURL.GetResponse().GetResponseStream();

            //objReader = new StreamReader(objStream);

            //sLine = objReader.ReadLine();
            //serializer = new JavaScriptSerializer();
            //serializer.RegisterConverters(new[] { new DynamicJsonConverter() });

            //obj = serializer.Deserialize(sLine, typeof(object));

            //newURL = obj.link;


            //access routes



            //render the answer on the console screen
            //create TCP server to serve the answer
            //answer to concurrent TCP requests
            Console.WriteLine("Press ENTER to exit from the application.");
            Console.ReadLine();
        }
        static bool ParseJson(JToken token, Dictionary<string, string> nodes, string parentLocation = "")
        {
            if (token.HasValues)
            {
                foreach (JToken child in token.Children())
                {
                    if (token.Type == JTokenType.Property)
                    {
                        if (parentLocation == "")
                        {
                            parentLocation = ((JProperty)token).Name;
                        }
                        else
                        {
                            parentLocation += "." + ((JProperty)token).Name;
                        }
                    }

                    ParseJson(child, nodes, parentLocation);
                }

                // we are done parsing and this is a parent node
                return true;
            }
            else
            {
                // leaf of the tree
                if (nodes.ContainsKey(parentLocation))
                {
                    // this was an array
                    nodes[parentLocation] += "|" + token.ToString();
                }
                else
                {
                    // this was a single property
                    nodes.Add(parentLocation, token.ToString());
                }

                return false;
            }
        }
    }






    public sealed class DynamicJsonConverter : JavaScriptConverter
    {
        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            return type == typeof(object) ? new DynamicJsonObject(dictionary) : null;
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Type> SupportedTypes
        {
            get { return new ReadOnlyCollection<Type>(new List<Type>(new[] { typeof(object) })); }
        }

        #region Nested type: DynamicJsonObject

        private sealed class DynamicJsonObject : DynamicObject
        {
            private readonly IDictionary<string, object> _dictionary;

            public DynamicJsonObject(IDictionary<string, object> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException("dictionary");
                _dictionary = dictionary;
            }

            public override string ToString()
            {
                var sb = new StringBuilder("{");
                ToString(sb);
                return sb.ToString();
            }

            private void ToString(StringBuilder sb)
            {
                var firstInDictionary = true;
                foreach (var pair in _dictionary)
                {
                    if (!firstInDictionary)
                        sb.Append(",");
                    firstInDictionary = false;
                    var value = pair.Value;
                    var name = pair.Key;
                    if (value is string)
                    {
                        sb.AppendFormat("{0}:\"{1}\"", name, value);
                    }
                    else if (value is IDictionary<string, object>)
                    {
                        new DynamicJsonObject((IDictionary<string, object>)value).ToString(sb);
                    }
                    else if (value is ArrayList)
                    {
                        sb.Append(name + ":[");
                        var firstInArray = true;
                        foreach (var arrayValue in (ArrayList)value)
                        {
                            if (!firstInArray)
                                sb.Append(",");
                            firstInArray = false;
                            if (arrayValue is IDictionary<string, object>)
                                new DynamicJsonObject((IDictionary<string, object>)arrayValue).ToString(sb);
                            else if (arrayValue is string)
                                sb.AppendFormat("\"{0}\"", arrayValue);
                            else
                                sb.AppendFormat("{0}", arrayValue);

                        }
                        sb.Append("]");
                    }
                    else
                    {
                        sb.AppendFormat("{0}:{1}", name, value);
                    }
                }
                sb.Append("}");
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                if (!_dictionary.TryGetValue(binder.Name, out result))
                {
                    // return null to avoid exception.  caller can check for null this way...
                    result = null;
                    return true;
                }

                result = WrapResultObject(result);
                return true;
            }

            public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
            {
                if (indexes.Length == 1 && indexes[0] != null)
                {
                    if (!_dictionary.TryGetValue(indexes[0].ToString(), out result))
                    {
                        // return null to avoid exception.  caller can check for null this way...
                        result = null;
                        return true;
                    }

                    result = WrapResultObject(result);
                    return true;
                }

                return base.TryGetIndex(binder, indexes, out result);
            }

            private static object WrapResultObject(object result)
            {
                var dictionary = result as IDictionary<string, object>;
                if (dictionary != null)
                    return new DynamicJsonObject(dictionary);

                var arrayList = result as ArrayList;
                if (arrayList != null && arrayList.Count > 0)
                {
                    return arrayList[0] is IDictionary<string, object>
                        ? new List<object>(arrayList.Cast<IDictionary<string, object>>().Select(x => new DynamicJsonObject(x)))
                        : new List<object>(arrayList.Cast<object>());
                }

                return result;
            }
        }

        #endregion
    }
}

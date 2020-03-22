namespace Client
{
    using Core;

    using System;
    using System.Net;

    public class Client
    {
        public static void Main()
        {
            #region MyRegion

            /*
            {
                var jsonOptions = new JsonSerializerOptions();

                //jsonOptions.Converters.Add(new CustomJsonConverter());

                ISerializer serializer = new Json_Serializer(jsonOptions);

                var TestClass = new TestClass()
                {
                    Number = int.MaxValue,
                    Text = "465",
                    Bool = true,
                    Enumerable = new List<int>()
                    {
                        1,
                        4,
                        3,
                        5,
                    },
                };


                var b = serializer.Serialize(TestClass);

                JsonElement s = serializer.Deserialize<JsonElement>(b);

                object sTestCopy = Activator.CreateInstance<TestClass>();
                Type type = sTestCopy.GetType();

                var props = type.GetProperties();


                foreach (var prop in props)
                {
                    bool bb = s.TryGetProperty(prop.Name, out JsonElement jsonElement);

                    if (bb == true)
                    {
                        switch (jsonElement.ValueKind)
                        {
                            case JsonValueKind.String:
                            {
                                prop.SetValue(sTestCopy, jsonElement.GetString());
                                break;
                            };

                            case JsonValueKind.Number:
                            {
                                if (jsonElement.TryGetByte(out byte byteValue))
                                {
                                    prop.SetValue(sTestCopy, byteValue);
                                }
                                else if (jsonElement.TryGetInt16(out short shortValue))
                                {
                                    prop.SetValue(sTestCopy, shortValue);
                                }
                                else if (jsonElement.TryGetInt32(out int intValue))
                                {
                                    prop.SetValue(sTestCopy, intValue);
                                };

                                break;
                            };

                            case JsonValueKind.Array:
                            {
                                int counter = 0;


                                foreach (var element in jsonElement.EnumerateArray())
                                {
                                    switch (element.ValueKind)
                                    {
                                        case JsonValueKind.String:
                                        {
                                            prop.SetValue(sTestCopy, element.GetString(), 3);
                                            break;
                                        };

                                        case JsonValueKind.Number:
                                        {
                                            if (element.TryGetByte(out byte byteValue))
                                            {
                                                prop.SetValue(sTestCopy, byteValue, null);
                                            }
                                            else if (element.TryGetInt16(out short shortValue))
                                            {
                                                prop.SetValue(sTestCopy, shortValue, null);
                                            }
                                            else if (element.TryGetInt32(out int intValue))
                                            {
                                                prop.SetValue(sTestCopy, intValue, null);
                                            };

                                            break;
                                        };

                                        case JsonValueKind.True:
                                        {
                                            prop.SetValue(sTestCopy, element.GetBoolean(), null);
                                            break;
                                        };

                                        case JsonValueKind.False:
                                        {
                                            prop.SetValue(sTestCopy, element.GetBoolean(), null);
                                            break;
                                        };
                                    };

                                    counter++;
                                };
                                //prop.SetValue(sTestCopy, 
                                break;
                            }

                            case JsonValueKind.True:
                            {
                                prop.SetValue(sTestCopy, jsonElement.GetBoolean());
                                break;
                            };

                            case JsonValueKind.False:
                            {
                                prop.SetValue(sTestCopy, jsonElement.GetBoolean());
                                break;
                            };
                        };

                    };
                };
            };
            */

            /*
            {
                ISerializer serializer = new Json_Serializer();

                var testClass = new TestClass()
                {
                    Number = int.MaxValue,
                    Text = "465",
                    Bool = true,
                    Enumerable = new List<int>()
                    {
                        1,
                        4,
                        3,
                        5,
                    },
                };

                var json = JsonSerializer.Serialize(testClass)
               .Insert(1, "\"" + typeof(TestClass).AssemblyQualifiedName + "\"" + ":{");
                json = json.Insert(json.Length, "}");

                // Server code

                int index = json.IndexOf(':');
                string typeName = json.Substring(2, index - 3);

                var pureJson = json.Remove(0, typeName.Length + 4);
                pureJson = pureJson.Remove(pureJson.Length - 1);

                var objType = Type.GetType(typeName);

                var obj = JsonSerializer.Deserialize(pureJson, objType);


            };
            */

            #endregion

            Console.WriteLine("Press enter to connec");
            Console.ReadLine();

            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            const int port = 5500;

            IPEndPoint ipEndPoint = new IPEndPoint(iPAddress, port);

            TestTcpClient client = new TestTcpClient(ipEndPoint, new Json_Serializer());

            client.AddReceivedEvent("Event1", () =>
            {
                Console.WriteLine("Event1 was called");
            })
            .AddReceivedEvent("Event2", () =>
            {
                Console.WriteLine("Event2 was called");
            });

            client.InitializeConnection();


            while (true)
            {
                Console.WriteLine("Press enter to send");
                Console.ReadLine();

                string s = client.Send<TestClass, string>("Controller/Action2",
                    new TestClass()
                    {
                        Text = "asdasfdfads",
                        Number = int.MaxValue,
                        Bool = true,
                        Enumerable = new[] { 1, 2, 4, 8, 16 }
                    });

                Console.WriteLine($"Received {s}");
            };

        }
    };
};


/*
public TReturn Send<T, TReturn>(T obj)
{
    byte[] buffer = new byte[1024];
    List<byte> completeRequest = new List<byte>();


    NetworkStream networkStream = _client.GetStream();

    _handleReceivedEvents = false;

    _client.Client.Send(_serializer.Serialize(new NetworkMessage()
    {
        Message = obj
    }));

    while (networkStream.DataAvailable == false)
        Thread.Sleep(1);


    while (networkStream.DataAvailable == true)
    {
        int readBytes = networkStream.Read(buffer, 0, buffer.Length);

        for (int a = 0; a < readBytes; a++)
        {
            completeRequest.Add(buffer[a]);
        };
    };

    TReturn data = _serializer.Deserialize<TReturn>(completeRequest.ToArray());

    _handleReceivedEvents = true;

    return data;
}
*/

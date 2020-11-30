using Ceremony;
using Ceremony.Core;
using Ceremony.Cryptography.ECC;
using Ceremony.IO.Json;
using Ceremony.IO.MySQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CeremonyServer.Network 
{
    class RpcServer : IDisposable
    {
        private IWebHost host;

        private MainWindow Window = null;

        public RpcServer(MainWindow window)
        {
            Window = window;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
        private static JObject CreateResponse(JObject id)
        {
            JObject response = new JObject();
            response["jsonrpc"] = "2.0";
            response["id"] = id;
            return response;
        }
        private static JObject CreateErrorResponse(JObject id, int code, string message, JObject data = null)
        {
            JObject response = CreateResponse(id);
            response["error"] = new JObject();
            response["error"]["code"] = code;
            response["error"]["message"] = message;
            if (data != null)
                response["error"]["data"] = data;
            return response;
        }

        private async Task ProcessAsync(HttpContext context)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";
            if (context.Request.Method != "GET" && context.Request.Method != "POST") return;
            JObject request = null;
            if (context.Request.Method == "POST")
            {
                using (StreamReader reader = new StreamReader(context.Request.Body))
                {
                    try
                    {
                        request = JObject.Parse(reader);
                    }
                    catch (FormatException) { }
                }
            }
            JObject response;
            //LogUtil.Default.Log("Request: " + request == null ? "null" : request.ToString());
            if (request == null)
            {
                response = CreateErrorResponse(null, -32700, "Parse error");
            }
            else if (request is JArray array)
            {
                if (array.Count == 0)
                {
                    response = CreateErrorResponse("CeremonyClient", -32600, "Invalid Request");
                }
                else
                {
                    response = array.Select(p => ProcessRequest(p)).Where(p => p != null).ToArray();
                }
            }
            else
            {
                response = ProcessRequest(request);
            }
            if (response == null || (response as JArray)?.Count == 0) return;
            context.Response.ContentType = "application/json-rpc";
            await context.Response.WriteAsync(response.ToString());
        }

        private JObject ProcessRequest(JObject request)
        {
            if (!request.ContainsProperty("method") || !request.ContainsProperty("params") || !(request["params"] is JObject))
            {
                return CreateErrorResponse("CeremonyClient", -32600, "Invalid Request");
            }
            JObject result = null;
            try
            {
                result = Process(request["method"].AsString(), request["params"]);
            }
            catch (Exception ex)
            {
#if DEBUG
                return CreateErrorResponse("CeremonyClient", ex.HResult, ex.Message, ex.StackTrace);
#else
                return CreateErrorResponse("CeremonyClient", ex.HResult, ex.Message);
#endif
            }
            JObject response = CreateResponse("CeremonyClient");
            response["result"] = result;
            return response;
        }

        JObject Process(string method, JObject param)
        {
            JObject result = new JObject();
            switch (method)
            {
                case "SignUp":
                    result = ProcessSignUp(param);
                    break;
                case "SignIn":
                    result = ProcessSignIn(param);
                    break;
                case "GetInfo":
                    result = ProcessGetInfo(param);
                    break;
                case "CompleteCeremony":
                    result = ProcessCompleteCeremony(param);
                    break;
                case "SendKey":
                    result = ProcessSaveKey(param);
                    break;
                default:
                    result = BuildResponse(false, "This api is not implemented");
                    break;
            }

            return result;
        }

        private JObject BuildResponse(bool result, string message = "", JObject data = null)
        {
            JObject ret = new JObject();
            ret["result"] = result;
            ret["msg"] = message;
            ret["data"] = data == null? "": data;

            return ret;
        }

        public void Start(string[] uriPrefix, string sslCert, string password)
        {
            int port = 10033;
            host = new WebHostBuilder().UseKestrel(options => options.Listen(IPAddress.Any, port, listenOptions =>
            {
                if (!string.IsNullOrEmpty(sslCert))
                    listenOptions.UseHttps(sslCert, password);
            }))
            .Configure(app =>
            {
                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                services.AddResponseCompression(options =>
                {
                    // options.EnableForHttps = false;
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json-rpc" });
                });

                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
            })
            .Build();

            host.Start();
        }

        private JObject ProcessSignUp(JObject param)
        {
            JObject result = null;
            LogUtil.Default.Log("SignUp Request: " + param.ToString());
            using (CeremonySQL ceremonySQL = new CeremonySQL())
            {
                if (ceremonySQL.GetCeremonyStatus() == CeremonyStatus.CEREMONY_STATUS_WAITING)
                {
                    if (ceremonySQL.reconnect().SignUpUser(param))
                    {
                        result = BuildResponse(true);

                        System.Windows.Application.Current.Dispatcher.Invoke(delegate {
                            Window.RefreshView();
                        });
                    }
                    else
                    {
                        result = BuildResponse(false, "Sign up failed.");
                    }
                }
                else
                {
                    result = BuildResponse(false, "Ceremony already has been started.");
                }
            }

            LogUtil.Default.Log("SignUp Response: " + result.ToString());

            return result;
        }

        private JObject ProcessSignIn(JObject param)
        {
            JObject result = null;

            LogUtil.Default.Log("SignIn Request: " + param.ToString());

            using (CeremonySQL ceremonySQL = new CeremonySQL())
            {
                User user = ceremonySQL.SignInUser(param);
                if (user != null)
                {
                    result = BuildResponse(false, "SignIn Successed.");

                    JObject ret = new JObject();
                    ret["name"] = user.Name;
                    ret["country"] = user.Country;
                    ret["order"] = user.Order;
                    ret["total"] = ceremonySQL.reconnect().GetUserIndex();
                    ret["selected_index"] = ceremonySQL.reconnect().GetSelectedUserIndex();

                    result = BuildResponse(true, "", ret);
                }
                else
                {
                    result = BuildResponse(false, "SignIn Failed.");
                }
            }
            
            LogUtil.Default.Log("SignIn Response: " + result.ToString());

            return result;
        }

        private JObject ProcessGetInfo(JObject param)
        {
            JObject ret = new JObject();
            using (CeremonySQL ceremonySQL = new CeremonySQL())
            {
                User selectedUser = ceremonySQL.GetSelectedUser();
                User user = ceremonySQL.reconnect().GetUserByPubkey(param["pubkey"].AsString());
                ret["total_participants"] = ceremonySQL.reconnect().GetUserIndex();
                ret["selected_participants"] = (selectedUser == null ? 0 : selectedUser.Order);
                ret["ceremony_status"] = ceremonySQL.reconnect().GetCeremonyStatus();
                ret["my_status"] = ((int)user.Status);
                ret["segment_number"] = ceremonySQL.reconnect().GetSegmentNumber();
            }
            
            JObject result = BuildResponse(true, "", ret);

            LogUtil.Default.Log(ret.ToString());

            return result;
        }

        private JObject ProcessCompleteCeremony(JObject param)
        {
            try
            {
                string pubkey = param["pubkey"].AsString();
                int nSegmentNumber = int.Parse(param["SegmentNumber"].AsString());

                using (CeremonySQL ceremonySQL = new CeremonySQL())
                {
                    User user = ceremonySQL.GetUserByPubkey(pubkey);

                    if (user == null)
                    {
                        return BuildResponse(false, "This user does not exist.");
                    }

                    if (VerifyConstraint(user.Order, nSegmentNumber - ceremonySQL.reconnect().GetSegmentNumber()) == false)
                    {
                        return BuildResponse(false, "VERIFY=false");
                    }

                    if (!ceremonySQL.reconnect().ToNextUser()/*CeremonySQL.Default.CompleteCeremony()*/)
                    {
                        LogUtil.Default.Log("Next User in database failed.");
                        return BuildResponse(false, "Server internal error.");
                    }

                    if (!ceremonySQL.reconnect().UpdateSegmentNumber(nSegmentNumber))
                    {
                        LogUtil.Default.Log("Update Seg number failed.");
                        return BuildResponse(false, "Server internal error.");
                    }

                    if (nSegmentNumber == Window.nTotalSegments)
                    {
                        if (!ceremonySQL.reconnect().CompleteCeremony())
                        {
                            LogUtil.Default.Log("Complete ceremony in database failed.");
                            return BuildResponse(false, "Server internal error.");
                        }
                        Task.Run(() => {
                            GenerateKeyFromConstraints();
                        });
                    }
                }

                Application.Current.Dispatcher.Invoke(delegate {
                    Window.StartTimeInterval();
                });

                LogUtil.Default.Log("Complete ceremony successed.");
                return BuildResponse(true);
            } 
            catch (Exception ex)
            {
                return BuildResponse(false);
            }

            
        }

        public static byte[] StrToByteArray(string str)
        {
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (int i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("x2"), (byte)i);

            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }

        private byte[] HashFile(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        public void GenerateKeyFromConstraints()
        {
            string path = "crypto//constraint.key";
            CombineAllConstraints(path);
            ExtractResource();
            GenerateKey();
        }

        private void CombineAllConstraints(string path)
        {
            List<User> userList = new List<User>();
            using (CeremonySQL ceremonySQL = new CeremonySQL())
            {
                userList = ceremonySQL.GetAllUser();
            }

            if (File.Exists(path))
                File.Delete(path);

            for (int i = 0; i < userList.Count(); i++)
            {
                try
                {
                    if (userList[i].Status == UserStatus.USER_STATUS_SUCCESSED)
                    {
                        FileStream fs = File.OpenRead("crypto//constraint_" + userList[i].Order + ".key");

                        byte[] buf = new byte[1024 * 1024];
                        int len;

                        while ((len = fs.Read(buf, 0, buf.Length)) > 0)
                        {
                            using (var stream = new FileStream(path, FileMode.Append | FileMode.OpenOrCreate))
                            {
                                stream.Write(buf, 0, len);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    LogUtil.Default.Log("Exception reading User " + userList[i].Name + "'s constraints");
                }
            }
        }

        private void ExtractResource()
        {
            ResourceManager rm = new ResourceManager("CeremonyServer.Properties.Resources", Assembly.GetExecutingAssembly());
            Directory.CreateDirectory("output");
            File.WriteAllBytes("output//snarks.exe", (byte[])rm.GetObject("Quras_snarks"));
            File.WriteAllBytes("output//libcryptoMD.dll", (byte[])rm.GetObject("libcryptoMD"));
            File.WriteAllBytes("output//libsodium.dll", (byte[])rm.GetObject("libsodium"));
            Directory.CreateDirectory("crypto");
        }

        private void GenerateKey()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "output//snarks.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (System.Diagnostics.Process exeProcess = System.Diagnostics.Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                    if (File.Exists("crypto//vk.key"))
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private bool VerifyConstraint(int order, int size)
        {
            ExtractResource();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "output//snarks.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "crypto//constraint_" + order + ".key " + size;
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (System.Diagnostics.Process exeProcess = System.Diagnostics.Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                    return exeProcess.ExitCode == 1;
                }
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        private bool ProcessSaveKey(JObject param)
        {
            try
            {
                if (!Directory.Exists("crypto"))
                {
                    Directory.CreateDirectory("crypto");
                }

                if (param["FileInit"].AsString() == "1")
                {
                    File.Delete("crypto//constraint_" + param["UserId"].AsString() + ".key");
                }

                byte[] buf = StrToByteArray(param["FileData"].AsString());

                using (var stream = new FileStream("crypto//constraint_" + param["UserId"].AsString() + ".key", FileMode.Append | FileMode.OpenOrCreate))
                {
                    stream.Write(buf, 0, buf.Length);
                }
            }
            catch(Exception ex)
            {
                return false;
            }

            return true;
        }
    }
}

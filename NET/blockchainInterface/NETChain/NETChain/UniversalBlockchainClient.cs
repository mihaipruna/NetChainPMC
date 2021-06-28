using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
namespace NETChain
{



    /// <summary>
    /// Submits transactionss on the blockchain using REST API
    /// </summary>
    public static class UnivBlockchainClient
    {

        static List<BlockchainTransaction> failedTrans;
        /// <summary>
        /// Represents a transactio
        /// </summary>
        public class BlockchainTransaction
        {
            public Dictionary<string, string> ParamVals { get; set; }
            public string tranClass { get; set; }
            public string APICommand { get; set; }
            public BlockchainTransaction()
            {
                
                tranClass = "";
                APICommand = "";
                ParamVals = new Dictionary<string, string>();
            }
            
        }
        static bool initOK = false;
  static BlockchainTransaction EncryptTran(BlockchainTransaction stran)
        {
            BlockchainTransaction bt = new BlockchainTransaction();
            bt.APICommand = Crypto.EncryptStringAES(stran.APICommand,"myse");
            bt.tranClass = Crypto.EncryptStringAES(stran.tranClass, "myse");
            foreach (KeyValuePair<string,string> dv in stran.ParamVals)
            {
                bt.ParamVals.Add(Crypto.EncryptStringAES(dv.Key, "myse"), Crypto.EncryptStringAES(dv.Value, "myse"));
            }
            return bt;

        }

        static BlockchainTransaction DecryptTran(BlockchainTransaction stran)
        {
            BlockchainTransaction bt = new BlockchainTransaction();
            bt.APICommand = Crypto.DecryptStringAES(stran.APICommand, "myse");
            bt.tranClass = Crypto.DecryptStringAES(stran.tranClass, "myse");
            foreach (KeyValuePair<string, string> dv in stran.ParamVals)
            {
                bt.ParamVals.Add(Crypto.DecryptStringAES(dv.Key, "myse"), Crypto.DecryptStringAES(dv.Value, "myse"));
            }
            return bt;

        }



        /// IP address of REST API
        /// </summary>
        public static string address { get; set; }
        /// <summary>
        /// Port for REST API
        /// </summary>
        public static string port { get; set; }

        static HttpClient client = new HttpClient();

        static string apia = "";
        static string fresponse="";
        static string JSON = "";

        
       
        public static void InitializeClient(string cAddress, int cPort)
        {

            //initialize daily log
#if DEBUG
            debugger.initlog();
#endif
           
            initOK = true;
            address = cAddress;
            port = cPort.ToString();
            failedTrans = new List<BlockchainTransaction>();
            ReadFailedTrans();

        }
        
        static void ReadFailedTrans()
        {
            string ftf = utils.AssemblyDirectory + @"\ftrans.xml";
#if DEBUG
            debugger.logev("Reading failed trans from "+ftf);
#endif
            
            if (File.Exists(ftf))
            {
                try
                {

                    // Create an instance of StreamReader to read from a file.
                    // The using statement also closes the StreamReader.
                    using (StreamReader sr = new StreamReader(ftf))
                    {

                        Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
                        List<BlockchainTransaction> cfailedTrans = js.Deserialize(sr, typeof(List<BlockchainTransaction>)) as List<BlockchainTransaction>;
#if DEBUG
                        debugger.logev("Found " + cfailedTrans.Count.ToString()+" failed transactions");
#endif

                        failedTrans = new List<BlockchainTransaction>();
                        foreach (BlockchainTransaction bt in cfailedTrans)
                        {
                            failedTrans.Add(DecryptTran(bt));
                        }
                        sr.Close();
                    }
                    //submit them
                    List<BlockchainTransaction> bts = new List<BlockchainTransaction>(failedTrans);
                    failedTrans.Clear();
                    foreach (BlockchainTransaction ba in bts)
                    {
                        string fsr = "";
                        string res = subtran(ba, out fsr);
                        if (res != "OK") failedTrans.Add(ba);
                    }
                    //write if any still failed
                    if (failedTrans.Count == 0)
                    {
                        try
                        {
                            File.Delete(ftf);
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            debugger.logev(ex.ToString());
#endif
                        }
                    }
                    else
                    {
                        try
                        {
                            List<BlockchainTransaction> cfailedTrans = new List<BlockchainTransaction>();
                            foreach (BlockchainTransaction bt in failedTrans)
                            {
                                cfailedTrans.Add(EncryptTran(bt));
                            }
                            string serialized = JsonConvert.SerializeObject(cfailedTrans);
                            System.IO.File.WriteAllText(ftf, serialized);
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            debugger.logev(e.ToString());
#endif
                        }
                    }


                }
                catch (Exception e)
                {
#if DEBUG
                    debugger.logev(e.ToString());
#endif
                }
            }
        }

        /// <summary>
        /// Submits transaction to blockchain
        /// </summary>
        /// <param name="btran">BlockchainTransaction</param>
        /// <param name="fullResponse">outputs server full response</param>
        /// <returns>server reason</returns>
        public static string SubmitTrans(BlockchainTransaction btran,out string fullResponse)
        {
#if DEBUG
            debugger.logev("Submitting transaction: " + JsonConvert.SerializeObject(btran));
#endif
            fullResponse = "";
            ReadFailedTrans();
            return subtran(btran, out fullResponse);

        }

        private static string subtran(BlockchainTransaction btran,out string fullResponse)
        {
            fullResponse = "";
            if (!initOK)
            {
                return "Client not initialized";
            }
            apia = "";
            JSON = "";
            apia = btran.APICommand;
            JSON = BuildJSONSubmission(btran);


            string s = RunAsync().GetAwaiter().GetResult();
            if (s != "OK")
            {
                string ftf = utils.AssemblyDirectory + @"\ftrans.xml";
                //  failedTrans
#if DEBUG
                debugger.logev("Transaction failed, adding to failed transactions file"+ftf);
#endif
                failedTrans.Add(btran);
                
                try
                {
                    List<BlockchainTransaction> cfailedTrans = new List<BlockchainTransaction>();
                    foreach (BlockchainTransaction bt in failedTrans)
                    {
                        cfailedTrans.Add(EncryptTran(bt));
                    }
                    string serialized = JsonConvert.SerializeObject(cfailedTrans);
                    System.IO.File.WriteAllText(ftf, serialized);
                }
                catch (Exception e)
                {
#if DEBUG
                    debugger.logev(e.ToString());
#endif
                }
            }
#if DEBUG
            debugger.logev(s);
#endif
#if DEBUG
            debugger.logev(fullResponse);
#endif
            return s;
        }

        ///// <summary>
        ///// Creates hash from string input
        ///// </summary>
        ///// <param name="input">String</param>
        ///// <returns>Hash string</returns>
        //public static string CreateMD5(string input)
        //{
        //    // Use input string to calculate MD5 hash
        //    using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        //    {
        //        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        //        byte[] hashBytes = md5.ComputeHash(inputBytes);

        //        // Convert the byte array to hexadecimal string
        //        StringBuilder sb = new StringBuilder();
        //        for (int i = 0; i < hashBytes.Length; i++)
        //        {
        //            sb.Append(hashBytes[i].ToString("X"));
        //        }
        //        return sb.ToString().Replace("-", "").ToLowerInvariant();
        //    }
        //}


        /// <summary>
        /// Calculates HASH of file
        /// </summary>
        /// <param name="filename">Existing file</param>
        /// <returns>Hash code</returns>
        public static string CalculateMD5(string filename)
        {
#if DEBUG
            debugger.logev("Hashing "+filename);
#endif 
            using (var md5 = MD5.Create())
            {
                try
                {

                    //copy it
                    string fextension = System.IO.Path.GetExtension(filename);
                    string newfilename = filename.Replace(fextension, "_bchain1234" + fextension);
                    if (File.Exists(newfilename)) File.Delete(newfilename);
                    System.IO.File.Copy(filename, newfilename);

                    //byte[] mybytes = System.IO.File.ReadAllBytes(newfilename);

                    //string fs = System.IO.File

                    //byte[] mybytes = Encoding.ASCII.GetBytes(fs);




                   

                    string myString = "";
                    using (FileStream fs = new FileStream(newfilename, FileMode.Open))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        byte[] bin = br.ReadBytes(Convert.ToInt32(fs.Length));
                        myString = Convert.ToBase64String(bin);
                    }
                    string newfilenametxt = filename.Replace(fextension, "_bchain1234_txt.txt");
                    System.IO.File.WriteAllText(newfilenametxt,myString);


                    //#if DEBUG
                    //                    debugger.logev(myString);
                    //#endif 
                    byte[] mybytes = System.IO.File.ReadAllBytes(newfilenametxt);//Convert.FromBase64String(myString);
                    var hash = md5.ComputeHash(mybytes);
                    string myhash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();


                    File.Delete(newfilename);
                    File.Delete(newfilenametxt);
                  
                  
//#if DEBUG
//                    debugger.logev(smstring);
//#endif 

                   // string myhash= CreateMD5(smstring);
#if DEBUG
                    debugger.logev(myhash);
#endif 


                    return myhash;

                }
                catch (Exception ex)
                {
#if DEBUG
                    debugger.logev(ex.ToString());
#endif
                    return "error";
                }
            }
        }

        static async Task<HttpResponseMessage> SendRequestAsync(string adaptiveUri, string xmlRequest)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                StringContent httpContent = new StringContent(xmlRequest, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                           new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage responseMessage = null;
                try
                {
                    responseMessage = await httpClient.PostAsync(adaptiveUri, httpContent).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (responseMessage == null)
                    {
                        responseMessage = new HttpResponseMessage();
                    }
                    responseMessage.StatusCode = HttpStatusCode.InternalServerError;
                    responseMessage.ReasonPhrase = string.Format("RestHttpClient.SendRequest failed: {0}", ex);
                }
#if DEBUG
                debugger.logev(responseMessage.ReasonPhrase);
#endif
                return responseMessage;
            }
        }


        static async Task<string> RunAsync()
        {
            string aur = "http://" + address + ":" + port + "/" + apia;
            try
            {
                var hrc = await SendRequestAsync(aur, JSON).ConfigureAwait(false);
                fresponse = hrc.Content.ToString();
                return hrc.ReasonPhrase;
            }
            catch(Exception ex)
            {
                fresponse = ex.ToString();
                return "error";
            }
        }


        //not using serialization because of $ for now
        static string BuildJSONSubmission(BlockchainTransaction btran)
        {
            string myJS = "{";
            myJS += "\"" + "$class" + "\"" + ":" + "\"" + btran.tranClass + "\"" + ",";
            
            int ind = 0;
            foreach (KeyValuePair<string, string> kv in btran.ParamVals)
            {
                myJS += "\"" + kv.Key + "\"" + ":" + "\"" + kv.Value + "\"";
                  if (ind<btran.ParamVals.Keys.Count-1)  
                    myJS+= ",";
                ind++;
            }
           
                myJS+= "}";
            return myJS;


        }

        class Crypto
        {

            //While an app specific salt is not the best practice for
            //password based encryption, it's probably safe enough as long as
            //it is truly uncommon. Also too much work to alter this answer otherwise.
            private static byte[] _salt = { 5, 8, 9, 23, 17 ,34,89,87,109,43,189};

            /// <summary>
            /// Encrypt the given string using AES.  The string can be decrypted using 
            /// DecryptStringAES().  The sharedSecret parameters must match.
            /// </summary>
            /// <param name="plainText">The text to encrypt.</param>
            /// <param name="sharedSecret">A password used to generate a key for encryption.</param>
            public static string EncryptStringAES(string plainText, string sharedSecret)
            {
                if (string.IsNullOrEmpty(plainText))
                    throw new ArgumentNullException("plainText");
                if (string.IsNullOrEmpty(sharedSecret))
                    throw new ArgumentNullException("sharedSecret");

                string outStr = null;                       // Encrypted string to return
                RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.

                try
                {
                    // generate the key from the shared secret and the salt
                    Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                    // Create a RijndaelManaged object
                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

                    // Create a decryptor to perform the stream transform.
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for encryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        // prepend the IV
                        msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                //Write all data to the stream.
                                swEncrypt.Write(plainText);
                            }
                        }
                        outStr = Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
                finally
                {
                    // Clear the RijndaelManaged object.
                    if (aesAlg != null)
                        aesAlg.Clear();
                }

                // Return the encrypted bytes from the memory stream.
                return outStr;
            }

            /// <summary>
            /// Decrypt the given string.  Assumes the string was encrypted using 
            /// EncryptStringAES(), using an identical sharedSecret.
            /// </summary>
            /// <param name="cipherText">The text to decrypt.</param>
            /// <param name="sharedSecret">A password used to generate a key for decryption.</param>
            public static string DecryptStringAES(string cipherText, string sharedSecret)
            {
                if (string.IsNullOrEmpty(cipherText))
                    throw new ArgumentNullException("cipherText");
                if (string.IsNullOrEmpty(sharedSecret))
                    throw new ArgumentNullException("sharedSecret");

                // Declare the RijndaelManaged object
                // used to decrypt the data.
                RijndaelManaged aesAlg = null;

                // Declare the string used to hold
                // the decrypted text.
                string plaintext = null;

                try
                {
                    // generate the key from the shared secret and the salt
                    Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                    // Create the streams used for decryption.                
                    byte[] bytes = Convert.FromBase64String(cipherText);
                    using (MemoryStream msDecrypt = new MemoryStream(bytes))
                    {
                        // Create a RijndaelManaged object
                        // with the specified key and IV.
                        aesAlg = new RijndaelManaged();
                        aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                        // Get the initialization vector from the encrypted stream
                        aesAlg.IV = ReadByteArray(msDecrypt);
                        // Create a decrytor to perform the stream transform.
                        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
                finally
                {
                    // Clear the RijndaelManaged object.
                    if (aesAlg != null)
                        aesAlg.Clear();
                }

                return plaintext;
            }

            private static byte[] ReadByteArray(Stream s)
            {
                byte[] rawLength = new byte[sizeof(int)];
                if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
                {
                    throw new SystemException("Stream did not contain properly formatted byte array");
                }

                byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
                if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new SystemException("Did not read byte array properly");
                }

                return buffer;
            }
        }

    }

}

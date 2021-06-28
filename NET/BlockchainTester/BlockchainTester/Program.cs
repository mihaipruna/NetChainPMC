using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETChain;

namespace BlockchainTester
{
    class Program
    {
        static void Main(string[] args)
        {

            string curfile = args[0];
            string prevfile = args[1];

            UnivBlockchainClient.BlockchainTransaction buildtran;
            UnivBlockchainClient.InitializeClient("127.0.0.1", 3000);
            buildtran = new UnivBlockchainClient.BlockchainTransaction();

            buildtran.APICommand = "api/SubmitDocument";

            buildtran.tranClass = "org.pmc.model.SubmitDocument";
            buildtran.ParamVals = new System.Collections.Generic.Dictionary<string, string>();
            buildtran.ParamVals.Add("hashCode", UnivBlockchainClient.CalculateMD5(curfile));
            buildtran.ParamVals.Add("prevHashcode", UnivBlockchainClient.CalculateMD5(prevfile));
            buildtran.ParamVals.Add("submitterEmail", "sarah_engineer@progmodcon.com");
            string fresponse = "";
            string reason = UnivBlockchainClient.SubmitTrans(buildtran, out fresponse);
            Console.Write(reason + " " + fresponse);
            Console.ReadKey();
        }
    }
}

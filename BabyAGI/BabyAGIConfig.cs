using Examples.Demos.FunctionGenerator;
using LombdaAgentSDK.Agents.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI
{
    public class BabyAGIConfig
    {
        public static string ChromaDbURI = "http://localhost:8001/api/v2/";
        public static string FunctionsPath = "C:\\Users\\johnl\\source\\repos\\FunctionApplications";

        public BabyAGIConfig()
        {
            if (!Directory.Exists(FunctionsPath))
            {
                throw new Exception("Functions directory is not setup");
            }
        }


    }
}

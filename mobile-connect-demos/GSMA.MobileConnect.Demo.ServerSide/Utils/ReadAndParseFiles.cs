﻿using System.IO;
using System.Json;
using System.Web.Hosting;
using GSMA.MobileConnect.ServerSide.Web.Objects;
using Newtonsoft.Json;

namespace GSMA.MobileConnect.ServerSide.Web.Utils
{
    public class ReadAndParseFiles
    {
        public OperatorParameters ReadFile(string filePath)
        {
            OperatorParameters operatorParameters;
            using (StreamReader streamReader = new StreamReader(HostingEnvironment.MapPath(filePath)))

            {
                operatorParameters = JsonConvert.DeserializeObject<OperatorParameters>(streamReader.ReadToEnd());
            }
            return operatorParameters;
        }

        public static string ReadFileAsString(string filePath)
        {
            string json;
            using (StreamReader streamReader = new StreamReader(HostingEnvironment.MapPath(filePath)))
            {
                json = streamReader.ReadToEnd();
            }
            return json;
        }
    }
}
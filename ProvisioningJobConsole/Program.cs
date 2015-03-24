﻿using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Framework.ConfigurationModel;
using ProvisioningLibrary;

namespace ProvisioningJobConsole
{
    public class Program
    {
        public static  IConfiguration Configuration { get; set; }
        static void Main()
        {
            // Setup configuration sources.
            Configuration = new Configuration()
                .AddEnvironmentVariables("APPSETTING_");

            var storageCstr = GetConnectionString();

            var host = new JobHost(new JobHostConfiguration(storageCstr));


            //IDictionary<string, string> settings = new Dictionary<string, string>();
            //settings.Add("Provisioning:StorageConnectionString", storageCstr);
            //ProvisioningLibrary.WebJobController w = new WebJobController(settings);
            //w.SubmitActionInQueue("54e490e1-5bbe-44c3-b3c6-c6cfe6880cde", ResourceAction.Start);


            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }

        private static string GetConnectionString()
        {

            var  rv = Configuration["Provisioning:StorageConnectionString"];


            if (string.IsNullOrEmpty(rv))
            {
                throw new ArgumentNullException(@"you're missing StorageConnectionString in either ENV or Config");

            }


            return rv;
        }
    }
}

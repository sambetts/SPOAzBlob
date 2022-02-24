using CommonUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    public abstract class AbstractTest
    {

        protected TestConfig? _config;
        protected DebugTracer _tracer = DebugTracer.ConsoleOnlyTracer();

        [TestInitialize]
        public void Init()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true);


            var config = builder.Build();
            _config = new TestConfig(config);
        }
    }
}

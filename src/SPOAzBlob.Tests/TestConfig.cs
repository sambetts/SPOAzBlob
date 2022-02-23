﻿using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using SPOAzBlob.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    internal class TestConfig : Config
    {
        public TestConfig(IConfiguration config) : base(config)
        {
        }


        [ConfigValue]
        public string TestGraphNotificationEndpoint { get; set; } = string.Empty;
    }
}
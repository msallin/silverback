﻿// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Messages
{
    public class TestRequestCommandOne : ICommand<string>, ITestMessage
    {
        public string Message { get; set; }
    }
}
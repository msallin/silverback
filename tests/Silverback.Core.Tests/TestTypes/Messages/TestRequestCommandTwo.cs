﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Tests.Core.TestTypes.Messages.Base;

namespace Silverback.Tests.Core.TestTypes.Messages
{
    public class TestRequestCommandTwo : IRequest<IEnumerable<string>>, ITestMessage
    {
        public string Message { get; set; }
    }
}
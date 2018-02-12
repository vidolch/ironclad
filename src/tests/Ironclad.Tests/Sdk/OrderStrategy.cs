﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System.Collections.Generic;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class OrderStrategy : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            return testCases;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Services.TestFramework
{
    public abstract class HttpTest : IClassFixture<RunContext>
    {
        public RunContext Context { get; private set; }

        protected HttpTest(RunContext context)
        {
            Context = context;
        }
    }
}
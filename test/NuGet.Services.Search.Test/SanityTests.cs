using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NuGet.Services.TestFramework;
using Xunit;

namespace NuGet.Services.Search.Test
{
    public class SanityTests : HttpTest
    {
        public SanityTests(RunContext context) : base(context) { }

        [Fact]
        public async Task RootDocumentIsWorking()
        {
            await Context.GetJson("/");
        }

        [Fact]
        public async Task SearchRootIsWorking()
        {
            await Context.GetJson("/search");
        }
    }
}

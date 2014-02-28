using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Indexing.Facts
{
    public class BoostFactorsFacts
    {
        public class TheIndexer
        {
            [Fact]
            public void ReturnsDefaultBoostFactorIfNoOtherFactorsAdded()
            {
                // Arrange
                var boost = new BoostFactors();

                // Act
                Assert.Equal(boost["flarg"], BoostFactors.DefaultDefaultBoost, precision: 1);
            }

            [Fact]
            public void ReturnsProvidedBoostFactorIfOneIsProvided()
            {
                // Arrange
                var boost = new BoostFactors();
                boost["flarg"] = 1.2f;

                // Act
                Assert.Equal(boost["flarg"], 1.2f, precision: 1);
            }

            [Fact]
            public void IsCaseInsensitive()
            {
                // Arrange
                var boost = new BoostFactors();
                boost["FLARG"] = 1.2f;

                // Act
                Assert.Equal(boost["flarg"], 1.2f, precision: 1);
            }
        }

        public class TheConstructor
        {
            [Fact]
            public void InitializesBoostsFromDictionary()
            {
                // Arrange
                var boost = new BoostFactors(new Dictionary<string, float>() {
                    {"foo", 2.4f}
                });

                // Act
                Assert.Equal(boost["foo"], 2.4f, precision: 1);
            }

            [Fact]
            public void InitializesBoostsAndDefaultBoost()
            {
                // Arrange
                var boost = new BoostFactors(new Dictionary<string, float>() {
                    {"foo", 2.4f}
                }, defaultBoost: 4.2f);

                // Act
                Assert.Equal(boost["foo"], 2.4f, precision: 1);
                Assert.Equal(boost["bar"], 4.2f, precision: 1);
            }

            [Fact]
            public void InitializesDefaultBoost()
            {
                // Arrange
                var boost = new BoostFactors(defaultBoost: 4.2f);

                // Act
                Assert.Equal(boost["bar"], 4.2f, precision: 1);
            }

            [Fact]
            public void UsesDefaultDefaultBoostIfNoneSpecified()
            {
                // Arrange
                var boost = new BoostFactors();

                // Act
                Assert.Equal(boost["bar"], 1.0f, precision: 1);
            }
        }
    }
}

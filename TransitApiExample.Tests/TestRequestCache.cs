using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using FluentAssertions;
using Xunit;
using TransitAPIExample;

namespace TransitApiExample.Tests
{
    public class TestRequestCache
    {
        [Fact]
        public async Task RequestCacheShouldCache() {
            var cache = new RequestCache<string, int>(
                (x) => Task.FromResult(new Random().Next(0, 999999)),
                new TimeSpan(0, 0, 0, 1, 0)
            );
            var result1 = await cache.Get("test");
            var result2 = await cache.Get("test");
            result1.ShouldBeEquivalentTo(result2);
        }

        [Fact]
        public async Task RequestCacheShouldExpire()
        {
            var cache = new RequestCache<string, int>(
                (x) => Task.FromResult(new Random().Next(0, 999999)),
                new TimeSpan(0, 0, 0, 0, 0)
            );
            var result1 = await cache.Get("test");
            await Task.Delay(new TimeSpan(0, 0, 0, 0, 10));
            var result2 = await cache.Get("test");
            result1.Should().NotBe(result2);
        }
    }
}

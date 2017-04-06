using System;
using Xunit;
using Xunit.Abstractions;

namespace Xpressive.Home.Services.Tests
{
    public class Base62ConvertTests
    {
        private readonly ITestOutputHelper _output;

        public Base62ConvertTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Given_a_long_byte_array_then_it_works()
        {
            var converter = new Base62Converter();
            var array = new byte[128];
            var random = new Random(0);
            random.NextBytes(array);

            var result = converter.ToBase62(array);

            _output.WriteLine(result);
        }

        [Fact]
        public void Given_a_short_byte_array_then_it_works()
        {
            var converter = new Base62Converter();
            var array = new byte[2];
            var random = new Random(0);
            random.NextBytes(array);

            var result = converter.ToBase62(array);

            _output.WriteLine(result);
        }

        [Fact]
        public void Given_a_big_number_then_it_works()
        {
            var converter = new Base62Converter();

            var result = converter.ToBase62(UInt64.MaxValue);

            _output.WriteLine(result);
        }

        [Fact]
        public void Given_a_low_number_then_it_works()
        {
            var converter = new Base62Converter();

            var result = converter.ToBase62(13);

            _output.WriteLine(result);
        }

        [Fact]
        public void Given_an_empty_array_then_it_doesnt_fail()
        {
            var converter = new Base62Converter();
            var array = new byte[0];

            var result = converter.ToBase62(array);

            _output.WriteLine(result);
        }

        [Fact]
        public void Given_zero_then_it_doesnt_fail()
        {
            var converter = new Base62Converter();

            var result = converter.ToBase62(0);

            _output.WriteLine(result);
        }
    }
}

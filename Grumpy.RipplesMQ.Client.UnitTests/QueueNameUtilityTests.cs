using System;
using FluentAssertions;
using Xunit;

namespace Grumpy.RipplesMQ.Client.UnitTests
{
    public class QueueNameUtilityTests
    {
        private readonly QueueNameUtility _cut;

        public QueueNameUtilityTests()
        {
            _cut = new QueueNameUtility("ServiceName");
        }

        [Fact]
        public void LongDurableQueueShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => _cut.Build("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890", true));
        }

        [Fact]
        public void ShortDurableQueueShouldMatch()
        {
            var name = _cut.Build("Name", true);

            name.Should().Be("ServiceName.Name");
        }

        [Fact]
        public void LongDurableQueueShouldReplaceServiceName()
        {
            var name = _cut.Build("12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789", true);

            name.Should().Be("RipplesMQ.12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");
        }

        [Fact]
        public void LongNoneDurableQueueShouldReturn99Long()
        {
            var name = _cut.Build("12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");

            name.Length.Should().Be(99);
        }
    }
}
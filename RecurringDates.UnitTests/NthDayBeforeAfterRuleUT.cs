using System;
using FluentAssertions;
using NUnit.Framework;

namespace RecurringDates.UnitTests
{
    public class NthDayBeforeAfterRuleUT<T> : ProjectedRuleTestFixture<T> where T : IRuleProcessor, new()
    {
        [TestCase(2015, 3, 3, true)]
        [TestCase(2015, 3, 2, false)]
        [TestCase(2015, 3, 4, false)]
        [TestCase(2015, 2, 28, false)]
        [TestCase(2015, 5, 1, true)]
        public void ThreeDaysAfterThe28Th(int year, int month, int day, bool expected)
        {
            var rule = new NthDayBeforeAfterRule()
            {
                Nth = 3,
                ReferencedRule = new EveryDayRule().TheNthOccurenceInTheMonth( 28 )
            };
            var date = new DateTime(year, month, day);

            Process(rule).IsMatch(date).Should().Be(expected);

        }
    }
}
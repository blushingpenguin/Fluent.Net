using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Fluent.Net.Test
{
    public class OrderedSetTest
    {
        OrderedSet<T> CreateSet<T>(T[] testItems)
        {
            var set = new OrderedSet<T>();
            foreach (T testItem in testItems)
            {
                set.Add(testItem);
            }
            return set;
        }

        [Test]
        public void AddedItemsAreContainedInTheSet()
        {
            int[] testItems = { 11, 5, 4, 1, 9 };
            var set = CreateSet(testItems);

            foreach (int testItem in testItems)
            {
                set.Contains(testItem).Should().BeTrue();
            }
        }

        [Test]
        public void ItemsNotAddedAreNotInTheSet()
        {
            int[] testItems = { 1, 8, 9, 54, 6, 1, 12 };
            var set = CreateSet(testItems);

            int[] notInSetItems = { 99, 2, 7, 10, 53, 104, 1000, -6, -1, -8 };
            foreach (var notInSetItem in notInSetItems)
            {
                set.Contains(notInSetItem).Should().BeFalse();
            }
        }

        [Test]
        public void CountIsCorrect()
        {
            var set = new OrderedSet<string>();
            set.Count.Should().Be(0);
            set.Add("one");
            set.Count.Should().Be(1);
            set.Add("two");
            set.Count.Should().Be(2);
            set.Add("three");
            set.Count.Should().Be(3);
            set.Add("four");
            set.Count.Should().Be(4);
            set.Add("five");
            set.Count.Should().Be(5);
        }

        [Test]
        public void IsReadOnlyIsFalse()
        {
            var set = new OrderedSet<string>();
            set.IsReadOnly.Should().BeFalse();
        }

        [Test]
        public void AddingDuplicateItemsDoesNotChangeCount()
        {
            var set = new OrderedSet<string>();
            set.Add("beta");
            set.Add("alpha");
            set.Add("gamma");
            set.Count.Should().Be(3);
            set.Add("beta");
            set.Count.Should().Be(3);
            set.Add("alpha");
            set.Count.Should().Be(3);
            set.Add("gamma");
        }

        [Test]
        public void AddingItemsPreservesOrder()
        {
            var set = new OrderedSet<string>();
            set.Add("beta");
            set.Add("alpha");
            set.Add("gamma");
            set.Add("epsilon");
            set.Add("delta");

            var copy = new List<string>(set);
            copy.Should().BeEquivalentTo(
                new string[] { "beta", "alpha", "gamma", "epsilon", "delta" },
                options => options.WithStrictOrdering());
        }

        [Test]
        public void AddingDuplicateItemsPreservesOrder()
        {
            var set = new OrderedSet<int>();
            set.Add(40);
            set.Add(6);
            set.Add(4);
            set.Add(9);
            set.Add(40);
            set.Add(4);
            set.Add(9);
            set.Add(9);

            var copy = new List<int>(set);
            copy.Should().BeEquivalentTo(
                new int[] { 40, 6, 4, 9 },
                options => options.WithStrictOrdering());
        }

        [Test]
        public void ClearEmptiesSet()
        {
            var set = CreateSet(new int[] { 9, 100, 12, 43, 4, 44, 9 });
            set.Count.Should().Be(6);
            set.Clear();
            set.GetEnumerator().MoveNext().Should().BeFalse();
            set.Count.Should().Be(0);
        }

        [Test]
        public void CopyToCopiesSet()
        {
            decimal[] testItems = new decimal[] { 14.0M, 16.0M, 3.0M, 3.4M, 6.7123M, 17.49M };
            var set = CreateSet(testItems);
            decimal[] copy = new decimal[testItems.Length];
            set.CopyTo(copy, 0);
            copy.Should().BeEquivalentTo(testItems,
                options => options.WithStrictOrdering());
        }

        [Test]
        public void NonGenericEnumeratorEnumeratesItems()
        {
            var set = CreateSet(new string[] { "upsilon", "tau", "sigma" });
            IEnumerable enumerable = set;
            IEnumerator enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be("upsilon");
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be("tau");
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be("sigma");
            enumerator.MoveNext().Should().BeFalse();
        }

        [Test]
        public void GenericEnumeratorEnumeratorsItems()
        {
            var set = CreateSet(new string[] { "upsilon", "tau", "sigma" });
            IEnumerable<string> enumerable = set;
            IEnumerator<string> enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be("upsilon");
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be("tau");
            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be("sigma");
            enumerator.MoveNext().Should().BeFalse();
        }

        [Test]
        public void RemoveRemovesItemsKeepingOrder()
        {
            int[] testItems = new int[] { 99, 43, 23, 66, 14, 55, 9, 8, 12, -4, -6, 100, -100 };
            var set = CreateSet(testItems);

            var copy = new List<int>(set);
            copy.Should().BeEquivalentTo(testItems,
                options => options.WithStrictOrdering());

            set.Remove(55);
            copy = new List<int>(set);
            copy.Should().BeEquivalentTo(new int[] { 99, 43, 23, 66, 14, 9, 8, 12, -4, -6, 100, -100 },
                options => options.WithStrictOrdering());

            set.Remove(43);
            copy = new List<int>(set);
            copy.Should().BeEquivalentTo(new int[] { 99, 23, 66, 14, 9, 8, 12, -4, -6, 100, -100 },
                options => options.WithStrictOrdering());

            set.Remove(-4);
            copy = new List<int>(set);
            copy.Should().BeEquivalentTo(new int[] { 99, 23, 66, 14, 9, 8, 12, -6, 100, -100 },
                options => options.WithStrictOrdering());

            set.Remove(23);
            copy = new List<int>(set);
            copy.Should().BeEquivalentTo(new int[] { 99, 66, 14, 9, 8, 12, -6, 100, -100 },
                options => options.WithStrictOrdering());

            set.Remove(9);
            set.Remove(14);
            copy = new List<int>(set);
            copy.Should().BeEquivalentTo(new int[] { 99, 66, 8, 12, -6, 100, -100 },
                options => options.WithStrictOrdering());

            set.Remove(99);
            set.Remove(-100);
            copy = new List<int>(set);
            copy.Should().BeEquivalentTo(new int[] { 66, 8, 12, -6, 100 },
                options => options.WithStrictOrdering());
        }

        [Test]
        public void RemovingItemsThatDontExistDoesNothing()
        {
            string[] testItems = { "shall", "i", "write", "more", "tests" };
            var set = CreateSet(testItems);
            set.Remove("these");
            set.Remove("items");
            set.Remove("are");
            set.Remove("not");
            set.Remove("present");

            var copy = new List<string>(set);
            copy.Should().BeEquivalentTo(testItems,
                options => options.WithStrictOrdering());
        }
    }
}

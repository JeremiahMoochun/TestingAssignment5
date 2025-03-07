using NUnit.Framework;
using System.Reflection;
using System;
using System.Collections.Generic; // Add this line
using System.Linq;

namespace SmartLockTests
{
    [TestFixture]
    public class SmartLockTestsClass
    {
        private const string DllPath = "leetgrind.dll";
        private const string ClassName = "leetgrind.SmartLock";

        private static object? lockObject;
        private static MethodInfo? resetMethod;
        private static MethodInfo? enterCodeMethod;
        private static MethodInfo? lockMethod;
        private static MethodInfo? getLockStateMethod;

        private const string LockedValue = "Locked";   // Replace with the actual value
        private const string UnlockedValue = "Unlocked"; // Replace with the actual value
        private const string FaultValue = "Fault";    // Replace with the actual value

        [SetUp]
        public void Setup()
        {
            if (lockObject == null)
            {
                Assembly assembly = Assembly.LoadFile(System.IO.Path.Combine(Environment.CurrentDirectory, DllPath));
                Type? lockType = assembly.GetType(ClassName);
                lockObject = Activator.CreateInstance(lockType ?? throw new InvalidOperationException("Could not create instance of SmartLock class."));

                resetMethod = lockType?.GetMethod("Reset");
                enterCodeMethod = lockType?.GetMethod("EnterCode");
                lockMethod = lockType?.GetMethod("Lock");
                getLockStateMethod = lockType?.GetMethod("GetLockState");

                if (resetMethod == null || enterCodeMethod == null || lockMethod == null || getLockStateMethod == null)
                {
                    throw new InvalidOperationException("Could not find required methods in SmartLock class.");
                }
            }

            resetMethod?.Invoke(lockObject, null);
        }

        [Test]
        public void TestValidCodeUnlock()
        {
            enterCodeMethod?.Invoke(lockObject, new object[] { "1234" });
            Assert.That(getLockStateMethod?.Invoke(lockObject, null), Is.EqualTo(UnlockedValue), "Lock should be unlocked");
        }

        [Test]
        public void TestInvalidCodeDoesNotUnlock()
        {
            enterCodeMethod?.Invoke(lockObject, new object[] { "9999" });
            Assert.That(getLockStateMethod?.Invoke(lockObject, null), Is.EqualTo(LockedValue), "Lock should remain locked");
        }

        [Test]
        public void TestLockMethodLocksTheLock()
        {
            enterCodeMethod?.Invoke(lockObject, new object[] { "1234" });
            lockMethod?.Invoke(lockObject, null);
            Assert.That(getLockStateMethod?.Invoke(lockObject, null), Is.EqualTo(LockedValue), "Lock should be locked");
        }

        [Test]
        public void TestResetMethodResetsToLockedState()
        {
            enterCodeMethod?.Invoke(lockObject, new object[] { "1234" });
            resetMethod?.Invoke(lockObject, null);
            Assert.That(getLockStateMethod?.Invoke(lockObject, null), Is.EqualTo(LockedValue), "Lock should be reset to locked state");
        }

        [Test]
        public void DiscoverRandomError()
        {
            int numberOfTestRuns = 20; // Number of times to run the test
            int maxAttempts = 50; // Adjust as needed
            int totalFaultStateCount = 0;
            List<int> attemptsToFaultList = new List<int>(); // To store attempts to trigger fault

            for (int testRun = 1; testRun <= numberOfTestRuns; testRun++)
            {
                int faultStateCount = 0;
                int attemptsToFault = 0;

                for (int i = 0; i < maxAttempts; i++)
                {
                    resetMethod?.Invoke(lockObject, null);
                    for (int j = 0; j < 100; j++)
                    {
                        enterCodeMethod?.Invoke(lockObject, new object[] { "9999" });
                        string? lockState = (string?)getLockStateMethod?.Invoke(lockObject, null);
                        if (lockState == FaultValue)
                        {
                            faultStateCount++;
                            attemptsToFault = j + 1;
                            Console.WriteLine($"Fault state triggered after {j + 1} attempts in iteration {i} in Test Run {testRun}");
                            break;
                        }
                    }
                    if (faultStateCount > 0)
                    {
                        break; // Exit inner loop if Fault is triggered
                    }
                }

                totalFaultStateCount += faultStateCount;
                if (attemptsToFault > 0)
                {
                    attemptsToFaultList.Add(attemptsToFault);
                }

                Console.WriteLine($"Fault state triggered {faultStateCount} times out of {maxAttempts} attempts in Test Run {testRun}.");
            }

            double probability = (double)totalFaultStateCount / numberOfTestRuns;
            Console.WriteLine($"Probability of Fault state: {probability}");

            double averageAttemptsToFault = attemptsToFaultList.Count > 0 ? attemptsToFaultList.Average() : 0;
            Console.WriteLine($"Average attempts to trigger Fault state: {averageAttemptsToFault}");

            Assert.Greater(totalFaultStateCount, 0, "Fault state was not triggered within the maximum number of attempts.");
        }
    }
}
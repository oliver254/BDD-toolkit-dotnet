using System;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Execution;
using ITLIBRIUM.BddToolkit.Docs;
using ITLIBRIUM.BddToolkit.Syntax.Scenarios;
using ITLIBRIUM.BddToolkit.Tests;
using ITLIBRIUM.BddToolkit.Tests.Results.Exceptions;
using Moq;
using Xunit;

namespace ITLIBRIUM.BddToolkit.Execution
{
    public class TestedScenarioTests : TestsBase
    {
        private readonly Mock<DocPublisher> _docPublisherMock;

        public TestedScenarioTests() => _docPublisherMock = new Mock<DocPublisher>();

        [Fact]
        public void ScenarioIsPublishedUnchanged()
        {
            var testedScenario = CreateScenarioWithResultsCheck().RunTest();

            Publish(testedScenario);

            var expectedScenario = testedScenario.Scenario;
            _docPublisherMock.Verify(p => p
                    .Append(in expectedScenario,
                        It.IsAny<TestStatus>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void ScenarioWithPassedTestIsPublishedWithPassedTestStatus()
        {
            var testedScenario = CreateScenarioWithResultsCheck().RunTest();

            Publish(testedScenario);

            _docPublisherMock.Verify(p => p
                    .Append(It.Ref<Scenario>.IsAny,
                        TestStatus.Passed,
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void ScenarioWithFailedTestIsPublishedWithFailedTestStatus()
        {
            FirstThenActionThrows(new InvalidOperationException());
            var testedScenario = CreateScenarioWithResultsCheck().RunTest();

            Publish(testedScenario);

            _docPublisherMock.Verify(p => p
                    .Append(It.Ref<Scenario>.IsAny,
                        TestStatus.Failed,
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void ExceptionIsNotThrownForPassingTest()
        {
            var testedScenario = CreateScenarioWithResultsCheck().RunTest();

            Action action = () => testedScenario.ThrowOnErrors();

            action.Should().NotThrow();
        }

        [Fact]
        public void GivenActionFailedIsThrownForExceptionInGivenActions()
        {
            var exception = new InvalidOperationException();
            FirstGivenActionThrows(exception);
            var testedScenario = CreateScenarioWithResultsAndExceptionCheck().RunTest();

            Action action = () => testedScenario.ThrowOnErrors();

            action.Should().ThrowExactly<GivenActionFailed>()
                .And.ExceptionFromGivenAction.Should().BeSameAs(exception);
        }

        [Fact]
        public void UncheckedExceptionInWhenActionFoundIsThrownForExceptionInWhenActionIfNoExceptionCheckIsMade()
        {
            var exception = new InvalidOperationException();
            WhenActionThrows(exception);
            var testedScenario = CreateScenarioWithResultsCheck().RunTest();

            Action action = () => testedScenario.ThrowOnErrors();

            action.Should().ThrowExactly<UncheckedExceptionInWhenActionFound>()
                .And.ExceptionFromWhenAction.Should().BeSameAs(exception);
        }
        
        [Fact]
        public void ExceptionChecksFailedIsThrownIfWhenActionThrowsAndExceptionCheckFails()
        {
            var exception1 = new InvalidOperationException();
            WhenActionThrows(exception1);
            var exception2 = new AssertionFailedException("error");
            ExceptionCheckThrows(exception2);
            var testedScenario = CreateScenarioWithResultsAndExceptionCheck().RunTest();

            Action action = () => testedScenario.ThrowOnErrors();

            action.Should().ThrowExactly<ExceptionChecksFailed>()
                .And.ExceptionFromWhenAction.Should().BeSameAs(exception1);
            action.Should().ThrowExactly<ExceptionChecksFailed>()
                .And.FailedExceptionChecks.Should().BeEquivalentTo(new[] { exception2 });
        }
        
        [Fact]
        public void AssertsFailedIsThrownIfOneAssertFailed()
        {
            var exception1 = new AssertionFailedException("error1");
            FirstThenActionThrows(exception1);
            var testedScenario = CreateScenarioWithResultsCheck().RunTest();

            Action action = () => testedScenario.ThrowOnErrors();

            action.Should().ThrowExactly<AssertsFailed>()
                .And.FailedAssertions.Should().BeEquivalentTo(new[] { exception1 });
        }

        [Fact]
        public void AssertsFailedIsThrownIfMoreThanOneAssertFailed()
        {
            var exception1 = new AssertionFailedException("error1");
            FirstThenActionThrows(exception1);
            var exception2 = new AssertionFailedException("error2");
            SecondThenActionThrows(exception2);
            var testedScenario = CreateScenarioWithResultsCheck().RunTest();

            Action action = () => testedScenario.ThrowOnErrors();

            action.Should().ThrowExactly<AssertsFailed>()
                .And.FailedAssertions.Should().BeEquivalentTo(new[] { exception1, exception2 });
        }

        private void Publish(TestedScenario testedScenario) =>
            testedScenario.PublishDoc(_docPublisherMock.Object, CancellationToken.None);
    }
}
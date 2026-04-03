using AzureAI.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

namespace AzureAI.Application.Tests;

public sealed class ValidationBehaviorTests
{
    public sealed record FakeRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_NoValidators_CallsNextAndReturnsResult()
    {
        var behavior = new ValidationBehavior<FakeRequest, string>([]);
        var request = new FakeRequest("test");

        var result = await behavior.Handle(request, _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ValidatorPasses_CallsNext()
    {
        var validator = new Mock<IValidator<FakeRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<FakeRequest>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<FakeRequest, string>([validator.Object]);
        var request = new FakeRequest("valid");

        var result = await behavior.Handle(request, _ => Task.FromResult("result"), CancellationToken.None);

        result.Should().Be("result");
    }

    [Fact]
    public async Task Handle_ValidatorFails_ThrowsValidationExceptionWithErrors()
    {
        var failure = new ValidationFailure("Value", "Value cannot be empty.");
        var validationResult = new ValidationResult([failure]);

        var validator = new Mock<IValidator<FakeRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<FakeRequest>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(validationResult);

        var behavior = new ValidationBehavior<FakeRequest, string>([validator.Object]);
        var request = new FakeRequest("");

        var act = async () => await behavior.Handle(request, _ => Task.FromResult("nope"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Value");
    }

    [Fact]
    public async Task Handle_MultipleValidatorsOneFails_ThrowsValidationException()
    {
        var passingValidator = new Mock<IValidator<FakeRequest>>();
        passingValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<FakeRequest>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ValidationResult());

        var failure = new ValidationFailure("Value", "Bad value.");
        var failingValidator = new Mock<IValidator<FakeRequest>>();
        failingValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<FakeRequest>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ValidationResult([failure]));

        var behavior = new ValidationBehavior<FakeRequest, string>([passingValidator.Object, failingValidator.Object]);
        var request = new FakeRequest("bad");

        var act = async () => await behavior.Handle(request, _ => Task.FromResult("nope"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}

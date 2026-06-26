using FluentValidation;
using HelpDesk.Application.Common.DTOs;
using HelpDesk.Application.Features.Auth;
using HelpDesk.Application.Features.Comments;
using HelpDesk.Application.Features.Departments;
using HelpDesk.Application.Features.Tickets;
using HelpDesk.Application.Features.Users;

namespace HelpDesk.Application.Common.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Must contain an uppercase letter")
            .Matches("[a-z]").WithMessage("Must contain a lowercase letter")
            .Matches("[0-9]").WithMessage("Must contain a digit");
    }
}

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Category).IsInEnum();
    }
}

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
    }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Data.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Data.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Data.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Data.Role).IsInEnum();
    }
}

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Data.Description).MaximumLength(500);
    }
}

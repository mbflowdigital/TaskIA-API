using Application.Core.DTOs.Auth;
using FluentValidation;

namespace Application.Core.Validators.Auth;

/// <summary>
/// Validador para requisição de login
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.CPF)
            .NotEmpty().WithMessage("CPF é obrigatório")
            .Length(11).WithMessage("CPF deve ter 11 dígitos")
            .Matches(@"^\d{11}$").WithMessage("CPF deve conter apenas números");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória");
    }
}

/// <summary>
/// Validador para troca de senha de primeiro acesso
/// </summary>
public class ChangePasswordFirstAccessRequestValidator : AbstractValidator<ChangePasswordFirstAccessRequest>
{
    public ChangePasswordFirstAccessRequestValidator()
    {
        RuleFor(x => x.CPF)
            .NotEmpty().WithMessage("CPF é obrigatório")
            .Length(11).WithMessage("CPF deve ter 11 dígitos")
            .Matches(@"^\d{11}$").WithMessage("CPF deve conter apenas números");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Senha atual é obrigatória");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória")
            .MinimumLength(8).WithMessage("Nova senha deve ter no mínimo 8 caracteres")
            .MaximumLength(50).WithMessage("Nova senha deve ter no máximo 50 caracteres");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória")
            .Equal(x => x.NewPassword).WithMessage("Senhas não conferem");
    }
}

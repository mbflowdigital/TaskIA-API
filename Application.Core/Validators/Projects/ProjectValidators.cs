using Application.Core.DTOs.Projects;
using FluentValidation;

namespace Application.Core.Validators.Projects;

/// <summary>
/// Validador para criação de Project
/// </summary>
public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do projeto é obrigatório")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.Objective)
            .MaximumLength(1000).WithMessage("Objetivo deve ter no máximo 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Objective));

        RuleFor(x => x.Status)
            .Must(BeValidStatus).WithMessage("Status inválido. Use: Draft, Active, Paused, Completed ou Cancelled")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Data de término deve ser maior ou igual à data de início")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID do usuário é obrigatório");
    }

    private bool BeValidStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return true;
        
        var validStatuses = new[] { "Draft", "Active", "Paused", "Completed", "Cancelled" };
        return validStatuses.Contains(status);
    }
}

/// <summary>
/// Validador para atualização de Project
/// </summary>
public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID do projeto é obrigatório");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do projeto é obrigatório")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.Objective)
            .MaximumLength(1000).WithMessage("Objetivo deve ter no máximo 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Objective));

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status é obrigatório")
            .Must(BeValidStatus).WithMessage("Status inválido. Use: Draft, Active, Paused, Completed ou Cancelled");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Data de término deve ser maior ou igual à data de início")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }

    private bool BeValidStatus(string status)
    {
        var validStatuses = new[] { "Draft", "Active", "Paused", "Completed", "Cancelled" };
        return validStatuses.Contains(status);
    }
}

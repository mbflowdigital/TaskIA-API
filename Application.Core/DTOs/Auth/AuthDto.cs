namespace Application.Core.DTOs.Auth;

/// <summary>
/// DTO para requisi��o de login
/// </summary>
public class LoginRequest
{
    public string CPF { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO para resposta de login
/// </summary>
public class LoginResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsFirstAccess { get; set; }
    public string Role { get; set; } = "USER";

    /// <summary>
    /// Indica que o usuário ADM ainda não vinculou sua empresa e deve passar pelo onboarding.
    /// </summary>
    public bool RequiresOnboarding { get; set; }
    
    // Preparado para futuro (JWT)
    public string? Token { get; set; }
    public DateTime? TokenExpiration { get; set; }
}

/// <summary>
/// DTO para o endpoint de onboarding do ADM (cria empresa e vincula ao usuário)
/// </summary>
public class OnboardingRequest
{
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int NumberOfMembers { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// DTO para registro/criação de usuário via Auth
/// (senha padrão: data de nascimento no formato ddMMyyyy)
/// </summary>
public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
}

/// <summary>
/// DTO para troca de senha de primeiro acesso
/// </summary>
public class ChangePasswordFirstAccessRequest
{
    public string CPF { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

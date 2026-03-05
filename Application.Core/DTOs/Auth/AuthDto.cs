namespace Application.Core.DTOs.Auth;

/// <summary>
/// DTO para requisição de login
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
    
    // JWT
    public string? Token { get; set; }
    public DateTime? TokenExpiration { get; set; }
    public string? RefreshToken { get; set; }
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

/// <summary>
/// DTO para requisição de refresh token
/// </summary>
public class RefreshTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// DTO para troca de senha (usuário autenticado)
/// </summary>
public class ChangePasswordRequest
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO para resetar senha (esqueceu a senha)
/// </summary>
public class ForgotPasswordRequest
{
    public string CPF { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

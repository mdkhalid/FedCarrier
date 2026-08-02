using FedCarrier.Contracts;
using FedCarrier.Identity.Application.Commands;
using FedCarrier.Identity.Application.Queries;
using FedCarrier.Identity.Domain;
using FedCarrier.Identity.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FedCarrier.Identity.Application.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<Guid>>
{
    private readonly IdentityDbContext _db;
    public RegisterCommandHandler(IdentityDbContext db) => _db = db;

    public async Task<ApiResponse<Guid>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (existing is not null)
            return new ApiResponse<Guid> { Success = false, Errors = new List<string> { "Email already registered" } };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<Guid> { Success = true, Data = user.Id };
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<LoginResponse>>
{
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;
    public LoginCommandHandler(IdentityDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new ApiResponse<LoginResponse> { Success = false, Errors = new List<string> { "Invalid credentials" } };

        var token = GenerateJwt(user);
        var refreshToken = GenerateRefreshToken();

        user.LastLoginAt = DateTime.UtcNow;
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<LoginResponse>
        {
            Success = true,
            Data = new LoginResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                UserId = user.Id,
                Role = user.Role
            }
        };
    }

    private string GenerateJwt(User user)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
        var securityKey = System.Text.Encoding.UTF8.GetBytes(key);
        var signingAlgorithm = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256;
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(securityKey),
            signingAlgorithm);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role)
            },
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var random = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return Convert.ToBase64String(random);
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<LoginResponse>>
{
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;
    public RefreshTokenCommandHandler(IdentityDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<ApiResponse<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var refreshToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.Token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow, ct);

        if (refreshToken is null)
            return new ApiResponse<LoginResponse> { Success = false, Errors = new List<string> { "Invalid refresh token" } };

        var user = await _db.Users.FindAsync(new object[] { refreshToken.UserId }, ct);
        if (user is null)
            return new ApiResponse<LoginResponse> { Success = false, Errors = new List<string> { "User not found" } };

        var token = GenerateJwt(user);
        var newRefreshToken = GenerateRefreshToken();

        refreshToken.IsRevoked = true;
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return new ApiResponse<LoginResponse>
        {
            Success = true,
            Data = new LoginResponse
            {
                AccessToken = token,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                UserId = user.Id,
                Role = user.Role
            }
        };
    }

    private string GenerateJwt(User user)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
        var securityKey = System.Text.Encoding.UTF8.GetBytes(key);
        var signingAlgorithm = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256;
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(securityKey),
            signingAlgorithm);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role)
            },
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var random = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return Convert.ToBase64String(random);
    }
}

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserDto>>
{
    private readonly IdentityDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetCurrentUserQueryHandler(IdentityDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiResponse<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return new ApiResponse<UserDto> { Success = false, Errors = new List<string> { "User not authenticated" } };

        var user = await _db.Users.FindAsync(new object[] { Guid.Parse(userId) }, ct);
        if (user is null)
            return new ApiResponse<UserDto> { Success = false, Errors = new List<string> { "User not found" } };

        return new ApiResponse<UserDto>
        {
            Success = true,
            Data = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive
            }
        };
    }
}

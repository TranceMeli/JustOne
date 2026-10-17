using JustOne.Auth;
using JustOne.Data;
using JustOne.Entities;
using JustOne.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static JustOne.Models.AuthModels;

namespace JustOne.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ITokenService tokenService,
    AppDbContext db,
    IOptions<JwtSettings> jwtSettings) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return BadRequest($"Email '{request.Email}' is already registered.");
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        await userManager.AddToRoleAsync(user, Roles.User);

        return Ok($"User '{request.Email}' registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(user, roles);

        var refreshToken = tokenService.CreateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = refreshExpiresAt
        });

        await db.SaveChangesAsync();

        return Ok(new AuthResponse(
            user.Id, user.Email!, roles,
            accessToken, accessExpiresAt,
            refreshToken, refreshExpiresAt));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (existing is null)
        {
            return Unauthorized();
        }

        if (!existing.IsActive)
        {
            // War der Token schon widerrufen und taucht erneut auf: vermutlich gestohlen -
            // vorsichtshalber ALLE aktiven Tokens dieses Users invalidieren.
            if (existing.Revoked is not null)
            {
                await RevokeAllActiveTokensAsync(existing.UserId);
            }

            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(existing.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        var newRefreshToken = tokenService.CreateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenDays);

        existing.Revoked = DateTime.UtcNow;
        existing.ReplacedToken = newRefreshToken;

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = refreshExpiresAt
        });

        await db.SaveChangesAsync();

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(user, roles);

        return Ok(new AuthResponse(
            user.Id, user.Email!, roles,
            accessToken, accessExpiresAt,
            newRefreshToken, refreshExpiresAt));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RevokeRequest request)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (token is null || !token.IsActive)
        {
            return NotFound("Token not found or already inactive.");
        }

        token.Revoked = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok("Refresh token revoked.");
    }

    [HttpPost("add-role")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddRole(AddRoleRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return NotFound($"No user found with email '{request.Email}'.");
        }

        if (!await roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest($"Role '{request.Role}' does not exist.");
        }

        await userManager.AddToRoleAsync(user, request.Role);

        return Ok($"Role '{request.Role}' added to '{request.Email}'. Log in again to refresh the token.");
    }

    private async Task RevokeAllActiveTokensAsync(string userId)
    {
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.Revoked == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.Revoked = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }
}
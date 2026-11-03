using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Auth.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Auth.Infrastructure;

public class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthDbContext _dbContext;

    public GetUserProfileQueryHandler(UserManager<ApplicationUser> userManager, AuthDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserIdOrEmail)
            ?? await _userManager.FindByNameAsync(request.UserIdOrEmail)
            ?? await _userManager.FindByEmailAsync(request.UserIdOrEmail);

        if (user == null)
        {
            return Result.Failure<UserProfileDto>(Error.NotFound("Profile.UserNotFound", "Usuario no encontrado."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var passkeyCount = await _dbContext.PasskeyCredentials
            .CountAsync(p => p.UserId == user.Id, cancellationToken);

        var dto = new UserProfileDto(
            user.Id,
            user.UserName ?? "",
            user.Email ?? "",
            user.PhoneNumber,
            roles.ToList(),
            user.TwoFactorEnabled,
            passkeyCount,
            user.CreatedAt,
            user.LastLoginAt);

        return Result.Success(dto);
    }
}

public class ChangeUserPasswordCommandHandler : ICommandHandler<ChangeUserPasswordCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChangeUserPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return Result.Failure<bool>(Error.Validation("Profile.CurrentPasswordRequired", "Debe ingresar su contraseña actual."));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result.Failure<bool>(Error.Validation("Profile.NewPasswordRequired", "Debe ingresar la nueva contraseña."));
        }

        if (request.NewPassword.Length < 6)
        {
            return Result.Failure<bool>(Error.Validation("Profile.PasswordTooShort", "La nueva contraseña debe tener como mínimo 6 caracteres."));
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return Result.Failure<bool>(Error.Validation("Profile.PasswordMismatch", "La nueva contraseña y su confirmación no coinciden."));
        }

        var user = await _userManager.FindByIdAsync(request.UserIdOrEmail)
            ?? await _userManager.FindByNameAsync(request.UserIdOrEmail)
            ?? await _userManager.FindByEmailAsync(request.UserIdOrEmail);

        if (user == null)
        {
            return Result.Failure<bool>(Error.NotFound("Profile.UserNotFound", "Usuario no encontrado."));
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description switch
            {
                "Incorrect password." => "La contraseña actual suministrada es incorrecta.",
                _ => e.Description
            }));
            return Result.Failure<bool>(Error.Validation("Profile.ChangePasswordError", errors));
        }

        return Result.Success(true);
    }
}

public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateUserProfileCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserIdOrEmail)
            ?? await _userManager.FindByNameAsync(request.UserIdOrEmail)
            ?? await _userManager.FindByEmailAsync(request.UserIdOrEmail);

        if (user == null)
        {
            return Result.Failure<bool>(Error.NotFound("Profile.UserNotFound", "Usuario no encontrado."));
        }

        user.PhoneNumber = request.PhoneNumber;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return Result.Failure<bool>(Error.Validation("Profile.UpdateError", errors));
        }

        return Result.Success(true);
    }
}

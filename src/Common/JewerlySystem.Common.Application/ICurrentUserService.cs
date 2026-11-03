namespace JewerlySystem.Common.Application;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    ValueTask<string?> GetCurrentUserNameAsync();
    string? GetCurrentUserName();
}

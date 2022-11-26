namespace DDS.Core.Services;

public interface ILoginService
{
    bool IsLoggedIn { get; }
}

public class LoginService : ILoginService
{
    public bool IsLoggedIn => true;
}
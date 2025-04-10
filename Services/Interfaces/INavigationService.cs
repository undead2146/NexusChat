namespace NexusChat.Services.Interfaces
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route);
        Task NavigateToAsync(string route, object parameter);
        Task GoBackAsync();
        void RegisterRoutes();
    }
}

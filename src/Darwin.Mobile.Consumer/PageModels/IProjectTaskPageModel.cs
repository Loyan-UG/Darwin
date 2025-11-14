using CommunityToolkit.Mvvm.Input;
using Darwin.Mobile.Consumer.Models;

namespace Darwin.Mobile.Consumer.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}
using CommunityToolkit.Mvvm.Input;
using Darwin.Mobile.Business.Models;

namespace Darwin.Mobile.Business.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}
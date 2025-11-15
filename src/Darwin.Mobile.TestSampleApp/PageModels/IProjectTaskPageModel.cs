using CommunityToolkit.Mvvm.Input;
using Darwin.Mobile.TestSampleApp.Models;

namespace Darwin.Mobile.TestSampleApp.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}
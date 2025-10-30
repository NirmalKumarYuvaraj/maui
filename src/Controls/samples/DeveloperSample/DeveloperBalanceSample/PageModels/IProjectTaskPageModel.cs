using CommunityToolkit.Mvvm.Input;
using DeveloperBalanceSample.Models;

namespace DeveloperBalanceSample.PageModels;

public interface IProjectTaskPageModel
{
	IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
	bool IsBusy { get; }
}
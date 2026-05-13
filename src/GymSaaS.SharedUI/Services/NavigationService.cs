using System;

namespace GymSaaS.SharedUI.Services
{
    public enum AppView
    {
        Dashboard,
        SocioList,
        SocioDetails,
        SocioForm,
        PlansList,
        PlansForm,
        Agenda
    }

    public class NavigationService
    {
        public AppView CurrentView { get; private set; } = AppView.SocioList;
        public int? SelectedSocioId { get; private set; }

        public event Action? OnNavigationChanged;

        public void NavigateTo(AppView view, int? socioId = null)
        {
            CurrentView = view;
            SelectedSocioId = socioId;
            OnNavigationChanged?.Invoke();
        }
    }
}

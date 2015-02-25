using System;
using RxApp;

namespace FacebookPagesApp
{
    public interface IUnknownStateViewModel : INavigationViewModel
    {
    }

    public interface IUnknownStateControllerModel : INavigationControllerModel
    {
    }

    public sealed class UnknownStateModel : NavigationModel, IUnknownStateViewModel, IUnknownStateControllerModel
    {
        public UnknownStateModel()
        {
        }
    }
}


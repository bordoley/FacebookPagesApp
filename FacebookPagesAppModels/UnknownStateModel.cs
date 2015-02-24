using System;
using RxApp;

namespace FacebookPagesApp
{
    public interface IUnknownStateViewModel : INavigationModel, IActivationViewModel
    {
    }

    public interface IUnknownStateControllerModel : INavigationModel, IActivationControllerModel
    {
    }

    public sealed class UnknownStateModel : MobileModel, IUnknownStateViewModel, IUnknownStateControllerModel
    {
        public UnknownStateModel()
        {
        }
    }
}


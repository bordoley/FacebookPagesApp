using System;
using RxApp;

namespace FacebookPagesApp
{
    public interface IUnknownStateViewModel : IMobileViewModel
    {
    }

    public interface IUnknownStateControllerModel : IMobileControllerModel
    {
    }

    public sealed class UnknownStateModel : MobileModel, IUnknownStateViewModel, IUnknownStateControllerModel
    {
        public UnknownStateModel()
        {
        }
    }
}


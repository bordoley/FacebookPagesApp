using System;
using System.Windows.Input;
using ReactiveUI;
using RxApp;
using System.Reactive;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    public interface ILoginViewModel : INavigableViewModel, IServiceViewModel
    {
        IObservable<Unit> LoginFailed { get; }
        ICommand Login { get; }
        bool NetworkAvailable { get; }
    }

    public interface ILoginControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        ICommand LoginFailed { get; }
        IObservable<Unit> Login { get; }
        bool CanLogin { set; }
        bool NetworkAvailable { set; }
    }

    public class LoginModel : MobileModel, ILoginViewModel, ILoginControllerModel
    {
        private bool _canLogin = true;

        private bool _networkAvailable = true;

        private readonly IReactiveCommand<object> _login;

        private readonly IReactiveCommand<object> _loginFailed = ReactiveCommand.Create();


        public LoginModel() 
        { 
            _login = this.WhenAnyValue(x => x.CanLogin).ToCommand();
        }


        IObservable<Unit> ILoginViewModel.LoginFailed { get { return _loginFailed.Select(_ => Unit.Default); } }

        ICommand ILoginControllerModel.LoginFailed { get { return _loginFailed; } }


        ICommand ILoginViewModel.Login { get { return _login; } }

        IObservable<Unit> ILoginControllerModel.Login { get { return _login.Select(_ => Unit.Default); } }


        bool ILoginViewModel.NetworkAvailable { get { return _networkAvailable; } }

        bool ILoginControllerModel.NetworkAvailable { set { this.RaiseAndSetIfChanged(ref _networkAvailable, value); } }


        bool ILoginControllerModel.CanLogin { set { this.RaiseAndSetIfChanged(ref _canLogin, value); } }

        private bool CanLogin { get { return _canLogin; } }
    }
}


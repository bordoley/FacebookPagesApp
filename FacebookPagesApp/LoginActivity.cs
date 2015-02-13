using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using RxApp;

namespace FacebookPagesApp
{
    public class LoginActivity : RxActivity<ILoginViewModel>
    {
        // This is very, very evil, but it works reliably and allows the rest of the code to pretend
        // that activities don't matter.
        private static LoginActivity _current;
        public static LoginActivity Current { get { return _current; } }

        private IDisposable subscription = null;
        private Button authButton;
        private TextView networkUnavailableMessage = null;

        public LoginActivity()
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Login);

            authButton = this.FindViewById<Button>(Resource.Id.login_button);
            networkUnavailableMessage = this.FindViewById<TextView>(Resource.Id.network_unavailable_message);
        }

        protected override void OnResume()
        {
            base.OnResume();

            _current = this;

            var subscription = new CompositeDisposable();

            subscription.Add(
                this.BindCommand(
                    this.ViewModel, 
                    vm => vm.Login,
                    view => view.authButton));

            subscription.Add(
                this.WhenAnyObservable(x => x.ViewModel.LoginFailed).Subscribe(_ =>
                    Toast.MakeText(
                        this, 
                        this.Resources.GetString(Resource.String.login_failed), 
                        ToastLength.Long).Show())); 

            subscription.Add(
                this.WhenAnyValue(x => x.ViewModel.NetworkAvailable).Subscribe(networkAvailable =>
                    {
                        if (networkAvailable)
                        {
                            networkUnavailableMessage.Visibility = ViewStates.Gone;
                            authButton.Visibility = ViewStates.Visible;
                        }
                        else
                        {
                            networkUnavailableMessage.Visibility = ViewStates.Visible;
                            authButton.Visibility = ViewStates.Gone;
                        }
                    }));

            this.subscription = subscription;
        }

        protected override void OnPause()
        {
            subscription.Dispose();

            _current = null;

            base.OnPause();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            FacebookSession.onActivityResultDelegate (this, requestCode, resultCode, data);
        }
    }
}


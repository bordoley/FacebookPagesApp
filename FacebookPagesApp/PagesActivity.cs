using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.Widget;

using ReactiveUI;
using RxApp;
using Splat;

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    [Activity(Theme = "@android:style/Theme.Material.Light")]    
    public class PagesActivity : RxActivity<IPagesViewModel>
    {
        private IDisposable subscription = null;

        private SwipeRefreshLayout refresher;
        private Button logoutButton;
        private TextView userName;
        private ImageView profilePicture;

        public PagesActivity()
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.SetContentView(Resource.Layout.Pages);

            refresher = FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
            logoutButton = FindViewById<Button>(Resource.Id.log_out);
            userName = FindViewById<TextView>(Resource.Id.user_name);
            profilePicture = this.FindViewById<ImageView>(Resource.Id.user_profile_picture);
        }

        protected override void OnResume()
        {
            base.OnResume();

            var subscription = new CompositeDisposable();

            subscription.Add(
                Observable.FromEventPattern(refresher, "Refresh").Subscribe(_ => 
                    this.ViewModel.RefeshPosts.Execute(null)));

            subscription.Add(
                this.WhenAnyValue(x => x.ViewModel.RefreshingPosts).Where(x => !x).Subscribe(_ => 
                    refresher.Refreshing = false));

            subscription.Add(
                this.BindCommand(this.ViewModel, vm => vm.LogOut, view => view.logoutButton));

            subscription.Add(
                this.WhenAnyValue(x => x.ViewModel.UserName).Subscribe(x => this.userName.Text = x));

            subscription.Add(
                this.WhenAnyValue(x => x.ViewModel.ProfilePhoto).Subscribe(bitmap =>
                    {
                        profilePicture.SetMinimumHeight((int)bitmap.Height);
                        profilePicture.SetImageDrawable (bitmap.ToNative());
                    }));
                
            this.subscription = subscription;
        }

        public override bool OnCreateOptionsMenu (IMenu menu)
        {
            MenuInflater.Inflate (Resource.Menu.PagesActionBarMenu, menu);       
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.pages_action_bar_new_post:
                    this.ViewModel.CreatePost.Execute(null);
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        protected override void OnPause()
        {
            subscription.Dispose();
            base.OnPause();
        }
    }
}


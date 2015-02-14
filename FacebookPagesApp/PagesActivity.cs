using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.Widget;

using ReactiveUI;
using RxApp;

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    [Activity(Theme = "@android:style/Theme.Holo.Light")]    
    public class PagesActivity : RxActivity<IPagesViewModel>
    {

        private SwipeRefreshLayout refresher;
        private IDisposable subscription = null;

        public PagesActivity()
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.SetContentView(Resource.Layout.Pages);

            refresher = FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
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


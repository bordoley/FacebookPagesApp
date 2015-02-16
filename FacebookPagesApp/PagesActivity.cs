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
    [Activity(Label="Page")]    
    public class PagesActivity : RxActivity<IPagesViewModel>
    {
        private IDisposable subscription = null;

        private SwipeRefreshLayout refresher;
        private Button logoutButton;
        private TextView userName;
        private ImageView profilePicture;
        private Switch showUnpublishedPosts;
        private ListView userpages;

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
            showUnpublishedPosts = this.FindViewById<Switch>(Resource.Id.show_unpublished);

            userpages = this.FindViewById<ListView>(Resource.Id.user_pages);
            var adapter = 
                new ReactiveListAdapter<FacebookAPI.Page>(
                    this.ViewModel.Pages,
                    (viewModel, parent) =>
                        {
                            var view = new TextView(parent.Context);
                            view.Text = viewModel.name;
                            return view;
                        });
            userpages.Adapter = adapter;

            var drawerLayout = this.FindViewById<DrawerLayout> (Resource.Id.drawer_layout);
            drawerLayout.SetDrawerShadow (Resource.Drawable.drawer_shadow_light, (int)GravityFlags.Start);
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
                this.WhenAnyValue(x => x.ViewModel.ProfilePhoto).Where(x => x != null).Subscribe(bitmap =>
                    {
                        profilePicture.SetMinimumHeight((int)bitmap.Height);
                        profilePicture.SetImageDrawable (bitmap.ToNative());
                    }));

            subscription.Add(
                Observable.FromEventPattern<CompoundButton.CheckedChangeEventArgs>(showUnpublishedPosts, "CheckedChange").Select(x => x.EventArgs.IsChecked).Subscribe(x =>
                    {
                        this.ViewModel.ShowUnpublishedPosts = x;
                    }));
             
            subscription.Add(
                Observable.FromEventPattern<AdapterView.ItemClickEventArgs>(userpages, "ItemClick")
                          .Select(x => this.ViewModel.Pages[x.EventArgs.Position])
                          .Subscribe(x => 
                            { 
                                this.ViewModel.CurrentPage = x;
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


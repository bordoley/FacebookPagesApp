using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.Widget;

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
            var adapter = this.ViewModel.Pages.GetAdapter(
                    (viewModel, parent) => new TextView(parent.Context),
                    (viewModel, view) => { view.Text = viewModel.name; });

            userpages.Adapter = adapter;

            var drawerLayout = this.FindViewById<DrawerLayout> (Resource.Id.drawer_layout);
            drawerLayout.SetDrawerShadow (Resource.Drawable.drawer_shadow_light, (int)GravityFlags.Start);
        }

        protected override void OnResume()
        {
            base.OnResume();

            var subscription = new CompositeDisposable();

            subscription.Add(this.ViewModel.RefeshPosts.BindTo(refresher));
                
            subscription.Add(this.ViewModel.LogOut.BindTo(this.logoutButton));   

            subscription.Add(this.ViewModel.UserName.BindTo(this.userName));

            subscription.Add(
                this.ViewModel.ProfilePhoto.Where(x => x != null).Subscribe(bitmap =>
                    {
                        profilePicture.SetMinimumHeight((int)bitmap.Height);
                        profilePicture.SetImageDrawable (bitmap.ToNative());
                    }));

            subscription.Add(this.ViewModel.ShowUnpublishedPosts.BindTo(this.showUnpublishedPosts));
             
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
                    this.ViewModel.CreatePost.Execute();
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


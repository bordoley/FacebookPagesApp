using System;
using System.Collections.Generic;
using System.Linq;

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
using System.Reactive.Subjects;

using Microsoft.FSharp.Core;

namespace FacebookPagesApp
{
    [Activity(Label="Page")]    
    public sealed class PagesActivity : RxActivity<IPagesViewModel>, AbsListView.IOnScrollListener
    {
        // FIXME: Ideally I'd like to add this directly to RxApp.Android in a friendly way. Maybe ObservableListView. 
        // It's hard though do to all the constructors and crazy inheritance patterns.
        private readonly Subject<Tuple<AbsListView, int, int, int>> onScroll = new Subject<Tuple<AbsListView, int, int, int>>();

        private IDisposable subscription = null;

        private SwipeRefreshLayout refresher;
        private Button logoutButton;
        private TextView userName;
        private ImageView profilePicture;
        private Switch showUnpublishedPosts;
        private ListView userpages;
        private ListView posts;

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
            posts = this.FindViewById<ListView>(Resource.Id.pages_posts);

            posts.SetOnScrollListener(this);

            var drawerLayout = this.FindViewById<DrawerLayout> (Resource.Id.drawer_layout);
            drawerLayout.SetDrawerShadow (Resource.Drawable.drawer_shadow_light, (int)GravityFlags.Start);
        }

        protected override void OnResume()
        {
            base.OnResume();

            subscription = Disposables.Combine(
                this.ViewModel.RefeshPosts.Bind(refresher),

                this.ViewModel.LogOut.Bind(this.logoutButton),   

                this.ViewModel.UserName.BindTo(this.userName, x => x.Text),

                this.ViewModel.ProfilePhoto.Where(x => x != null)
                    .ObserveOnMainThread()
                    .Subscribe(bitmap =>
                    {
                      profilePicture.SetMinimumHeight((int)bitmap.Height);
                      profilePicture.SetImageDrawable(bitmap.ToNative());
                    }),

                this.ViewModel.ShowUnpublishedPosts.Bind(this.showUnpublishedPosts),

                this.ViewModel.Pages
                    .Do(x =>
                        {
                            if (x.Count > 0)
                            {
                                this.ViewModel.CurrentPage.Value = FSharpOption<FacebookAPI.Page>.Some(x[0]);
                            }
                        })
                    .BindTo(
                        userpages, 
                        (parent) => new TextView(parent.Context),
                        (viewModel, view) => { view.Text = viewModel.name; }),

                Observable.FromEventPattern<AdapterView.ItemClickEventArgs>(userpages, "ItemClick")
                      .Select(x => x.EventArgs.Position)
                      .Combine(this.ViewModel.Pages)
                      .Select(t => FSharpOption<FacebookAPI.Page>.Some(t.Item2[t.Item1]))
                      .BindTo(this.ViewModel.CurrentPage), 

                this.onScroll.Where(t =>
                    {
                        var firstVisibleItem = t.Item2;
                        var visibleItemCount = t.Item3;
                        var totalItemCount = t.Item4;

                        return firstVisibleItem + visibleItemCount >= (totalItemCount - 4);
                    }).InvokeCommand(this.ViewModel.LoadMorePosts),

                this.ViewModel.Posts
                    .BindTo(
                        posts, 
                        (parent) => new TextView(parent.Context),
                        (viewModel, view) => { view.Text = viewModel.message; }),

                this.OptionsItemSelected
                    .Where(item => item.ItemId == Resource.Id.pages_action_bar_new_post)
                    .InvokeCommand(this.ViewModel.CreatePost)
            );
        }

        public override bool OnCreateOptionsMenu (IMenu menu)
        {
            MenuInflater.Inflate (Resource.Menu.PagesActionBarMenu, menu);       
            return base.OnCreateOptionsMenu(menu);
        }

        protected override void OnPause()
        {
            subscription.Dispose();
            base.OnPause();
        }

        public void OnScroll(AbsListView view, int firstVisibleItem, int visibleItemCount, int totalItemCount)
        {
            onScroll.OnNext(Tuple.Create(view, firstVisibleItem, visibleItemCount, totalItemCount));
        }

        public void OnScrollStateChanged(AbsListView view, ScrollState scrollState)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;

using RxApp;
using RxApp.Android;
using Splat;

using System.Reactive.Linq;
using System.Reactive.Subjects;

using Microsoft.FSharp.Core;


using Observable = System.Reactive.Linq.Observable;
using CardView = Android.Support.V7.Widget.CardView;

namespace FacebookPagesApp
{
    [Activity(Label="@string/facebook_pages")]    
    public sealed class PagesActivity : RxActivity<IPagesViewModel>, AbsListView.IOnScrollListener
    {
        // FIXME: Ideally I'd like to add this directly to RxApp.Android in a friendly way. Maybe ObservableListView. 
        // It's hard though do to all the constructors and crazy inheritance patterns.
        private readonly Subject<Tuple<AbsListView, int, int, int>> onScroll = new Subject<Tuple<AbsListView, int, int, int>>();
        private readonly Subject<ScrollState> onScrollState = new Subject<ScrollState>();

        private IDisposable subscription = null;

        private DrawerLayout drawerLayout;
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
            drawerLayout = this.FindViewById<DrawerLayout> (Resource.Id.drawer_layout);

            posts.SetOnScrollListener(this);

            drawerLayout.SetDrawerShadow (Resource.Drawable.drawer_shadow_light, (int)GravityFlags.Start);
        }

        protected override void OnStart()
        {
            base.OnStart();

            subscription = Disposable.Compose( 
                this.ViewModel.ShowRefresher.BindTo(refresher, x => x.Refreshing), 
                this.ViewModel.RefreshPosts.Bind(refresher), 
              
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

                // FIXME: This really belongs in the controller or maybe the model.
                this.ViewModel.Pages
                    .Where(x => x.Count > 0)
                    .Select(x => FSharpOption<FacebookAPI.Page>.Some(x[0]))
                    .Subscribe(x => this.ViewModel.CurrentPage.Value = x),

                this.ViewModel.Pages
                    .BindTo(
                        userpages, 
                        (parent) => new TextView(parent.Context),
                        (viewModel, view) => { view.Text = viewModel.name; }),

                this.ViewModel.CurrentPage
                    .Where(x => OptionModule.IsSome(x))
                    .Select(x => x.Value.name)
                    .BindTo(this, x=> x.Title),

                RxApp.Observable.CombineLatest(
                        Observable.FromEventPattern<AdapterView.ItemClickEventArgs>(userpages, "ItemClick").Select(x => x.EventArgs.Position),
                        this.ViewModel.Pages)
                    .Select(t => FSharpOption<FacebookAPI.Page>.Some(t.Item2[t.Item1]))

                    // FIXME: Closing the drawer imperitively in the view bindings here
                    // means that the experience is not testable.
                    .ObserveOnMainThread()
                    .Do(_ => this.drawerLayout.CloseDrawers())
                    .BindTo(this.ViewModel.CurrentPage), 

                // FIXME: need
                RxApp.Observable.CombineLatest(this.onScroll, this.onScrollState)
                    .Where(t =>
                    {
                        var firstVisibleItem = t.Item1.Item2;
                        var visibleItemCount = t.Item1.Item3;
                        var totalItemCount = t.Item1.Item4;

                        var scrollState = t.Item2;

                        if ((totalItemCount > 4) && (scrollState != ScrollState.Idle))
                        {
                            return firstVisibleItem + visibleItemCount >= (totalItemCount - 4);
                        }

                        // Issue is who caused the scroll?
                        return false;
                    })
                    .InvokeCommand(this.ViewModel.LoadMorePosts),

                this.ViewModel.Posts
                    .BindTo(
                        posts, 
                        (parent) => 
                            {
                                return LayoutInflater.From(parent.Context).Inflate(Resource.Layout.FacebookPostCard, parent,false);
                                //return (CardView) view;
                            },
                        (viewModel, view) => 
                            { 
                                var textView = view.FindViewById<TextView>(Resource.Id.post_card_text);
                                textView.Text = viewModel.message;
                            }),

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

        protected override void OnStop()
        {
            subscription.Dispose();
            base.OnStop();
        }

        public void OnScroll(AbsListView view, int firstVisibleItem, int visibleItemCount, int totalItemCount)
        {
            onScroll.OnNext(Tuple.Create(view, firstVisibleItem, visibleItemCount, totalItemCount));
        }

        public void OnScrollStateChanged(AbsListView view, ScrollState scrollState)
        {
            onScrollState.OnNext(scrollState);
        }
    }
}
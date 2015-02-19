using System;
using System.Collections.Generic;
using RxApp;
using System.Reactive;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    public interface INewPostViewModel : INavigableViewModel, IServiceViewModel
    {
        IRxProperty<bool> ShouldPublishPost { get; }
        IRxProperty<DateTime> PublishDate { get; }
        IRxProperty<TimeSpan> PublishTime { get; }
        IRxProperty<string> PostContent { get; }
        IRxProperty<FacebookAPI.Page> Page { get; }

        IObservable<IReadOnlyList<FacebookAPI.Page>> Pages { get; }

        IRxCommand PublishPost { get; }
    }

    public interface INewPostControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        IObservable<bool> ShouldPublishPost { get; }
        IObservable<DateTime> PublishDate { get; }
        IObservable<TimeSpan> PublishTime { get; }
        IObservable<string> PostContent { get; }
        IObservable<FacebookAPI.Page> Page { get; }

        IObservable<Unit> PublishPost { get; }
        IRxProperty<bool> CanPublishPost { get; }
    }

    public sealed class NewPostModel : MobileModel, INewPostViewModel, INewPostControllerModel
    {
        private readonly IRxProperty<bool> _shouldPublishPost = RxProperty.Create(true);
        private readonly IRxProperty<DateTime> _publishDate = RxProperty.Create(DateTime.Now);
        private readonly IRxProperty<TimeSpan> _publishTime = RxProperty.Create(new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0));
        private readonly IRxProperty<string> _postContent = RxProperty.Create<string>("");
        private readonly IRxProperty<FacebookAPI.Page> _page;

        private readonly IObservable<IReadOnlyList<FacebookAPI.Page>> _pages;

        private readonly IRxProperty<bool> _canPublishPost = RxProperty.Create(true);
        private readonly IRxCommand _publishPost = RxCommand.Create();

        public NewPostModel(IReadOnlyList<FacebookAPI.Page> pages, FacebookAPI.Page defaultPage)
        {
            _pages = Observable.Return(pages);
            _page = RxProperty.Create(defaultPage);
            _publishPost = _canPublishPost.ToCommand();
        }

        IRxProperty<bool> INewPostViewModel.ShouldPublishPost
        {
            get { return _shouldPublishPost; }
        }

        IObservable<bool> INewPostControllerModel.ShouldPublishPost
        {
            get { return _shouldPublishPost; }
        }

        IObservable<DateTime> INewPostControllerModel.PublishDate 
        {
            get { return _publishDate; }
        }

        IObservable<TimeSpan> INewPostControllerModel.PublishTime
        {
            get { return _publishTime; }
        }

        IRxProperty<DateTime> INewPostViewModel.PublishDate
        {
            get { return this._publishDate; }
        }

        IRxProperty<TimeSpan> INewPostViewModel.PublishTime
        {
            get { return this._publishTime; }
        }

        IRxProperty<string> INewPostViewModel.PostContent
        {
            get { return _postContent; }
        }

        IObservable<string> INewPostControllerModel.PostContent
        {
            get { return _postContent; }
        }

        IObservable<FacebookAPI.Page> INewPostControllerModel.Page
        {
            get { return _page; }
        }

        IRxProperty<FacebookAPI.Page> INewPostViewModel.Page
        {
            get { return _page; }
        }

        IObservable<IReadOnlyList<FacebookAPI.Page>> INewPostViewModel.Pages
        {
            get { return _pages; }
        }

        IRxCommand INewPostViewModel.PublishPost { get { return _publishPost; } }
        IRxProperty<bool> INewPostControllerModel.CanPublishPost { get { return _canPublishPost; } }

        IObservable<Unit> INewPostControllerModel.PublishPost { get { return _publishPost; } }
    }
}


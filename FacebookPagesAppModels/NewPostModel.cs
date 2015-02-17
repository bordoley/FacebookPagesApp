using System;
using RxApp;
using System.Reactive;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    public interface INewPostViewModel : INavigableViewModel, IServiceViewModel
    {
        bool ShouldPublishPost { set; }
        IRxProperty<DateTime> PublishDate { get; }
        IRxProperty<TimeSpan> PublishTime { get; }
        string PostContent { set; }

        IRxCommand PublishPost { get; }
    }

    public interface INewPostControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        IObservable<bool> ShouldPublishPost { get; }
        IObservable<DateTime> PublishDate { get; }
        IObservable<TimeSpan> PublishTime { get; }
        IObservable<string> PostContent { get; }
        IObservable<Unit> PublishPost { get; }
    }

    public sealed class NewPostModel : MobileModel, INewPostViewModel, INewPostControllerModel
    {
        private readonly IRxProperty<bool> _shouldPublishPost = RxProperty.Create<bool>(true);
        private readonly IRxProperty<DateTime> _publishDate = RxProperty.Create<DateTime>(DateTime.Now);
        private readonly IRxProperty<TimeSpan> _publishTime = RxProperty.Create<TimeSpan>(new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0));
        private readonly IRxProperty<string> _postContent = RxProperty.Create<string>("");

        private readonly IRxCommand _publishPost = RxCommand.Create();

        public NewPostModel()
        {
        }

        bool INewPostViewModel.ShouldPublishPost
        {
            set { _shouldPublishPost.Value = value; }
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

        string INewPostViewModel.PostContent
        {
            set { _postContent.Value = value; }
        }

        IObservable<string> INewPostControllerModel.PostContent
        {
            get { return _postContent; }
        }


        IRxCommand INewPostViewModel.PublishPost { get { return _publishPost; } }

        IObservable<Unit> INewPostControllerModel.PublishPost { get { return _publishPost; } }
    }
}

